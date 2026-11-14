# CLAUDE.md — Аптечна програма (демо)

Този файл е контекстът на проекта за Claude Code. Чете се автоматично при всяка сесия в тази папка.

## Какво правим

Нова аптечна програма за българска аптека (заместител на „Аптека Експерт 2“).
Сега правим **демо**: номенклатура, доставки с партиди, наличности и проста каса (свободна продажба).
НЕ правим още: фискален принтер, НЗИС / е-рецепти, НЗОК отчети, БОВЛ, НАП (Н-18).

- Интерфейсът е на **български**.
- Кодът, имената, коментарите и commit съобщенията са на **английски**.

## Технологии

- C#, **.NET 10**
- **ASP.NET Core** (Web API + Blazor Web App, Interactive Server) — едно приложение за демото
- **PostgreSQL 16** чрез **Entity Framework Core** (Npgsql), миграции с `dotnet ef`
- Тестове: **xUnit**, **Testcontainers** (истинска PostgreSQL), **bUnit** за Blazor компоненти
- Локално: `docker compose up -d` пуска само PostgreSQL; приложението — с `dotnet run`

### Структура

```
apteka-demo/
  CLAUDE.md
  docker-compose.yml          # само postgres
  Apteka.sln
  src/
    Apteka.Domain/            # ентитети, правила, без зависимости към EF/ASP.NET
    Apteka.Infrastructure/    # EF Core DbContext, миграции, seed
    Apteka.Web/               # ASP.NET Core + Blazor (UI и API)
  tests/
    Apteka.Domain.Tests/      # unit тестове
    Apteka.Integration.Tests/ # Testcontainers + PostgreSQL
```

## Речник (един термин = едно име в кода)

| Български | В кода | Значение |
|---|---|---|
| Стока | `Product` | Лекарство или друг артикул |
| Мярка | `ProductUnit` | Кутия, блистер, таблетка; `Factor` спрямо най-малката мярка |
| Най-малка мярка | `BaseUnit` | Мярката с `Factor = 1`; всички количества се пазят в нея |
| Доставчик | `Supplier` | Фьоникс, Стинг, Софарма и др. |
| Доставка / складова разписка | `Delivery`, `DeliveryLine` | Документ за получена стока |
| Партида | `Batch` | Номер на партида + срок на годност + цени |
| Движение | `StockMovement` | +/- количество за партида (доставка, продажба, брак, корекция) |
| Свободна продажба | `Sale` с `SaleType.Retail` | Продажба без рецепта по НЗОК |
| Продажба по НЗОК | `SaleType.Nhif` | По-късно, не в демото |
| Склад | `StockType.Retail` / `StockType.Nhif` | Наличността по НЗОК се води отделно |
| Сторно | `Reversal` | Обратна операция; нищо не се изтрива |

## Задължителни правила

1. **Пари:** само `decimal` в C# и `numeric(12,2)` (цени до `numeric(12,4)`) в базата. Никога `double`/`float`.
2. **Количества:** цяло число (`int`) в най-малката мярка. Преобразуване кутия ↔ таблетка само чрез `Factor`.
3. **Наличност:** изчислява се като сума от `StockMovement` по партида. `StockMovement` е само за добавяне — без UPDATE и DELETE.
4. **Без отрицателна наличност:** всяка продажба/брак проверява наличността в същата транзакция и заключва реда на партидата (`SELECT ... FOR UPDATE` или еквивалент), за да не продадат две каси последната кутия.
5. **Изписване по партиди:** по подразбиране първо партидата с най-близък срок на годност (FEFO). Изтекли партиди не се продават.
6. **Нищо не се изтрива физически.** Грешките се оправят със сторно или корекция.
7. **Одит:** всяка промяна на данни записва ред в `AuditLog` (потребител, време, действие, ентитет, стара и нова стойност като JSON).
8. **Лични данни:** никакви истински ЕГН, имена на пациенти или данни от реална аптека — само измислени seed данни. Пароли и connection string-ове — в `appsettings.Development.json` / user-secrets, никога в git.
9. **Време:** пази се в UTC (`timestamptz`); показва се в часова зона Europe/Sofia.
10. **Валута:** евро (EUR), два знака след десетичната запетая; показва се с български формат (`1 234,50 €`).

## Модел на данните (демо)

- `Product` — Id, Name, AtcCode?, Manufacturer?, VatRate, IsRx (по лекарско предписание), IsActive
- `ProductUnit` — Id, ProductId, Name, Factor (int ≥ 1); точно една мярка с Factor = 1 на стока
- `Barcode` — Id, ProductId, ProductUnitId, Code (уникален)
- `Supplier` — Id, Name, Eik, PaymentTermDays
- `Delivery` — Id, SupplierId, InvoiceNo, InvoiceDate, ReceivedAt, UserId, Status (Draft/Posted)
- `DeliveryLine` — Id, DeliveryId, ProductId, ProductUnitId, Quantity, UnitPurchasePrice, BatchNo, ExpiryDate, SalePrice
- `Batch` — Id, ProductId, BatchNo, ExpiryDate, PurchasePrice (за най-малка мярка), SalePrice (за най-малка мярка), StockType
- `StockMovement` — Id, BatchId, QuantityDelta (int, в най-малка мярка), Kind (Delivery/Sale/Reversal/WriteOff/Correction), DocumentType, DocumentId, UserId, CreatedAt
- `Sale` — Id, Number, SaleType, UserId, CreatedAt, Total, PaymentMethod (Cash/Card), Status (Completed/Reversed)
- `SaleLine` — Id, SaleId, BatchId, ProductUnitId, Quantity, UnitPrice, Discount, LineTotal
- `User` — Id, UserName, PasswordHash, Role (Manager/Pharmacist), IsActive
- `AuditLog` — Id, UserId, At, Action, Entity, EntityId, OldValues (jsonb), NewValues (jsonb)

Публикуването на доставка (`Draft → Posted`) създава партидите и движенията в една транзакция; публикувана доставка не се редактира, само се коригира.

## Екрани в демото

1. **Вход** — потребител + парола (две роли).
2. **Номенклатура** — списък, търсене по име/баркод, създаване и редакция на стока с мерки и баркодове.
3. **Доставка** — нова доставка: доставчик, фактура, редове (стока, мярка, количество, партида, срок, цени) → „Запиши“ (Draft) → „Публикувай“.
4. **Наличност** — по стока и по партида, с филтър „изтича до 90 дни“.
5. **Каса** — поле за баркод с постоянен фокус; Enter добавя ред; F2 количество; F9 плащане; Esc отказ. Изцяло без мишка.
6. **Продажби за деня** — списък и сторно на продажба.

## Тестове (задължителни преди всяко „готово“)

- Unit: преобразуване на мерки; изчисляване на ред и обща сума с ДДС и отстъпка; закръгляне; FEFO избор на партида.
- Integration (PostgreSQL в Testcontainers): публикуване на доставка → наличност; продажба намалява наличността; продажба над наличността се отказва; **две паралелни продажби на последната кутия — само едната минава**; сторно връща наличността; запис в `AuditLog`.
- Команда: `dotnet test` трябва да минава зелено.

## Как работим

- Малки стъпки: една функционалност → тестове → `dotnet build` и `dotnet test` → кратко резюме.
- Преди голяма промяна (нова таблица, промяна на модела) — първо предложи план и изчакай одобрение.
- Не добавяй функционалност извън демото (виж „Какво правим“). Идеи за по-късно — в `docs/BACKLOG.md`.
- README.md на български: изисквания, как се пуска, как се тества, тестови потребители (от seed).
