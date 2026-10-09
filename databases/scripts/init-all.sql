-- Лабораторна робота №1. Головний SQL-скрипт ініціалізації
-- Розгортає дві незалежні бази даних:
-- 1. EVShop_Ordering (для сервісу замовлень під Dapper)
-- 2. EVShop_Catalog  (для сервісу каталогу під EF Core)

PRINT '--> 1. Ініціалізація бази даних замовлень (EVShop_Ordering)...';

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EVShop_Ordering')
BEGIN
    CREATE DATABASE EVShop_Ordering COLLATE Latin1_General_100_CI_AS_SC_UTF8;
END;
GO

USE EVShop_Ordering;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistory;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

-- Клієнти
CREATE TABLE dbo.Customers (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(25) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'admin',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Customers_Email UNIQUE NONCLUSTERED (Email)
);
GO

-- Локальна копія каталогу (без FK на іншу базу)
CREATE TABLE dbo.Products (
    Id BIGINT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    LastSyncedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Ordering_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CHK_Ordering_Products_Price CHECK (Price > 0),
    CONSTRAINT CHK_Ordering_Products_Stock CHECK (StockQuantity >= 0)
);
GO

-- Замовлення
CREATE TABLE dbo.Orders (
    Id BIGINT IDENTITY(1001,1) NOT NULL,
    CustomerId BIGINT NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted',
    OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'customer',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
    CONSTRAINT CHK_Orders_Status CHECK (Status IN ('Submitted', 'AwaitingValidation', 'StockConfirmed', 'Paid', 'Shipped', 'Cancelled')),
    CONSTRAINT CHK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);
GO

-- Деталі замовлення (1:1)
CREATE TABLE dbo.OrderDetails (
    OrderId BIGINT NOT NULL,
    ShippingAddress NVARCHAR(400) NOT NULL,
    ContactPhone NVARCHAR(25) NOT NULL,
    TargetVehicleModel NVARCHAR(150) NOT NULL,
    ConversionType NVARCHAR(50) NOT NULL DEFAULT 'DIY_Kit',
    InstallationNotes NVARCHAR(1000) NULL,
    CONSTRAINT PK_OrderDetails PRIMARY KEY CLUSTERED (OrderId),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);
GO

-- Позиції замовлення (M:N зі знімком цін)
CREATE TABLE dbo.OrderItems (
    OrderId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    LineTotal AS (CAST((Quantity * UnitPrice - Discount) AS DECIMAL(18,2))) PERSISTED,
    CONSTRAINT PK_OrderItems PRIMARY KEY CLUSTERED (OrderId, ProductId),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id),
    CONSTRAINT CHK_OrderItems_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT CHK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CHK_OrderItems_Discount CHECK (Discount >= 0)
);
GO

-- Аудит статусів
CREATE TABLE dbo.OrderStatusHistory (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    OrderId BIGINT NOT NULL,
    OldStatus NVARCHAR(30) NULL,
    NewStatus NVARCHAR(30) NOT NULL,
    ChangedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ChangedBy NVARCHAR(100) NOT NULL DEFAULT 'system',
    Comment NVARCHAR(500) NULL,
    CONSTRAINT PK_OrderStatusHistory PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE
);
GO

-- Індекси Ordering DB
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status) INCLUDE (TotalAmount, OrderDate);
CREATE NONCLUSTERED INDEX IX_Orders_Active ON dbo.Orders (CustomerId, OrderDate DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId) INCLUDE (UnitPrice, Quantity);
CREATE NONCLUSTERED INDEX IX_OrderStatusHistory_OrderId_Date ON dbo.OrderStatusHistory (OrderId, ChangedAt DESC);
GO

-- Процедури Ordering DB
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
        THROW 50010, 'Клієнта не знайдено', 1;
    BEGIN TRANSACTION;
    INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
    VALUES (@CustomerId, 'Submitted', SYSUTCDATETIME(), 0.00, @CreatedBy);
    SET @NewOrderId = SCOPE_IDENTITY();
    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
    VALUES (@NewOrderId, @ShippingAddress, @ContactPhone, @TargetVehicleModel, @ConversionType, @InstallationNotes);
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@NewOrderId, NULL, 'Submitted', @CreatedBy, 'Створено нове замовлення');
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AddOrderItem
    @OrderId BIGINT,
    @ProductId BIGINT,
    @Quantity INT,
    @Discount DECIMAL(18,2) = 0.00
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Quantity <= 0 THROW 50020, 'Кількість повинна бути більшою за 0', 1;
    BEGIN TRANSACTION;
    DECLARE @OrderStatus NVARCHAR(30);
    SELECT @OrderStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;
    IF @OrderStatus IS NULL THROW 50001, 'Замовлення не знайдено', 1;
    IF @OrderStatus <> 'Submitted' THROW 50021, 'Редагувати можна лише замовлення у статусі Submitted', 1;
    DECLARE @ProdName NVARCHAR(200);
    DECLARE @UnitPrice DECIMAL(18,2);
    SELECT @ProdName = Name, @UnitPrice = Price FROM dbo.Products WHERE Id = @ProductId;
    IF @ProdName IS NULL THROW 50022, 'Товар не знайдено в каталозі', 1;
    IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId AND ProductId = @ProductId)
        UPDATE dbo.OrderItems SET Quantity = Quantity + @Quantity, Discount = @Discount WHERE OrderId = @OrderId AND ProductId = @ProductId;
    ELSE
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
        VALUES (@OrderId, @ProductId, @ProdName, @UnitPrice, @Quantity, @Discount);
    UPDATE dbo.Orders
    SET TotalAmount = (SELECT ISNULL(SUM(LineTotal), 0.00) FROM dbo.OrderItems WHERE OrderId = @OrderId),
        UpdatedAt = SYSUTCDATETIME()
    WHERE Id = @OrderId;
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'manager'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;
    IF @CurrentStatus IS NULL THROW 50001, 'Замовлення не знайдено', 1;
    IF @CurrentStatus NOT IN ('Submitted', 'AwaitingValidation')
        THROW 50002, 'Підтвердити наявність можна лише для нового замовлення (Submitted/AwaitingValidation)', 1;
    UPDATE dbo.Orders SET Status = 'StockConfirmed', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy WHERE Id = @OrderId;
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'StockConfirmed', @UpdatedBy, 'Комплектуючі перевірено та зарезервовано');
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_PayOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'payment-gateway'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;
    IF @CurrentStatus IS NULL THROW 50001, 'Замовлення не знайдено', 1;
    IF @CurrentStatus <> 'StockConfirmed'
        THROW 50003, 'Оплатити можна лише замовлення з підтвердженою наявністю (StockConfirmed)', 1;
    UPDATE dbo.Orders SET Status = 'Paid', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy WHERE Id = @OrderId;
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Paid', @UpdatedBy, 'Оплату підтверджено');
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ShipOrder
    @OrderId BIGINT,
    @UpdatedBy NVARCHAR(100) = 'warehouse'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;
    IF @CurrentStatus IS NULL THROW 50001, 'Замовлення не знайдено', 1;
    IF @CurrentStatus <> 'Paid'
        THROW 50004, 'Неможливо відвантажити неоплачене замовлення!', 1;
    UPDATE dbo.Orders SET Status = 'Shipped', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy WHERE Id = @OrderId;
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Shipped', @UpdatedBy, 'Відвантажено перевізником');
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_CancelOrder
    @OrderId BIGINT,
    @CancellationReason NVARCHAR(500) = 'Скасовано замовником',
    @UpdatedBy NVARCHAR(100) = 'customer'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;
    DECLARE @CurrentStatus NVARCHAR(30);
    SELECT @CurrentStatus = Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId;
    IF @CurrentStatus IS NULL THROW 50001, 'Замовлення не знайдено', 1;
    IF @CurrentStatus IN ('Paid', 'Shipped')
        THROW 50005, 'Неможливо скасувати замовлення після оплати чи відвантаження', 1;
    IF @CurrentStatus = 'Cancelled'
        THROW 50006, 'Замовлення вже скасовано', 1;
    UPDATE dbo.Orders SET Status = 'Cancelled', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy WHERE Id = @OrderId;
    INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
    VALUES (@OrderId, @CurrentStatus, 'Cancelled', @UpdatedBy, @CancellationReason);
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetOrderDetailsById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.Id, o.CustomerId, c.FullName AS CustomerName, c.Email AS CustomerEmail, o.Status, o.OrderDate, o.TotalAmount, o.CreatedAt, o.UpdatedAt, o.RowVersion, d.ShippingAddress, d.ContactPhone, d.TargetVehicleModel, d.ConversionType, d.InstallationNotes
    FROM dbo.Orders o
    INNER JOIN dbo.Customers c ON o.CustomerId = c.Id
    INNER JOIN dbo.OrderDetails d ON o.Id = d.OrderId
    WHERE o.Id = @OrderId AND o.IsDeleted = 0;

    SELECT i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.Discount, i.LineTotal
    FROM dbo.OrderItems i WHERE i.OrderId = @OrderId;

    SELECT h.Id, h.OldStatus, h.NewStatus, h.ChangedAt, h.ChangedBy, h.Comment
    FROM dbo.OrderStatusHistory h WHERE h.OrderId = @OrderId ORDER BY h.ChangedAt ASC;
END;
GO

-- Seed Ordering DB
INSERT INTO dbo.Customers (Email, FullName, PhoneNumber, CreatedBy)
VALUES 
    (N'ivan.zakharchuk@example.com', N'Іван Захарчук', N'+380501112233', N'admin'),
    (N'olena.petrenko@example.com', N'Олена Петренко', N'+380674445566', N'admin'),
    (N'serhii.koval@example.com', N'Сергій Коваль', N'+380937778899', N'admin');

INSERT INTO dbo.Products (Id, Name, Price, StockQuantity)
VALUES 
    (5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 8),
    (5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 24),
    (5003, N'Інвертор Sevcon Gen4 Size 6 300V 400A', 1100.00, 5),
    (5004, N'Комплект перехідної плити КПП BMW E30 / Getrag', 450.00, 12),
    (5005, N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', 620.00, 15);

INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
VALUES (1, 'Submitted', DATEADD(DAY, -2, SYSUTCDATETIME()), 2500.00, N'ivan.zakharchuk');
INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
VALUES (1001, N'м. Чернівці, вул. Університетська, 28', N'+380501112233', N'BMW E30 (1988) Coupe', N'DIY_Kit', N'Консультація');
INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
VALUES (1001, 5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 1, 0.00),
       (1001, 5003, N'Інвертор Sevcon Gen4 Size 6 300V 400A', 1100.00, 1, 0.00);
INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
VALUES (1001, NULL, 'Submitted', N'ivan.zakharchuk', N'Створено замовлення');

INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
VALUES (2, 'StockConfirmed', DATEADD(DAY, -5, SYSUTCDATETIME()), 3950.00, N'olena.petrenko');
INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
VALUES (1002, N'м. Київ, вул. Хрещатик, 15', N'+380674445566', N'Volkswagen Golf Mk2 (1990)', N'Turnkey_Installation', N'Монтаж у боксі');
INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
VALUES (1002, 5001, N'Електромотор Nissan Leaf EM57 80kW', 1400.00, 1, 0.00),
       (1002, 5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 2, 0.00),
       (1002, 5004, N'Комплект перехідної плити КПП BMW E30 / Getrag', 450.00, 1, 0.00);
INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
VALUES (1002, NULL, 'Submitted', N'olena.petrenko', N'Створено замовлення'),
       (1002, 'Submitted', 'StockConfirmed', N'warehouse-mgr', N'Комплектуючі перевірено');

INSERT INTO dbo.Orders (CustomerId, Status, OrderDate, TotalAmount, CreatedBy)
VALUES (3, 'Paid', DATEADD(DAY, -10, SYSUTCDATETIME()), 1470.00, N'serhii.koval');
INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, ContactPhone, TargetVehicleModel, ConversionType, InstallationNotes)
VALUES (1003, N'м. Львів, вул. Городоцька, 100', N'+380937778899', N'Lada Niva 4x4', N'DIY_Kit', N'Оплачено');
INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, Discount)
VALUES (1003, 5002, N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 850.00, 1, 0.00),
       (1003, 5005, N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', 620.00, 1, 0.00);
INSERT INTO dbo.OrderStatusHistory (OrderId, OldStatus, NewStatus, ChangedBy, Comment)
VALUES (1003, NULL, 'Submitted', N'serhii.koval', N'Створено замовлення'),
       (1003, 'Submitted', 'StockConfirmed', N'warehouse-mgr', N'Наявність підтверджено'),
       (1003, 'StockConfirmed', 'Paid', N'liqpay-gateway', N'Оплату підтверджено');
GO

PRINT '--> 2. Ініціалізація бази даних каталогу (EVShop_Catalog)...';

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EVShop_Catalog')
BEGIN
    CREATE DATABASE EVShop_Catalog COLLATE Latin1_General_100_CI_AS_SC_UTF8;
END;
GO

USE EVShop_Catalog;
GO

IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID(N'dbo.TechnicalSpecifications', N'U') IS NOT NULL DROP TABLE dbo.TechnicalSpecifications;
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
GO

CREATE TABLE dbo.Brands (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Country NVARCHAR(100) NOT NULL,
    Website NVARCHAR(250) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Brands PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Brands_Name UNIQUE NONCLUSTERED (Name)
);
GO

CREATE TABLE dbo.Categories (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Categories_Name UNIQUE NONCLUSTERED (Name),
    CONSTRAINT UQ_Categories_Slug UNIQUE NONCLUSTERED (Slug)
);
GO

CREATE TABLE dbo.Products (
    Id BIGINT IDENTITY(5001,1) NOT NULL,
    BrandId BIGINT NOT NULL,
    SKU NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    RowVersion ROWVERSION NOT NULL,
    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands (Id),
    CONSTRAINT UQ_Products_SKU UNIQUE NONCLUSTERED (SKU),
    CONSTRAINT CHK_Products_BasePrice CHECK (BasePrice > 0),
    CONSTRAINT CHK_Products_StockQuantity CHECK (StockQuantity >= 0)
);
GO

CREATE TABLE dbo.ProductCategories (
    ProductId BIGINT NOT NULL,
    CategoryId BIGINT NOT NULL,
    DateAdded DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_ProductCategories PRIMARY KEY CLUSTERED (ProductId, CategoryId),
    CONSTRAINT FK_ProductCategories_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductCategories_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id) ON DELETE CASCADE
);
GO

CREATE TABLE dbo.TechnicalSpecifications (
    ProductId BIGINT NOT NULL,
    OperatingVoltageV INT NOT NULL,
    PeakPowerKW DECIMAL(6,2) NULL,
    ContinuousPowerKW DECIMAL(6,2) NULL,
    BatteryCapacityKWh DECIMAL(6,2) NULL,
    WeightKg DECIMAL(6,2) NOT NULL,
    CoolingType NVARCHAR(50) NOT NULL DEFAULT 'Air',
    DimensionsMm NVARCHAR(100) NULL,
    CompatibilityNotes NVARCHAR(500) NULL,
    CONSTRAINT PK_TechnicalSpecifications PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT FK_TechnicalSpecifications_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT CHK_TechSpecs_Voltage CHECK (OperatingVoltageV > 0),
    CONSTRAINT CHK_TechSpecs_Weight CHECK (WeightKg > 0)
);
GO

CREATE TABLE dbo.ProductImages (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    ProductId BIGINT NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    AltText NVARCHAR(200) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsThumbnail BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_ProductImages PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX IX_Products_Available_Price ON dbo.Products (IsAvailable, BasePrice ASC) INCLUDE (Name, SKU, StockQuantity, BrandId);
CREATE NONCLUSTERED INDEX IX_ProductCategories_CategoryId ON dbo.ProductCategories (CategoryId, IsPrimary DESC) INCLUDE (ProductId, DateAdded);
CREATE NONCLUSTERED INDEX IX_TechSpecs_Voltage_Power ON dbo.TechnicalSpecifications (OperatingVoltageV, PeakPowerKW);
CREATE NONCLUSTERED INDEX IX_ProductImages_ProductId_Thumbnail ON dbo.ProductImages (ProductId, IsThumbnail DESC, DisplayOrder ASC) INCLUDE (ImageUrl, AltText);
GO

-- Seed Catalog DB
INSERT INTO dbo.Brands (Name, Country, Website) VALUES 
    (N'Nissan OEM', N'Japan', N'https://www.nissan-global.com'),
    (N'CATL', N'China', N'https://www.catl.com'),
    (N'Sevcon / BorgWarner', N'USA', N'https://www.borgwarner.com'),
    (N'EV Retrofit Lab', N'Ukraine', N'https://evretrofit.ua'),
    (N'Phoenix Contact', N'Germany', N'https://www.phoenixcontact.com');

INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) VALUES 
    (N'Електродвигуни', N'electric-motors', N'Синхронні та асинхронні тягові двигуни', 1),
    (N'Тягові батареї та модулі', N'battery-modules', N'Li-ion та LFP модулі з BMS', 2),
    (N'Інвертори та контролери', N'inverters-controllers', N'Силові інвертори керування', 3),
    (N'Перехідні плити та муфти', N'adapter-plates', N'Адаптери під штатні КПП', 4),
    (N'Зарядні порти та станції', N'charging-systems', N'Порти Type 2, CCS Combo 2', 5);

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity) VALUES 
    (5001, 1, N'MOT-EM57-80KW', N'Електромотор Nissan Leaf EM57 80kW', N'Оригінальний синхронний мотор 80kW', 1400.00, 8),
    (5002, 2, N'BAT-CATL-12S-120AH', N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', N'Модуль Li-ion високої щільності', 850.00, 24),
    (5003, 3, N'INV-SEV-G4-S6', N'Інвертор Sevcon Gen4 Size 6 300V 400A', N'Високовольтний програмований інвертор', 1100.00, 5),
    (5004, 4, N'ADP-BMW-E30-EM57', N'Комплект перехідної плити КПП BMW E30 / Getrag', N'Алюмінієва перехідна плита Д16Т', 450.00, 12),
    (5005, 5, N'CHG-CCS2-150KW', N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', N'Комплект швидкої зарядки DC', 620.00, 15);
SET IDENTITY_INSERT dbo.Products OFF;

INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, PeakPowerKW, ContinuousPowerKW, WeightKg, CoolingType, DimensionsMm, CompatibilityNotes) VALUES 
    (5001, 360, 80.00, 45.00, 58.50, N'Liquid', N'340x310x290', N'Сумісний з інвертором Gen4'),
    (5002, 44, NULL, NULL, 29.80, N'Liquid', N'355x150x108', N'Збірка до 400V'),
    (5003, 300, 110.00, 60.00, 12.20, N'Liquid', N'280x210x95', N'Підтримка Sin/Cos енкодерів'),
    (5004, 12, NULL, NULL, 11.50, N'Passive', NULL, N'Getrag 240/260'),
    (5005, 500, NULL, NULL, 4.30, N'Passive', NULL, N'Швидка зарядка DC до 200A');

INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary) VALUES 
    (5001, 1, SYSUTCDATETIME(), 1),
    (5002, 2, SYSUTCDATETIME(), 1),
    (5003, 3, SYSUTCDATETIME(), 1),
    (5004, 4, SYSUTCDATETIME(), 1),
    (5005, 5, SYSUTCDATETIME(), 1);

INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail) VALUES 
    (5001, N'https://evshop.local/images/products/em57-front.webp', N'Nissan Leaf EM57', 1, 1),
    (5002, N'https://evshop.local/images/products/catl-module.webp', N'CATL 12S Module', 1, 1),
    (5003, N'https://evshop.local/images/products/sevcon-g4-s6.webp', N'Sevcon Inverter', 1, 1),
    (5004, N'https://evshop.local/images/products/plate-bmw-e30.webp', N'BMW E30 Adapter Plate', 1, 1),
    (5005, N'https://evshop.local/images/products/ccs2-port.webp', N'CCS2 Inlet Port', 1, 1);
GO

PRINT '--> Бази даних успішно розгорнуто!';
