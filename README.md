# Лабораторна робота №1: Проєктування та розробка баз даних для мікросервісної архітектури

**Дисципліна:** Технології .NET розробки Web-додатків / Розробка додатків на платформі .NET  
**Предметна область:** EV Conversional Shop (магазин компонентів та послуг переобладнання авто в електромобілі)  
**Гілка роботи:** `lab-1`

---

## 1. Короткий опис системи та контекстів

Для побудови системи обрано мікросервісний підхід із шаблоном **Database-per-Service** (кожен сервіс має власну ізольовану БД, міжсервісні FOREIGN KEY відсутні).

1. **Проєкт №1 — Ordering Service (Замовлення):**
   - **СУБД:** SQL Server 2022 (`EVShop_Ordering`).
   - **Технологія для Лаб 2:** ADO.NET + Dapper.
   - **Зв'язки:** 1:1 (`Orders` $\leftrightarrow$ `OrderDetails`), 1:N (`Customers` $\to$ `Orders`), M:N (`Orders` $\leftrightarrow$ `Products` через `OrderItems`).
   - **Функціонал:** 6 збережуваних процедур з транзакціями та блокуванням `WITH (UPDLOCK)` для безпечної зміни статусів замовлення, аудитні поля, soft delete, `RowVersion`.
2. **Проєкт №2 — Catalog Service (Каталог):**
   - **СУБД:** SQL Server 2022 (`EVShop_Catalog`).
   - **Технологія для Лаб 3:** EF Core (Code First + Fluent API).
   - **Зв'язки:** 1:1 (`Products` $\leftrightarrow$ `TechnicalSpecifications`), 1:N (`Brands` $\to$ `Products`, `Products` $\to$ `ProductImages`), M:N (`Products` $\leftrightarrow$ `Categories` через `ProductCategories` із власними полями `DateAdded` та `IsPrimary`).
3. **Проєкт №3 — Reviews & Community Service (Відгуки та спільнота):**
   - **СУБД:** MongoDB 7.0 (`evshop_reviews`).
   - **Технологія для Лаб 4:** MongoDB C# Driver.
   - **Колекції:** `reviews`, `discussions`, `conversion_showcases`.
   - **Особливості:** валідація схеми JSON Schema, embedding (вкладені коментарі), referencing (посилання на теми), поліморфні документи (різні набори полів для двигунів, акумуляторів, перехідних плит).

---

## 2. Політика дублювання даних між сервісами

| Дубльоване поле | Де зберігається | Сервіс-власник | Політика узгодженості |
| :--- | :--- | :--- | :--- |
| `OrderItems.ProductName`, `UnitPrice` | `EVShop_Ordering` | Каталог (`Products`) | **Snapshot (знімок):** Фіксується в момент покупки та не змінюється при подальшому редагуванні цін у каталозі. |
| `Products.Name`, `Price` (репліка) | `EVShop_Ordering` | Каталог (`Products`) | **Eventual Consistency:** Локальний довідник для швидкої перевірки та оформлення замовлення (актуалізується подіями). |
| `reviews.productName` | `evshop_reviews` | Каталог (`Products`) | **Read Projection:** Денормалізоване поле для показу карток відгуків без запитів до каталогу. |
| `reviews.author.displayName` | `evshop_reviews` | Замовлення / Клієнти | **Read Projection:** Кешується ім'я автора відгуку. |

---

## 3. Як розгорнути проєкт

Усі бази розгортаються локально в Docker без необхідності встановлення СКБД на систему.

### Крок 1. Запуск контейнерів
```bash
docker compose up -d
```
Служби:
- **SQL Server 2022:** `localhost:1433` (користувач `sa`, пароль `StrongPassw0rd!EV2026`)
- **MongoDB 7.0:** `localhost:27017` (користувач `admin`, пароль `AdminPassword123!`)
- **Mongo Express (Web GUI):** [http://localhost:8081](http://localhost:8081)

### Крок 2. Накатування схем та тестових даних
Виконайте скрипт автоматичного накатування та самоперевірки:
```powershell
powershell -ExecutionPolicy Bypass -File databases/scripts/run-init.ps1
```

Або вручну виконайте `init.sql` (у корені репозиторію) у будь-якому клієнті для SQL Server (SSMS / DBeaver), а файли з папки `databases/reviews-db/` у клієнті `mongosh`.

---

## 4. Перевірка роботи та критеріїв оцінювання

1. **Перевірка відсутності зовнішніх ключів між різними базами:**
   ```sql
   USE EVShop_Ordering;
   SELECT name, OBJECT_NAME(parent_object_id) AS ParentTable, OBJECT_NAME(referenced_object_id) AS RefTable 
   FROM sys.foreign_keys;
   ```
   Усі зв'язки ведуть тільки на таблиці поточної бази даних.

2. **Перевірка бізнес-правил (заборона відвантаження неоплаченого замовлення):**
   ```sql
   USE EVShop_Ordering;
   EXEC dbo.usp_ShipOrder @OrderId = 1001;
   -- Повертає керовану помилку THROW 50004
   ```

3. **Перевірка динамічних полів у MongoDB:**
   ```javascript
   use evshop_reviews;
   db.reviews.find({ dynoTestedTorqueNm: { $exists: true } });
   ```

---

## 5. ERD-діаграми баз даних

### Проєкт №1: Ordering DB (SQL Server)
```mermaid
erDiagram
    CUSTOMERS ||--o{ ORDERS : "1:N"
    ORDERS ||--|| ORDER_DETAILS : "1:1"
    ORDERS ||--o{ ORDER_ITEMS : "1:N"
    PRODUCTS_REPLICA ||--o{ ORDER_ITEMS : "1:N"
    ORDERS ||--o{ ORDER_STATUS_HISTORY : "1:N"

    CUSTOMERS {
        bigint Id PK
        nvarchar Email UK
        nvarchar FullName
    }
    ORDERS {
        bigint Id PK
        bigint CustomerId FK
        nvarchar Status
        decimal TotalAmount
        rowversion RowVersion
    }
    ORDER_DETAILS {
        bigint OrderId PK_FK
        nvarchar ShippingAddress
        nvarchar TargetVehicleModel
    }
    ORDER_ITEMS {
        bigint OrderId PK_FK
        bigint ProductId PK_FK
        nvarchar ProductName "Snapshot"
        decimal UnitPrice "Snapshot"
        int Quantity
    }
    PRODUCTS_REPLICA {
        bigint Id PK "No Remote FK"
        nvarchar Name
        decimal Price
    }
    ORDER_STATUS_HISTORY {
        bigint Id PK
        bigint OrderId FK
        nvarchar OldStatus
        nvarchar NewStatus
    }
```

### Проєкт №2: Catalog DB (SQL Server)
```mermaid
erDiagram
    BRANDS ||--o{ PRODUCTS : "1:N"
    PRODUCTS ||--|| TECHNICAL_SPECS : "1:1"
    PRODUCTS ||--o{ PRODUCT_IMAGES : "1:N"
    PRODUCTS ||--o{ PRODUCT_CATEGORIES : "M:N"
    CATEGORIES ||--o{ PRODUCT_CATEGORIES : "1:N"

    BRANDS {
        bigint Id PK
        nvarchar Name UK
    }
    PRODUCTS {
        bigint Id PK
        bigint BrandId FK
        nvarchar SKU UK
        nvarchar Name
        decimal BasePrice
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
        decimal BatteryCapacityKWh
    }
    PRODUCT_IMAGES {
        bigint Id PK
        bigint ProductId FK
        nvarchar ImageUrl
    }
```

Код схем для [dbdiagram.io](https://dbdiagram.io/) знаходиться у файлі [docs/erd-diagrams.md](file:///k:/ЧНУ%20лаби%204%20курс/Лаби%20з%20.NET%20інженерії/Mini-project-EV-conversional-shop/docs/erd-diagrams.md).

---

## 6. Детальний опис та політика узгодженості

- [docs/bounded-contexts.md](file:///k:/ЧНУ%20лаби%204%20курс/Лаби%20з%20.NET%20інженерії/Mini-project-EV-conversional-shop/docs/bounded-contexts.md) — детальний опис меж контекстів та сутностей.
- [docs/data-duplication-policy.md](file:///k:/ЧНУ%20лаби%204%20курс/Лаби%20з%20.NET%20інженерії/Mini-project-EV-conversional-shop/docs/data-duplication-policy.md) — обґрунтування політики узгодженості та snapshot-патерну.
- [docs/erd-diagrams.md](file:///k:/ЧНУ%20лаби%204%20курс/Лаби%20з%20.NET%20інженерії/Mini-project-EV-conversional-shop/docs/erd-diagrams.md) — розширені діаграми та код для dbdiagram.io.
