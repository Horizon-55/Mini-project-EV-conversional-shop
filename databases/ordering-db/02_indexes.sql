-- Індекси для бази замовлень Ordering DB
-- Створюємо індекси під конкретні сценарії пошуку, щоб уникнути зайвого навантаження

USE EVShop_Ordering;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Для перегляду замовлень користувача за статусом (наприклад, активні замовлення в кабінеті)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId_Status' AND object_id = OBJECT_ID(N'dbo.Orders'))
    DROP INDEX IX_Orders_CustomerId_Status ON dbo.Orders;
GO
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_Status 
ON dbo.Orders (CustomerId, Status)
INCLUDE (TotalAmount, OrderDate);
GO

-- 2. Фільтрований індекс: швидка вибірка лише активних (не видалених через soft delete) замовлень
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Active' AND object_id = OBJECT_ID(N'dbo.Orders'))
    DROP INDEX IX_Orders_Active ON dbo.Orders;
GO
CREATE NONCLUSTERED INDEX IX_Orders_Active 
ON dbo.Orders (CustomerId, OrderDate DESC)
WHERE IsDeleted = 0;
GO

-- 3. Швидкий пошук замовлень за кодом товару
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    DROP INDEX IX_OrderItems_ProductId ON dbo.OrderItems;
GO
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId 
ON dbo.OrderItems (ProductId)
INCLUDE (UnitPrice, Quantity);
GO

-- 4. Хронологія змін статусів конкретного замовлення
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderStatusHistory_OrderId_Date' AND object_id = OBJECT_ID(N'dbo.OrderStatusHistory'))
    DROP INDEX IX_OrderStatusHistory_OrderId_Date ON dbo.OrderStatusHistory;
GO
CREATE NONCLUSTERED INDEX IX_OrderStatusHistory_OrderId_Date 
ON dbo.OrderStatusHistory (OrderId, ChangedAt DESC);
GO
