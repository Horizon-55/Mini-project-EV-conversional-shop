-- ==========================================
-- 1. СХЕМА (DDL)
-- ==========================================
CREATE TABLE dbo.Customers (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsDeleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Orders (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    CustomerId BIGINT NOT NULL REFERENCES dbo.Customers(Id),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted' 
        CHECK (Status IN ('Submitted', 'AwaitingValidation', 'StockConfirmed', 'Paid', 'Shipped', 'Cancelled')),
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION 
);

CREATE TABLE dbo.OrderDetails (
    OrderId BIGINT PRIMARY KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ShippingAddress NVARCHAR(400) NOT NULL,
    ContactPhone NVARCHAR(20) NOT NULL
);

CREATE TABLE dbo.OrderItems (
    OrderId BIGINT NOT NULL REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductId BIGINT NOT NULL, 
    ProductName NVARCHAR(200) NOT NULL, 
    UnitPrice DECIMAL(18,2) NOT NULL CHECK (UnitPrice > 0),
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PRIMARY KEY (OrderId, ProductId)
);

CREATE INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status);
CREATE INDEX IX_Orders_Active ON dbo.Orders (CustomerId) WHERE IsDeleted = 0;
GO -- Розділювач пакетів для SQL Server

-- ==========================================
-- 2. ЗБЕРЕЖУВАНІ ПРОЦЕДУРИ (Крок 8)
-- ==========================================
CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; -- Будь-яка помилка автоматично відкочує транзакцію
    
    BEGIN TRANSACTION;
    
    -- UPDLOCK блокує рядок для інших транзакцій до завершення поточної
    DECLARE @Status NVARCHAR(30) = (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId);
    
    IF @Status IS NULL
        THROW 50001, 'Замовлення не знайдено', 1;
        
    IF @Status NOT IN ('Submitted', 'AwaitingValidation')
        THROW 50002, 'Підтвердити наявність можна лише для нового замовлення. Недопустимий перехід статусу.', 1;
        
    UPDATE dbo.Orders
    SET Status = 'StockConfirmed', UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;
    
    COMMIT TRANSACTION;
END;
GO

-- ==========================================
-- 3. ТЕСТОВІ ДАНІ (SEED - Крок 8)
-- ==========================================
-- Перевіряємо наявність перед вставкою, щоб не створювати дублі при повторному запуску скрипта
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'ivan.dev@example.com')
BEGIN
    INSERT INTO dbo.Customers (Email, FullName) 
    VALUES ('ivan.dev@example.com', 'Іван Захарчук');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE CustomerId = 1)
BEGIN
    INSERT INTO dbo.Orders (CustomerId, Status) 
    VALUES (1, 'Submitted');
    
    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone)
    VALUES (1, 'вул. Університетська, 28, Чернівці', '+380990000000');
    
    -- ProductId тут без FK на іншу базу, ми лише зберігаємо знімок на момент покупки
    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity)
    VALUES (1, 5001, 'Електромотор Nissan Leaf 80kW', 1200.00, 1);
END;
GO