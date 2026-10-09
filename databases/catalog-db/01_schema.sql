-- Лабораторна робота №1. База даних каталогу компонентів (Catalog Service)
-- Використовується для другого мікросервісу (в Лаб 3 сюди підключаємо EF Core Code First)

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'EVShop_Catalog')
BEGIN
    CREATE DATABASE EVShop_Catalog COLLATE Latin1_General_100_CI_AS_SC_UTF8;
END;
GO

USE EVShop_Catalog;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Очищення перед створенням
IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID(N'dbo.TechnicalSpecifications', N'U') IS NOT NULL DROP TABLE dbo.TechnicalSpecifications;
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
GO

-- 1. Виробники / бренди (зв'язок 1:N з товарами)
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

-- 2. Категорії товарів (зв'язок M:N з товарами)
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

-- 3. Товари (головна сутність каталогу)
-- Починаємо з 5001 для узгодження з локальною реплікою в замовленнях
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

-- 4. Проміжна таблиця категорій (зв'язок M:N з власними полями)
-- Містить власні атрибути: дату додавання та ознаку основної категорії
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

-- 5. Технічні характеристики (зв'язок 1:1 з товаром)
-- ProductId є водночас PK і FK
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

-- 6. Зображення товарів (зв'язок 1:N з товаром)
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
