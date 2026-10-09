-- Тестові дані для бази замовлень
-- Скрипт ідемпотентний: перевіряє IF NOT EXISTS, тому повторний запуск не створить дублікатів

USE EVShop_Ordering;
GO

-- 1. Клієнти
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'ivan.zakharchuk@example.com')
BEGIN
    INSERT INTO dbo.Customers (Email, FullName, PhoneNumber, CreatedBy)
    VALUES (N'ivan.zakharchuk@example.com', N'Іван Захарчук', N'+380501112233', N'admin');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'olena.petrenko@example.com')
BEGIN
    INSERT INTO dbo.Customers (Email, FullName, PhoneNumber, CreatedBy)
    VALUES (N'olena.petrenko@example.com', N'Олена Петренко', N'+380674445566', N'admin');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = N'serhii.koval@example.com')
BEGIN
    INSERT INTO dbo.Customers (Email, FullName, PhoneNumber, CreatedBy)
    VALUES (N'serhii.koval@example.com', N'Сергій Коваль', N'+380937778899', N'admin');
END;
GO

-- 2. Локальна копія товарів (знімок з каталогу для швидкого оформлення замовлення)
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5001)
    INSERT INTO dbo.Products (Id, Name, Price, StockQuantity) 
    VALUES (5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5002)
    INSERT INTO dbo.Products (Id, Name, Price, StockQuantity) 
    VALUES (5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 24);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5003)
    INSERT INTO dbo.Products (Id, Name, Price, StockQuantity) 
    VALUES (5003, N'Інвертор Sevcon Gen4 Size 6 300V 400A', 1100.00, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5004)
    INSERT INTO dbo.Products (Id, Name, Price, StockQuantity) 
    VALUES (5004, N'Комплект перехідної плити КПП BMW E30 / Getrag', 450.00, 12);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 5005)
    INSERT INTO dbo.Products (Id, Name, Price, StockQuantity) 
    VALUES (5005, N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', 620.00, 15);
GO

-- 3. Замовлення з різними статусами життєвого циклу

-- Замовлення 1: щойно створене клієнтом (Submitted)
DECLARE @Cust1Id BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'ivan.zakharchuk@example.com');
IF @Cust1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CustomerId = @Cust1Id AND Status = 'Submitted')
BEGIN
    INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
    VALUES (@Cust1Id, 'Submitted', DATEADD(DAY, -2, SYSUTCDATETIME()), 2500.00, N'ivan.zakharchuk');

    DECLARE @Ord1Id BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
    VALUES (@Ord1Id, N'м. Чернівці, вул. Університетська, 28', N'+380501112233', N'BMW E30 (1988) Coupe', N'DIY_Kit', N'Консультація щодо підключення датчика валу');

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
    VALUES 
        (@Ord1Id, 5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 1, 0.00),
        (@Ord1Id, 5003, N'Інвертор Sevcon Gen4 Size 6 300V 400A', 1100.00, 1, 0.00);

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@Ord1Id, NULL, 'Submitted', N'ivan.zakharchuk', N'Створено нове замовлення через сайт');
END;

-- Замовлення 2: підтверджено на складі (StockConfirmed)
DECLARE @Cust2Id BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olena.petrenko@example.com');
IF @Cust2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CustomerId = @Cust2Id AND Status = 'StockConfirmed')
BEGIN
    INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
    VALUES (@Cust2Id, 'StockConfirmed', DATEADD(DAY, -5, SYSUTCDATETIME()), 3950.00, N'olena.petrenko');

    DECLARE @Ord2Id BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
    VALUES (@Ord2Id, N'м. Київ, вул. Хрещатик, 15', N'+380674445566', N'Volkswagen Golf Mk2 (1990)', N'Turnkey_Installation', N'Монтаж у нашому сертифікованому боксі');

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
    VALUES 
        (@Ord2Id, 5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 1, 0.00),
        (@Ord2Id, 5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 2, 0.00),
        (@Ord2Id, 5004, N'Комплект перехідної плити КПП BMW E30 / Getrag', 450.00, 1, 0.00);

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES 
        (@Ord2Id, NULL, 'Submitted', N'olena.petrenko', N'Створено замовлення'),
        (@Ord2Id, 'Submitted', 'StockConfirmed', N'warehouse-mgr', N'Комплектуючі перевірено та відкладено');
END;

-- Замовлення 3: оплачене (Paid)
DECLARE @Cust3Id BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'serhii.koval@example.com');
IF @Cust3Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CustomerId = @Cust3Id AND Status = 'Paid')
BEGIN
    INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
    VALUES (@Cust3Id, 'Paid', DATEADD(DAY, -10, SYSUTCDATETIME()), 1470.00, N'serhii.koval');

    DECLARE @Ord3Id BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
    VALUES (@Ord3Id, N'м. Львів, вул. Городоцька, 100', N'+380937778899', N'Lada Niva 4x4', N'DIY_Kit', N'Оплата карткою');

    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
    VALUES 
        (@Ord3Id, 5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 1, 0.00),
        (@Ord3Id, 5005, N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', 620.00, 1, 0.00);

    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES 
        (@Ord3Id, NULL, 'Submitted', N'serhii.koval', N'Створено замовлення'),
        (@Ord3Id, 'Submitted', 'StockConfirmed', N'warehouse-mgr', N'Наявність підтверджено'),
        (@Ord3Id, 'StockConfirmed', 'Paid', N'liqpay-gateway', N'Успішна оплата замовлення');
END;
GO
