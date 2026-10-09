# ERD-діаграми баз даних мікросервісів

Цей документ містить діаграми зв'язків сутностей (ERD) для реляційних баз даних **Проєкту №1 (Ordering DB)** та **Проєкту №2 (Catalog DB)**.

Діаграми оформлені у двох форматах:
1. **Mermaid** (візуалізується прямо у GitHub / Markdown).
2. **DBML-код** (для копіювання в інструмент [dbdiagram.io](https://dbdiagram.io/)).

---

## 1. Проєкт №1: Ordering Service Database (ERD)

### Зв'язки у базі даних:
- **1:1** `Orders` $\leftrightarrow$ `OrderDetails` (`OrderId` є одночасно PK і FK).
- **1:N** `Customers` $\to$ `Orders` (Один покупець має багато замовлень).
- **M:N** `Orders` $\leftrightarrow$ `Products` через таблицю-міст `OrderItems` (із фіксацією `UnitPrice` і `ProductName` як знімка).
- **1:N** `Orders` $\to$ `OrderStatusHistory` (Аудит життєвого циклу).

### Mermaid ERD:

```mermaid
erDiagram
    CUSTOMERS ||--o{ ORDERS : "places (1:N)"
    ORDERS ||--|| ORDER_DETAILS : "has details (1:1)"
    ORDERS ||--o{ ORDER_ITEMS : "contains (1:N)"
    PRODUCTS_REPLICA ||--o{ ORDER_ITEMS : "referenced in (1:N)"
    ORDERS ||--o{ ORDER_STATUS_HISTORY : "tracks lifecycle (1:N)"

    CUSTOMERS {
        bigint Id PK
        nvarchar Email UK
        nvarchar FullName
        nvarchar PhoneNumber
        datetime2 CreatedAt
        bit IsDeleted
    }

    ORDERS {
        bigint Id PK
        bigint CustomerId FK
        nvarchar Status "CHECK: Submitted..Cancelled"
        datetime2 OrderDate
        decimal TotalAmount
        datetime2 CreatedAt
        bit IsDeleted
        rowversion RowVersion "Concurrency Token"
    }

    ORDER_DETAILS {
        bigint OrderId PK_FK
        nvarchar ShippingAddress
        nvarchar ContactPhone
        nvarchar TargetVehicleModel
        nvarchar ConversionType
        nvarchar InstallationNotes
    }

    PRODUCTS_REPLICA {
        bigint Id PK "Catalog ProductId (No Remote FK)"
        nvarchar Name
        decimal Price
        int StockQuantity
        datetime2 LastSyncedAt
    }

    ORDER_ITEMS {
        bigint OrderId PK_FK
        bigint ProductId PK_FK
        nvarchar ProductName "Immutable Snapshot"
        decimal UnitPrice "Immutable Snapshot"
        int Quantity
        decimal Discount
        decimal LineTotal "Persisted Formula"
    }

    ORDER_STATUS_HISTORY {
        bigint Id PK
        bigint OrderId FK
        nvarchar OldStatus
        nvarchar NewStatus
        datetime2 ChangedAt
        nvarchar ChangedBy
        nvarchar Comment
    }
```

### DBML Код для dbdiagram.io (Ordering DB):
```dbml
Table Customers {
  Id bigint [pk, increment]
  Email varchar(100) [unique, not null]
  FullName varchar(150) [not null]
  PhoneNumber varchar(25)
  CreatedAt datetime2 [not null]
  IsDeleted bit [not null, default: 0]
}

Table Orders {
  Id bigint [pk, increment]
  CustomerId bigint [not null, ref: > Customers.Id]
  Status varchar(30) [not null, default: 'Submitted']
  OrderDate datetime2 [not null]
  TotalAmount decimal(18,2) [not null, default: 0.00]
  CreatedAt datetime2 [not null]
  IsDeleted bit [not null, default: 0]
  RowVersion binary [not null]
}

Table OrderDetails {
  OrderId bigint [pk, ref: - Orders.Id]
  ShippingAddress varchar(400) [not null]
  ContactPhone varchar(25) [not null]
  TargetVehicleModel varchar(150) [not null]
  ConversionType varchar(50) [not null, default: 'DIY_Kit']
  InstallationNotes varchar(1000)
}

Table Products_Replica {
  Id bigint [pk, note: 'Matches Catalog.Products.Id, no FK constraint']
  Name varchar(200) [not null]
  Price decimal(18,2) [not null]
  StockQuantity int [not null, default: 0]
  LastSyncedAt datetime2 [not null]
}

Table OrderItems {
  OrderId bigint [ref: > Orders.Id]
  ProductId bigint [ref: > Products_Replica.Id]
  ProductName varchar(200) [not null, note: 'Snapshot on order placement']
  UnitPrice decimal(18,2) [not null, note: 'Snapshot on order placement']
  Quantity int [not null, default: 1]
  Discount decimal(18,2) [not null, default: 0.00]
  LineTotal decimal(18,2) [not null]
  indexes {
    (OrderId, ProductId) [pk]
  }
}

Table OrderStatusHistory {
  Id bigint [pk, increment]
  OrderId bigint [not null, ref: > Orders.Id]
  OldStatus varchar(30)
  NewStatus varchar(30) [not null]
  ChangedAt datetime2 [not null]
  ChangedBy varchar(100) [not null]
  Comment varchar(500)
}
```

---

## 2. Проєкт №2: Catalog Service Database (ERD)

### Зв'язки у базі даних:
- **1:1** `Products` $\leftrightarrow$ `TechnicalSpecifications` (`ProductId` є PK і FK).
- **1:N** `Brands` $\to$ `Products`.
- **1:N** `Products` $\to$ `ProductImages`.
- **M:N** `Products` $\leftrightarrow$ `Categories` через `ProductCategories` із власними полями `DateAdded` та `IsPrimary`.

### Mermaid ERD:

```mermaid
erDiagram
    BRANDS ||--o{ PRODUCTS : "manufactures (1:N)"
    PRODUCTS ||--|| TECHNICAL_SPECS : "has specs (1:1)"
    PRODUCTS ||--o{ PRODUCT_IMAGES : "has gallery (1:N)"
    PRODUCTS ||--o{ PRODUCT_CATEGORIES : "categorized in (M:N)"
    CATEGORIES ||--o{ PRODUCT_CATEGORIES : "contains (1:N)"

    BRANDS {
        bigint Id PK
        nvarchar Name UK
        nvarchar Country
        nvarchar Website
        bit IsActive
    }

    CATEGORIES {
        bigint Id PK
        nvarchar Name UK
        nvarchar Slug UK
        nvarchar Description
        int DisplayOrder
        bit IsActive
    }

    PRODUCTS {
        bigint Id PK
        bigint BrandId FK
        nvarchar SKU UK
        nvarchar Name
        nvarchar Description
        decimal BasePrice
        int StockQuantity
        bit IsAvailable
        datetime2 CreatedAt
        rowversion RowVersion
    }

    PRODUCT_CATEGORIES {
        bigint ProductId PK_FK
        bigint CategoryId PK_FK
        datetime2 DateAdded "Custom Field"
        bit IsPrimary "Custom Field"
    }

    TECHNICAL_SPECS {
        bigint ProductId PK_FK
        int OperatingVoltageV
        decimal PeakPowerKW
        decimal ContinuousPowerKW
        decimal BatteryCapacityKWh
        decimal WeightKg
        nvarchar CoolingType
        nvarchar DimensionsMm
        nvarchar CompatibilityNotes
    }

    PRODUCT_IMAGES {
        bigint Id PK
        bigint ProductId FK
        nvarchar ImageUrl
        nvarchar AltText
        int DisplayOrder
        bit IsThumbnail
    }
```

### DBML Код для dbdiagram.io (Catalog DB):
```dbml
Table Brands {
  Id bigint [pk, increment]
  Name varchar(100) [unique, not null]
  Country varchar(100) [not null]
  Website varchar(250)
  IsActive bit [not null, default: 1]
}

Table Categories {
  Id bigint [pk, increment]
  Name varchar(100) [unique, not null]
  Slug varchar(100) [unique, not null]
  Description varchar(500)
  DisplayOrder int [not null, default: 0]
  IsActive bit [not null, default: 1]
}

Table Products {
  Id bigint [pk, increment]
  BrandId bigint [not null, ref: > Brands.Id]
  SKU varchar(50) [unique, not null]
  Name varchar(200) [not null]
  Description text
  BasePrice decimal(18,2) [not null]
  StockQuantity int [not null, default: 0]
  IsAvailable bit [not null, default: 1]
  CreatedAt datetime2 [not null]
  RowVersion binary [not null]
}

Table ProductCategories {
  ProductId bigint [ref: > Products.Id]
  CategoryId bigint [ref: > Categories.Id]
  DateAdded datetime2 [not null]
  IsPrimary bit [not null, default: 0]
  indexes {
    (ProductId, CategoryId) [pk]
  }
}

Table TechnicalSpecifications {
  ProductId bigint [pk, ref: - Products.Id]
  OperatingVoltageV int [not null]
  PeakPowerKW decimal(6,2)
  ContinuousPowerKW decimal(6,2)
  BatteryCapacityKWh decimal(6,2)
  WeightKg decimal(6,2) [not null]
  CoolingType varchar(50) [not null, default: 'Air']
  DimensionsMm varchar(100)
  CompatibilityNotes varchar(500)
}

Table ProductImages {
  Id bigint [pk, increment]
  ProductId bigint [not null, ref: > Products.Id]
  ImageUrl varchar(500) [not null]
  AltText varchar(200)
  DisplayOrder int [not null, default: 0]
  IsThumbnail bit [not null, default: 0]
}
```
