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

---

# Лабораторна робота №2: ASP.NET. Тришарова архітектура на базі ADO.NET та Dapper

**Дисципліна:** Технології .NET розробки Web-додатків / Розробка додатків на платформі .NET  
**Предметна область:** EV Conversional Shop (Ordering Service)  
**Гілка роботи:** `lab-02` (базується на гілці `lab-01`)

---

## 1. Архітектура та структура рішення (Vertical Slice / Three-Tier)

Мікросервіс замовлень (`Ordering Service`) реалізовано як самостійну вертикаль із чітко розмежованою тришаровою архітектурою та суворим однонаправленим потоком залежностей:
**Web/API** $\to$ **BLL** $\to$ **DAL**, зі спільним підключенням шару **Domain** у DAL та BLL.

```
       +-------------------------------------------------+
       |           EVShop.Ordering.Api                   |
       |  (Контролери, ProblemDetails RFC 9457, Swagger) |
       +-------------------------------------------------+
                               |
                               v
       +-------------------------------------------------+
       |           EVShop.Ordering.Bll                   |
       | (Сервіси IOrderService, DTOs, AutoMapper Profile)|
       +-------------------------------------------------+
                               |
                               v
       +-------------------------------------------------+
       |           EVShop.Ordering.Dal                   |
       |  (Unit of Work, ADO.NET + Dapper Repositories)  |
       +-------------------------------------------------+
                               |
                               v
       +-------------------------------------------------+
       |                SQL Server 2022                  |
       |  (EVShop_Ordering, SP usp_ConfirmOrder, Tables) |
       +-------------------------------------------------+
```

> **Правило чистої архітектури:** Жоден SQL-запит не з'являється поза межами DAL. Контролери API є тонкими адаптерами (thin adapters) і ніколи не звертаються до репозиторіїв чи бази даних напряму — виключно через сервіси бізнес-логіки BLL.

### Проєкти в рішенні:
1. `src/Services/Ordering/EVShop.Ordering.Domain` — доменні моделі (`Customer`, `Order`, `OrderItem`, `Product`, `OrderDetails`) та кастомні бізнес-винятки (`NotFoundException`, `BusinessConflictException`, `ValidationException`).
2. `src/Services/Ordering/EVShop.Ordering.Dal` — репозиторії даних:
   - Базовий клас `RepositoryBase` з єдиним підключенням `SqlConnection` та делегатом до поточної транзакції `SqlTransaction`.
   - Узагальнений репозиторій `BaseDapperRepository<T>` та інтерфейс `IGenericRepository<T>`.
   - `CustomerRepository` — **чистий ADO.NET** (`Microsoft.Data.SqlClient`) без сторонніх ORM, ручне мапіння через `SqlDataReader`, параметризовані запити, перехоплення помилок унікальності (`ex.Number 2601/2627`).
   - `OrderRepository` — **ADO.NET + Dapper** (`QueryAsync` з multi-mapping один-до-багатьох для замовлення та позицій, виклики збережуваних процедур `dbo.usp_ConfirmOrder`, `dbo.usp_CreateOrder`, `dbo.usp_PayOrder` тощо з перехопленням `THROW 50001`, `50002`).
   - `ProductRepository` — **ADO.NET + Dapper** з атомарним зменшенням залишку (`UPDATE dbo.Products SET StockQuantity = StockQuantity - @Quantity WHERE Id = @Id AND StockQuantity >= @Quantity`).
   - `UnitOfWork` — реалізація шаблону Unit of Work: координує спільне підключення та транзакцію `SqlTransaction` з рівнем ізоляції `IsolationLevel.ReadCommitted`, методи `BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`, `DisposeAsync`.
3. `src/Services/Ordering/EVShop.Ordering.Bll` — бізнес-логіка:
   - DTOs із валідацією `System.ComponentModel.DataAnnotations` (`CreateOrderDto`, `CreateOrderItemDto`, `CreateCustomerDto`, `OrderDto`, `CustomerDto`).
   - Профілі `AutoMapper` (`OrderProfile`).
   - Сервіси `OrderService`, `CustomerService`. Метод `ConfirmOrderAsync` виконує бізнес-транзакцію: перевіряє статус, викликає збережувану процедуру `usp_ConfirmOrder` і атомарно зменшує залишок комплектуючих на складі; при нестачі товару викликає `RollbackAsync()`.
4. `src/Services/Ordering/EVShop.Ordering.Api` — веб-інтерфейс:
   - Асинхронні контролери (`OrdersController`, `CustomersController`) з передачею `CancellationToken`.
   - Централізований обробник винятків `DomainExceptionHandler` у форматі **ProblemDetails (RFC 9457)**.
   - OpenAPI / Swagger документація.
5. `src/EVShop.AppHost` — оркестратор **.NET Aspire** для одночасного запуску API та залежностей.
6. `tests/EVShop.Ordering.Tests` — модульні тести з використанням **xUnit** та **Moq**.

---

## 2. Гібридний шар доступу до даних: ADO.NET vs Dapper

| Характеристика | Репозиторій клієнтів (`CustomerRepository`) | Репозиторій замовлень (`OrderRepository`) |
| :--- | :--- | :--- |
| **Технологія** | Чистий **ADO.NET** (`Microsoft.Data.SqlClient`) | **Dapper** (Micro-ORM над ADO.NET) |
| **Generic Repository** | Свідомо **НЕ** успадковує `IGenericRepository<T>` | Успадковує `BaseDapperRepository<Order>` |
| **Мапінг даних** | Ручний через `SqlDataReader` та `reader.GetOrdinal(...)` | Автоматичний та `Multi-Mapping` (`Order` + `OrderItem`) |
| **Транзакції** | Працює через спільний `SqlTransaction` з UoW | Працює через спільний `SqlTransaction` з UoW |
| **Використання** | Базові операції з низьким оверхедом | Складні запити, JOIN, виклики Stored Procedures |

---

## 3. Транзакційний сценарій та Unit of Work

Бізнес-операція **«Підтвердження замовлення» (`ConfirmOrderAsync`)** гарантує властивості ACID для двох незалежних сутностей:

```
OrderService                   UnitOfWork             OrderRepository         ProductRepository        SQL Server
     |                              |                        |                        |                    |
     |--- BeginTransactionAsync --->|                        |                        |                    |
     |                              |------------------------|------------------------|---> BEGIN TRAN     |
     |--- ConfirmAsync(orderId) ---------------------------->|                        |                    |
     |                              |                        |--- usp_ConfirmOrder ----------------------->|
     |--- DecreaseStockAsync(item) -------------------------------------------------->|                    |
     |                              |                        |                        |---> UPDATE Stock ->|
     |                              |                        |                        |     (Check Stock)  |
     |-- [Якщо товару бракує] --------------------------------------------------------|                    |
     |--- RollbackAsync ----------->|                        |                        |                    |
     |                              |------------------------|------------------------|---> ROLLBACK TRAN  |
     |-- [Якщо все успішно] --------|                        |                        |                    |
     |--- CommitAsync ------------->|                        |                        |                    |
     |                              |------------------------|------------------------|---> COMMIT TRAN    |
```

Рівень ізоляції транзакції — **`IsolationLevel.ReadCommitted`**. Забезпечує захист від «брудного читання» (Dirty Read), водночас мінімізуючи блокування рядків у таблицях та уникаючи взаємних блокувань (Deadlocks).

---

## 4. Обробка помилок та RFC 9457 ProblemDetails

Усі винятки з БД або бізнес-правил транслюються в доменні винятки в DAL/BLL, а глобальний обробник `DomainExceptionHandler` формує стандартну відповідь `ProblemDetails`:

| Ситуація | Доменний виняток / Код БД | HTTP-статус | Title у ProblemDetails |
| :--- | :--- | :--- | :--- |
| Помилка валідації вхідних даних (DTO) | `ValidationException` / DataAnnotations | **400 Bad Request** | Помилка валідації даних |
| Замовлення або клієнта не знайдено | `NotFoundException` / `THROW 50001` | **404 Not Found** | Ресурс не знайдено |
| Недопустима зміна статусу (наприклад, повторне підтвердження) | `BusinessConflictException` / `THROW 50002` | **409 Conflict** | Конфлікт бізнес-правил |
| Недостатньо товару на складі | `BusinessConflictException` | **409 Conflict** | Конфлікт бізнес-правил |
| Порушення унікальності Email клієнта | `BusinessConflictException` / Error `2601, 2627` | **409 Conflict** | Конфлікт бізнес-правил |

Кожна відповідь з помилкою містить поле `traceId` (кореляційний ідентифікатор) для аудиту та трасування.

---

## 5. Запуск проєкту

### Варіант А. Запуск через .NET Aspire (Рекомендовано)
Aspire AppHost автоматично зв'язує API із базою даних і надає веб-дашборд моніторингу:
```bash
dotnet run --project src/EVShop.AppHost
```
Посилання на Aspire Dashboard виводиться в консоль (зазвичай `http://localhost:15xxx`).

### Варіант Б. Прямий запуск API
```bash
dotnet run --project src/Services/Ordering/EVShop.Ordering.Api --urls "http://localhost:5267"
```
- **Swagger UI:** [http://localhost:5267/swagger](http://localhost:5267/swagger)
- **OpenAPI Schema:** [http://localhost:5267/swagger/v1/swagger.json](http://localhost:5267/swagger/v1/swagger.json)

### Запуск тестів
```bash
dotnet test tests/EVShop.Ordering.Tests
```
Усі 5 тестів покривають конфігурацію AutoMapper, позитивні сценарії підтвердження та перевірку відкату транзакцій при браку товару.

---

## 6. Приклади перевірочних запитів (cURL)

### 1. Отримання клієнта (чистий ADO.NET)
```bash
curl -X GET "http://localhost:5267/api/customers/1" -H "Accept: application/json"
```

### 2. Захист від SQL Injection (перевірка спецсимволів: ім'я O'Brien)
```bash
curl -X POST "http://localhost:5267/api/customers" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Conan O'\''Brien","email":"conan.obrien@example.com","phoneNumber":"+380671234567"}'
```
*Результат:* HTTP 201 Created (запит успішно параметризовано, синтаксичної помилки SQL немає).

### 3. Створення нового замовлення
```bash
curl -X POST "http://localhost:5267/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": 1,
    "shippingAddress": "м. Київ, вул. Хрещатик, 22, кв. 10",
    "contactPhone": "+380501112233",
    "targetVehicleModel": "BMW E30",
    "conversionType": "Full_Turnkey",
    "installationNotes": "Підготовка під мотор Nissan Leaf EM57",
    "items": [{"productId": 5001, "quantity": 2, "discount": 0}]
  }'
```
*Результат:* HTTP 201 Created із фіксацією ціни та назви товару у вигляді знімка (Snapshot).

### 4. Підтвердження замовлення (Транзакція UoW + usp_ConfirmOrder + списання залишку)
```bash
curl -X POST "http://localhost:5267/api/orders/11002/confirm"
```
*Результат:* HTTP 204 No Content. Залишок товару 5001 у базі зменшився з 8 до 6.

### 5. Повторне підтвердження (Конфлікт статусів)
```bash
curl -X POST "http://localhost:5267/api/orders/11002/confirm"
```
*Результат:* HTTP 409 Conflict (ProblemDetails з повідомленням від збережуваної процедури: *«Підтвердити наявність можна лише для нового замовлення...»*).

### 6. Запит неіснуючого замовлення
```bash
curl -X GET "http://localhost:5267/api/orders/99999"
```
*Результат:* HTTP 404 Not Found (ProblemDetails: *«Замовлення з ID 99999 не знайдено»*).

### 7. Відкат транзакції при перевищенні залишку товару
Спроба підтвердження замовлення на кількість, більшу за залишок на складі (наприклад, 10 одиниць товару 5003 при залишку 5):
```bash
curl -X POST "http://localhost:5267/api/orders/11003/confirm"
```
*Результат:* HTTP 409 Conflict (*«Недостатньо залишку на складі...»*). Статус замовлення відкочується назад у `Submitted`, а залишок товару не змінюється.

---

## 7. Відповіді на контрольні питання

### 1. З яких шарів складається тришарова архітектура і в якому напрямку спрямовані залежності між ними?
**Відповідь:** Тришарова архітектура складається з:
1. **Presentation / Web API Layer** (контролери, валідація DTO, middleware обробки помилок, маршрутизація).
2. **Business Logic Layer (BLL)** (сервіси бізнес-логіки, оркестрація транзакцій, перевірка інваріантів, мапінг DTO/сутностей).
3. **Data Access Layer (DAL)** (репозиторії даних, Unit of Work, SQL-команди, з'єднання з БД).
4. **Domain Layer** (моделі сутностей, винятки) — спільне ядро.

Залежності спрямовані строго в один бік: **API $\to$ BLL $\to$ DAL**. Контролери нічого не знають про репозиторії чи деталі збереження даних у БД і спілкуються виключно з інтерфейсами BLL.

### 2. Чому інтерфейс репозиторію повинен повертати модель чи DTO, а не SqlDataReader чи DataTable?
**Відповідь:** 
1. **Інкапсуляція деталей інфраструктури:** `SqlDataReader` та `DataTable` прив'язують зовнішній код до конкретної СКБД (SQL Server) та технології ADO.NET. При переході на PostgreSQL, MySQL або ORM (EF Core) довелося б переписувати BLL.
2. **Управління ресурсами:** `SqlDataReader` тримає з'єднання з базою даних відкритим, поки читач не закрито. Повернення читача у вищі шари призводить до витоку підключень у пулі (Connection Pool starvation).
3. **Типобезпека та тестованість:** Моделі та DTO легко мокати у Unit-тестах без необхідності піднімати реальну СКБД.

### 3. Навіщо потрібен Generic Repository? Чому ADO.NET-репозиторій у цій роботі його свідомо не успадковує?
**Відповідь:** `Generic Repository<T>` реалізує принцип DRY (Don't Repeat Yourself), узагальнюючи повторювані базові CRUD-операції (`GetById`, `GetAll`, `Add`, `Delete`). 
ADO.NET-репозиторій (`CustomerRepository`) свідомо не успадковує `Generic Repository`, оскільки:
- Демонструє фундаментальну роботу з `Microsoft.Data.SqlClient` «з нуля» (без динамічної генерації SQL);
- Ручне мапіння полів через `SqlDataReader` для кожної сутності унікальне;
- Відокремлює спеціалізовану оптимізовану роботу від узагальнених абстракцій.

### 4. Чим відрізняється робота з тим самим запитом через чистий ADO.NET і через Dapper? Коли виправдано писати «руками»?
**Відповідь:**
- **ADO.NET:** вимагає самостійного створення `SqlCommand`, додавання параметрів із типами даних, відкриття `SqlDataReader` та вичитування кожного стовпця за індексом/іменем (`reader.GetString(...)`).
- **Dapper:** це micro-ORM, що автоматизує генерацію команд, параметризацію анонімних об'єктів та рефлексивне зіставлення колонок SQL із властивостями C#-класів (включно з multi-mapping для зв'язків 1:N).
- **Коли писати руками на ADO.NET:**
  1. Стрімінгове читання гігантських обсягів даних через `CommandBehavior.SequentialAccess` (наприклад, читання великих BLOB або експорт мільйонів рядків без завантаження всієї вибірки в пам'ять).
  2. Масове завантаження через `SqlBulkCopy`.
  3. Максимально критичні до мікросекундної швидкодії операції без оверхеду навіть мінімальної рефлексії.

### 5. Чому конкатенація рядків у SQL-запиті небезпечна, навіть якщо значення «просто Id»?
**Відповідь:**
1. **SQL Injection:** Якщо тип змінної у рядку прийшов як рядок або не провалідований, зловмисник може передати `1 OR 1=1; DROP TABLE Customers;--`.
2. **Засмічення кешу планів запитів (Plan Cache Bloat):** При конкатенації `SELECT * FROM Orders WHERE Id = 1` та `WHERE Id = 2` SQL Server сприймає це як два абсолютно різних запити і щоразу будує новий план виконання. Параметризація (`WHERE Id = @Id`) дозволяє кешувати і повторно використовувати скомпільований план виконання.
3. **Локалізація та типи:** Конкатенація чисел із плаваючою комою або дат може викликати синтаксичну помилку через роздільники крапка/кома в різних локалях ОС.

### 6. Як Unit of Work гарантує, що кілька репозиторіїв працюють в одній транзакції? Що станеться, якщо один із них не отримає той самий SqlTransaction?
**Відповідь:** Unit of Work створює **одне** спільне з'єднання `SqlConnection` та відкриває **одну** транзакцію `SqlTransaction`. Усі репозиторії (`Customers`, `Orders`, `Products`) успадковують `RepositoryBase` і отримують доступ до поточної транзакції через делегат `Func<SqlTransaction?>`. 
- Якщо хоча б один репозиторій виконає запит без прив'язки до цієї `SqlTransaction`, SqlClient кине виняток: *«Execute requires the command to have a transaction when the connection assigned to the command is in a pending local transaction»*, або запит виконається поза транзакцією, зруйнувавши атомарність і цілісність даних.

### 7. Як помилка, кинута збережуваною процедурою, перетворюється на HTTP-статус 409 для клієнта?
**Відповідь:** Ланцюжок трансляції:
1. **SQL Server:** Процедура `usp_ConfirmOrder` перевіряє статус замовлення. Якщо воно вже підтверджене, викликається `THROW 50002, '...', 1;`.
2. **DAL:** Dapper/ADO.NET перехоплює `SqlException` з номером `ex.Number == 50002`. Метод `TranslateSqlException` загортає її в доменний виняток `BusinessConflictException`.
3. **BLL:** Пропускає доменний виняток нагору без викривлення.
4. **Web/API:** `DomainExceptionHandler` через патерн-матчинг перетворює `BusinessConflictException` на `StatusCodes.Status409Conflict` та записує відповідь у стандарті RFC 9457 `ProblemDetails`.

### 8. Чому потрібні обидва рівні валідації — на DTO і в BLL?
**Відповідь:**
- **Валідація DTO (API рівень):** перевіряє формат і форму даних без звернення до сховища (наявність обов'язкових полів, довжина рядка, коректність email/телефону, додатні числа). Це відсікає сміттєві запити на самому початку конвеєра (`Fail-Fast`), заощаджуючи ресурси процесора та БД.
- **Бізнес-валідація (BLL рівень):** перевіряє стан системи та доменні інваріанти, які потребують доступу до бази даних (чи існує такий клієнт, чи статус замовлення дозволяє його редагування, чи достатньо товару на складі).

### 9. Яка різниця між optimistic і pessimistic concurrency? Коли доречний кожен підхід?
**Відповідь:**
- **Optimistic Concurrency:** Рядки не блокуються при читанні. Використовується стовпець версії (`rowversion` / `xmin`). Під час запису перевіряється, чи версія не змінилася (`UPDATE ... WHERE Id = @Id AND RowVersion = @OldVersion`). Якщо змінена — кидається виняток конфлікту.
  *Доречно:* коли конкурентні зміни рідкісні, переважна більшість операцій — читання, низька ціна повтору операції.
- **Pessimistic Concurrency:** Рядок явно блокується в момент читання (наприклад, `SELECT ... WITH (UPDLOCK, ROWLOCK)` у процедурах Лаб. 01). Інші транзакції чекають завершення першої.
  *Доречно:* коли ймовірність одночасного конфлікту висока (наприклад, списання дефіцитного товару під час розпродажу або банківські операції), де відкат і повтор для користувача неприйнятні.

### 10. Чому статус 500 у відповідь на «запис не знайдено» — помилка проєктування API?
**Відповідь:**
- Статус **500 Internal Server Error** означає непередбачену помилку на стороні сервера (падіння програми, збій з'єднання з БД, NullReferenceException, дефекти коду). Системи моніторингу (Datadog, Prometheus, Sentry) трактують статус 500 як аварійну ситуацію, що вимагає втручання чергового інженера.
- «Запис не знайдено» — це штатна, очікувана ситуація бізнес-домену, викликана клієнтом, що передав неіснуючий ідентифікатор. Згідно зі стандартом HTTP / REST, клієнтською помилкою відсутності ресурсу є **404 Not Found**.

### 11. Навіщо .NET Aspire AppHost потрібен уже на цьому етапі, якщо Gateway й Aggregator з’являться лише в Лаб. 05?
**Відповідь:**
1. **Єдиний запуск усього середовища:** AppHost дозволяє підняти API, бази даних, черги та залежності однією командою (`dotnet run --project src/EVShop.AppHost`) без необхідності відкривати кілька консолей.
2. **Централізоване управління конфігурацією:** Aspire автоматично прокидує Service Discovery та рядки підключення через системні змінні середовища, усуваючи hardcode портів та адрес.
3. **OpenTelemetry Dashboard «з коробки»:** Уже з Лабораторної №2 розробник має інтерактивний дашборд із логами в реальному часі, метриками запитів, трасуванням Distributed Tracing та статусом здоров'я сервісів.

