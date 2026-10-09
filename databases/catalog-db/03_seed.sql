-- Початкові довідникові дані для каталогу Catalog DB
-- Перевіряємо IF NOT EXISTS, щоб уникнути дублювання при повторному накатуванні

USE EVShop_Catalog;
GO

-- 1. Бренди / виробники
IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Nissan OEM')
    INSERT INTO dbo.Brands (Name, Country, Website) VALUES (N'Nissan OEM', N'Japan', N'https://www.nissan-global.com');

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'CATL')
    INSERT INTO dbo.Brands (Name, Country, Website) VALUES (N'CATL', N'China', N'https://www.catl.com');

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Sevcon / BorgWarner')
    INSERT INTO dbo.Brands (Name, Country, Website) VALUES (N'Sevcon / BorgWarner', N'USA', N'https://www.borgwarner.com');

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'EV Retrofit Lab')
    INSERT INTO dbo.Brands (Name, Country, Website) VALUES (N'EV Retrofit Lab', N'Ukraine', N'https://evretrofit.ua');

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Name = N'Phoenix Contact')
    INSERT INTO dbo.Brands (Name, Country, Website) VALUES (N'Phoenix Contact', N'Germany', N'https://www.phoenixcontact.com');
GO

-- 2. Категорії
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'electric-motors')
    INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) 
    VALUES (N'Електродвигуни', N'electric-motors', N'Синхронні та асинхронні тягові двигуни для авто', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'battery-modules')
    INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) 
    VALUES (N'Тягові батареї та модулі', N'battery-modules', N'Li-ion та LFP модулі з системами BMS', 2);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'inverters-controllers')
    INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) 
    VALUES (N'Інвертори та контролери', N'inverters-controllers', N'Силові інвертори керування електродвигуном', 3);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'adapter-plates')
    INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) 
    VALUES (N'Перехідні плити та муфти', N'adapter-plates', N'Адаптери під штатні механічні коробки передач', 4);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'charging-systems')
    INSERT INTO dbo.Categories (Name, Slug, Description, DisplayOrder) 
    VALUES (N'Зарядні порти та станції', N'charging-systems', N'Порти Type 2, CCS Combo 2 та контролери', 5);
GO

-- 3. Товари, характеристики, категорії та фото
DECLARE @BrandNissan BIGINT = (SELECT Id FROM dbo.Brands WHERE Name = N'Nissan OEM');
DECLARE @BrandCATL BIGINT = (SELECT Id FROM dbo.Brands WHERE Name = N'CATL');
DECLARE @BrandSevcon BIGINT = (SELECT Id FROM dbo.Brands WHERE Name = N'Sevcon / BorgWarner');
DECLARE @BrandRetrofit BIGINT = (SELECT Id FROM dbo.Brands WHERE Name = N'EV Retrofit Lab');
DECLARE @BrandPhoenix BIGINT = (SELECT Id FROM dbo.Brands WHERE Name = N'Phoenix Contact');

DECLARE @CatMotors BIGINT = (SELECT Id FROM dbo.Categories WHERE Slug = N'electric-motors');
DECLARE @CatBatteries BIGINT = (SELECT Id FROM dbo.Categories WHERE Slug = N'battery-modules');
DECLARE @CatInverters BIGINT = (SELECT Id FROM dbo.Categories WHERE Slug = N'inverters-controllers');
DECLARE @CatAdapters BIGINT = (SELECT Id FROM dbo.Categories WHERE Slug = N'adapter-plates');
DECLARE @CatCharging BIGINT = (SELECT Id FROM dbo.Categories WHERE Slug = N'charging-systems');

-- Товар 5001: Електромотор Nissan Leaf EM57
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'MOT-EM57-80KW')
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity)
    VALUES (5001, @BrandNissan, N'MOT-EM57-80KW', N'Електромотор Nissan Leaf EM57 80kW', 
            N'Синхронний тяговий мотор змінного струму. Підходить для авто масою до 1600 кг.', 
            1400.00, 8);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, PeakPowerKW, ContinuousPowerKW, WeightKg, CoolingType, DimensionsMm, CompatibilityNotes)
    VALUES (5001, 360, 80.00, 45.00, 58.50, N'Liquid', N'340x310x290', N'Сумісний з інвертором Sevcon Gen4');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary)
    VALUES (5001, @CatMotors, SYSUTCDATETIME(), 1);

    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail)
    VALUES 
        (5001, N'https://evshop.local/images/products/em57-front.webp', N'Nissan Leaf EM57 Front View', 1, 1),
        (5001, N'https://evshop.local/images/products/em57-shaft.webp', N'Shaft splines close-up', 2, 0);
END;

-- Товар 5002: Модуль батареї CATL
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'BAT-CATL-12S-120AH')
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity)
    VALUES (5002, @BrandCATL, N'BAT-CATL-12S-120AH', N'Батарейний модуль CATL 12S 44V 120Ah 5.3kWh', 
            N'Li-ion модуль високої щільності з датчиками температури та шинами балансування.', 
            850.00, 24);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, BatteryCapacityKWh, WeightKg, CoolingType, DimensionsMm, CompatibilityNotes)
    VALUES (5002, 44, 5.28, 29.80, N'Liquid', N'355x150x108', N'Збірка у блоки до 400V (8-9 модулів)');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary)
    VALUES (5002, @CatBatteries, SYSUTCDATETIME(), 1);

    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail)
    VALUES 
        (5002, N'https://evshop.local/images/products/catl-module.webp', N'CATL 12S Battery Module', 1, 1);
END;

-- Товар 5003: Інвертор Sevcon Gen4
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'INV-SEV-G4-S6')
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity)
    VALUES (5003, @BrandSevcon, N'INV-SEV-G4-S6', N'Інвертор Sevcon Gen4 Size 6 300V 400A', 
            N'Програмований високовольтний інвертор з шиною CAN-bus.', 
            1100.00, 5);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, PeakPowerKW, ContinuousPowerKW, WeightKg, CoolingType, DimensionsMm, CompatibilityNotes)
    VALUES (5003, 300, 110.00, 60.00, 12.20, N'Liquid', N'280x210x95', N'Підтримка Sin/Cos енкодерів');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary)
    VALUES (5003, @CatInverters, SYSUTCDATETIME(), 1);

    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail)
    VALUES 
        (5003, N'https://evshop.local/images/products/sevcon-g4-s6.webp', N'Sevcon Gen4 Inverter', 1, 1);
END;

-- Товар 5004: Перехідна плита BMW E30
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'ADP-BMW-E30-EM57')
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity)
    VALUES (5004, @BrandRetrofit, N'ADP-BMW-E30-EM57', N'Комплект перехідної плити КПП BMW E30 / Getrag', 
            N'Фрезерована плита з авіаційного алюмінію Д16Т з центруючими втулками.', 
            450.00, 12);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, WeightKg, CoolingType, CompatibilityNotes)
    VALUES (5004, 12, 11.50, N'Passive', N'КПП Getrag 240 / 260');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary)
    VALUES (5004, @CatAdapters, SYSUTCDATETIME(), 1);

    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail)
    VALUES 
        (5004, N'https://evshop.local/images/products/plate-bmw-e30.webp', N'CNC Adapter Plate BMW E30', 1, 1);
END;

-- Товар 5005: Порт CCS Combo 2
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'CHG-CCS2-150KW')
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, SKU, Name, Description, BasePrice, StockQuantity)
    VALUES (5005, @BrandPhoenix, N'CHG-CCS2-150KW', N'Порт швидкої зарядки CCS Combo 2 150kW + Контролер', 
            N'Комплект DC зарядки з блокуванням штекера.', 
            620.00, 15);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.TechnicalSpecifications (ProductId, OperatingVoltageV, WeightKg, CoolingType, CompatibilityNotes)
    VALUES (5005, 500, 4.30, N'Passive', N'Сумісний зі станціями швидкої зарядки DC до 200A');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, DateAdded, IsPrimary)
    VALUES (5005, @CatCharging, SYSUTCDATETIME(), 1);

    INSERT INTO dbo.ProductImages (ProductId, ImageUrl, AltText, DisplayOrder, IsThumbnail)
    VALUES 
        (5005, N'https://evshop.local/images/products/ccs2-port.webp', N'CCS2 Inlet Port', 1, 1);
END;
GO
