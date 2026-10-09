-- Індекси для бази каталогу Catalog DB
-- Оптимізують операції вибірки, сортування та фільтрації товарів

USE EVShop_Catalog;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Сортування та пагінація доступних товарів за ціною (для каталогу на сайті)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Available_Price' AND object_id = OBJECT_ID(N'dbo.Products'))
    DROP INDEX IX_Products_Available_Price ON dbo.Products;
GO
CREATE NONCLUSTERED INDEX IX_Products_Available_Price 
ON dbo.Products (IsAvailable, BasePrice ASC)
INCLUDE (Name, SKU, StockQuantity, BrandId);
GO

-- 2. Отримання товарів за категорією з пріоритетом головної категорії
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductCategories_CategoryId' AND object_id = OBJECT_ID(N'dbo.ProductCategories'))
    DROP INDEX IX_ProductCategories_CategoryId ON dbo.ProductCategories;
GO
CREATE NONCLUSTERED INDEX IX_ProductCategories_CategoryId 
ON dbo.ProductCategories (CategoryId, IsPrimary DESC)
INCLUDE (ProductId, DateAdded);
GO

-- 3. Фільтрація деталей за електричними параметрами (напруга, потужність)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TechSpecs_Voltage_Power' AND object_id = OBJECT_ID(N'dbo.TechnicalSpecifications'))
    DROP INDEX IX_TechSpecs_Voltage_Power ON dbo.TechnicalSpecifications;
GO
CREATE NONCLUSTERED INDEX IX_TechSpecs_Voltage_Power 
ON dbo.TechnicalSpecifications (OperatingVoltageV, PeakPowerKW);
GO

-- 4. Отримання головної фотографії для прев'ю в картці товару
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProductImages_ProductId_Thumbnail' AND object_id = OBJECT_ID(N'dbo.ProductImages'))
    DROP INDEX IX_ProductImages_ProductId_Thumbnail ON dbo.ProductImages;
GO
CREATE NONCLUSTERED INDEX IX_ProductImages_ProductId_Thumbnail 
ON dbo.ProductImages (ProductId, IsThumbnail DESC, DisplayOrder ASC)
INCLUDE (ImageUrl, AltText);
GO
