-- Лабораторна робота №1. База даних сервісу замовлень (Ordering Service)
-- Використовується для першого мікросервісу (в Лаб 2 сюди підключаємо Dapper/ADO.NET)

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

-- Очищення перед створенням (для можливості повторного накатування)
IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistory;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

-- 1. Таблиця клієнтів (зв'язок 1:N із замовленнями)
CREATE TABLE dbo.Customers (
    Id BIGINT IDENTITY(1,1) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(25) NULL,
    
    -- поля аудиту та м'якого видалення
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'admin',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Customers_Email UNIQUE NONCLUSTERED (Email)
);
GO

-- 2. Локальна копія товарів (репліка з каталогу)
-- Зверніть увагу: без FK на іншу базу, щоб сервіси були автономними
CREATE TABLE dbo.Products (
    Id BIGINT NOT NULL, -- ID збігається з Catalog.Products.Id
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL DEFAULT 0,
    LastSyncedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    CONSTRAINT PK_Ordering_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CHK_Ordering_Products_Price CHECK (Price > 0),
    CONSTRAINT CHK_Ordering_Products_Stock CHECK (StockQuantity >= 0)
);
GO

-- 3. Замовлення (головна таблиця агрегату)
CREATE TABLE dbo.Orders (
    Id BIGINT IDENTITY(1001,1) NOT NULL,
    CustomerId BIGINT NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Submitted',
    OrderDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    
    -- аудит та токен оптимістичного блокування
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'customer',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION NOT NULL,
    
    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
    CONSTRAINT CHK_Orders_Status CHECK (Status IN (
        'Submitted',
        'AwaitingValidation',
        'StockConfirmed',
        'Paid',
        'Shipped',
        'Cancelled'
    )),
    CONSTRAINT CHK_Orders_TotalAmount CHECK (TotalAmount >= 0)
);
GO

-- 4. Деталі замовлення (зв'язок 1:1 з Orders)
-- OrderId одночасно є PK і FK
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

-- 5. Позиції замовлення (зв'язок M:N між Orders та Products)
-- ProductName і UnitPrice фіксуються як незмінний знімок на момент оформлення
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

-- 6. Історія зміни статусів (для аудиту життєвого циклу)
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
