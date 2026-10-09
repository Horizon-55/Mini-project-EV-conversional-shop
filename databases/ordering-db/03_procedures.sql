-- Збережувані процедури для бази замовлень
-- Містять транзакційну бізнес-логіку з перевіркою переходів статусів (для використання в DAL у Лаб 2)

USE EVShop_Ordering;
GO

-- 1. Створення нового замовлення разом із деталями та початковим записом аудиту
CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId BIGINT,
    @ShippingAddress NVARCHAR(400),
    @ContactPhone NVARCHAR(25),
    @TargetVehicleModel NVARCHAR(150),
    @ConversionType NVARCHAR(50) = 'DIY_Kit',
    @InstallationNotes NVARCHAR(1000) = NULL,
    @CreatedBy NVARCHAR(100) = 'customer',
    @NewOrderId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
    BEGIN
        THROW 50010, 'Клієнта не знайдено або профіль заблоковано', 1;
    END;

    BEGIN TRANSACTION;

    INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
    VALUES (@CustomerId, 'Submitted', SYSUTCDATETIME(), 0.00, @CreatedBy);

    SET @NewOrderId = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetails (
        OrderId, 
        ShippingAddress, 
        ContactPhone, 
        TargetVehicleModel, 
        ConversionType, 
        InstallationNotes
    )
    VALUES (
        @NewOrderId, 
        @ShippingAddress, 
        @ContactPhone, 
        @TargetVehicleModel, 
        @ConversionType, 
        @InstallationNotes
    );

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@NewOrderId, NULL, 'Submitted', @CreatedBy, 'Створено нове замовлення');

    COMMIT TRANSACTION;
END;
GO

-- 2. Додавання товару до замовлення зі знімком ціни на момент покупки
CREATE OR ALTER PROCEDURE dbo.usp_AddOrderItem
    @OrderId BIGINT,
    @ProductId BIGINT,
    @Quantity INT,
    @Discount DECIMAL(18,2) = 0.00
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Quantity <= 0
        THROW 50020, 'Кількість товару має бути більшою за нуль', 1;

    BEGIN TRANSACTION;

    -- Блокуємо рядок замовлення на час перевірки
    DECLARE @OrderStatus NVARCHAR(30);
    SELECT @OrderStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;

    IF @OrderStatus IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;

    IF @OrderStatus <> 'Submitted'
        THROW 50021, 'Редагувати список позицій можна лише для замовлення у статусі Submitted', 1;

    -- Отримуємо ціну та назву з локальної копії каталогу
    DECLARE @ProdName NVARCHAR(200);
    DECLARE @UnitPrice DECIMAL(18,2);

    SELECT @ProdName = Name, @UnitPrice = Price 
    FROM dbo.Products 
    WHERE Id = @ProductId;

    IF @ProdName IS NULL
        THROW 50022, 'Товар відсутній у довіднику компонентів', 1;

    IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId AND ProductId = @ProductId)
    BEGIN
        UPDATE dbo.OrderItems
        SET Quantity = Quantity + @Quantity,
            Discount = @Discount
        WHERE OrderId = @OrderId AND ProductId = @ProductId;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
        VALUES (@OrderId, @ProductId, @ProdName, @UnitPrice, @Quantity, @Discount);
    END;

    -- Оновлюємо загальну суму замовлення
    UPDATE dbo.Orders
    SET TotalAmount = (SELECT ISNULL(SUM(LineTotal), 0.00) FROM dbo.OrderItems WHERE OrderId = @OrderId),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;

    COMMIT TRANSACTION;
END;
GO

-- 3. Підтвердження наявності деталей на складі (Submitted -> StockConfirmed)
CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'manager'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status 
    FROM dbo.Orders WITH (UPDLOCK) 
    WHERE Id = @OrderId;

    IF @CurrentStatus IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;

    -- Перевірка допустимого переходу
    IF @CurrentStatus NOT IN ('Submitted', 'AwaitingValidation')
    BEGIN
        THROW 50002, 'Підтвердити наявність можна лише для нового замовлення (Submitted/AwaitingValidation)', 1;
    END;

    UPDATE dbo.Orders
    SET Status = 'StockConfirmed',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'StockConfirmed', @UpdatedBy, 'Комплектуючі перевірено та зарезервовано на складі');

    COMMIT TRANSACTION;
END;
GO

-- 4. Оплата замовлення (StockConfirmed -> Paid)
CREATE OR ALTER PROCEDURE dbo.usp_PayOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'payment-gateway'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status 
    FROM dbo.Orders WITH (UPDLOCK) 
    WHERE Id = @OrderId;

    IF @CurrentStatus IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;

    IF @CurrentStatus <> 'StockConfirmed'
    BEGIN
        THROW 50003, 'Оплатити можна лише замовлення з підтвердженою наявністю (StockConfirmed)', 1;
    END;

    UPDATE dbo.Orders
    SET Status = 'Paid',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Paid', @UpdatedBy, 'Оплату успішно проведено');

    COMMIT TRANSACTION;
END;
GO

-- 5. Відвантаження клієнту (Paid -> Shipped)
-- Блокує відвантаження без попередньої оплати
CREATE OR ALTER PROCEDURE dbo.usp_ShipOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'warehouse'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status 
    FROM dbo.Orders WITH (UPDLOCK) 
    WHERE Id = @OrderId;

    IF @CurrentStatus IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;

    -- Не можна відвантажити неоплачене
    IF @CurrentStatus <> 'Paid'
    BEGIN
        THROW 50004, 'Неможливо відвантажити неоплачене замовлення! Поточний статус не є Paid', 1;
    END;

    UPDATE dbo.Orders
    SET Status = 'Shipped',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Shipped', @UpdatedBy, 'Передано службі доставки або прийнято в роботу');

    COMMIT TRANSACTION;
END;
GO

-- 6. Скасування замовлення (дозволено тільки ДО оплати)
CREATE OR ALTER PROCEDURE dbo.usp_CancelOrder
    @OrderId BIGINT,
    @CancellationReason NVARCHAR(500) = 'Скасовано за бажанням клієнта',
    @UpdatedBy NVARCHAR(100) = 'customer'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status 
    FROM dbo.Orders WITH (UPDLOCK) 
    WHERE Id = @OrderId;

    IF @CurrentStatus IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;

    -- Не дозволяємо скасувати вже оплачене або відвантажене замовлення
    IF @CurrentStatus IN ('Paid', 'Shipped')
    BEGIN
        THROW 50005, 'Неможливо скасувати замовлення після оплати чи відвантаження', 1;
    END;

    IF @CurrentStatus = 'Cancelled'
    BEGIN
        THROW 50006, 'Замовлення вже має статус Cancelled', 1;
    END;

    UPDATE dbo.Orders
    SET Status = 'Cancelled',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @UpdatedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Cancelled', @UpdatedBy, @CancellationReason);

    COMMIT TRANSACTION;
END;
GO

-- 7. Детальна вибірка замовлення для мапінгу в Dapper (кілька наборів даних)
CREATE OR ALTER PROCEDURE dbo.usp_GetOrderDetailsById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    -- Набір 1: Замовлення + Клієнт + Деталі 1:1
    SELECT 
        o.Id,
        o.CustomerId,
        c.FullName AS CustomerName,
        c.Email AS CustomerEmail,
        o.Status,
        o.OrderDate,
        o.TotalAmount,
        o.CreatedAt,
        o.UpdatedAt,
        o.RowVersion,
        d.ShippingAddress,
        d.ContactPhone,
        d.TargetVehicleModel,
        d.ConversionType,
        d.InstallationNotes
    FROM dbo.Orders o
    INNER JOIN dbo.Customers c ON o.CustomerId = c.Id
    INNER JOIN dbo.OrderDetails d ON o.Id = d.OrderId
    WHERE o.Id = @OrderId AND o.IsDeleted = 0;

    -- Набір 2: Позиції замовлення
    SELECT 
        i.OrderId,
        i.ProductId,
        i.ProductName,
        i.UnitPrice,
        i.Quantity,
        i.Discount,
        i.LineTotal
    FROM dbo.OrderItems i
    WHERE i.OrderId = @OrderId;

    -- Набір 3: Хронологія змін статусів
    SELECT 
        h.Id,
        h.OldStatus,
        h.NewStatus,
        h.ChangedAt,
        h.ChangedBy,
        h.Comment
    FROM dbo.OrderStatusHistory h
    WHERE h.OrderId = @OrderId
    ORDER BY h.ChangedAt ASC;
END;
GO
