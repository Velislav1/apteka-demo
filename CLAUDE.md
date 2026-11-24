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

Архитектура: **модулен монолит** (един процес, една PostgreSQL база, модули с ясни граници). Защо: `docs/architecture/0001-modular-monolith.md`.

```
apteka-demo/
  CLAUDE.md, README.md, Apteka.sln, global.json, .editorconfig, .gitignore
  Directory.Build.props       # общи настройки: net10.0, nullable, warnings as errors
  Directory.Packages.props    # централни версии на NuGet пакетите
  docker-compose.yml          # само PostgreSQL 16
  docs/ci/ci.yml.disabled     # CI (build + test); неактивен, виж docs/NEXT-STEPS.md
  docs/
    architecture/             # ADR: 0001-modular-monolith.md ...
    domain/glossary.md        # речник
    BACKLOG.md                # идеи и бъдещи модули
  src/
    Host/
      Apteka.Web/             # ASP.NET Core + Blazor; само сглобява модулите
        Features/<Module>/    # Blazor страници, групирани по модул
    BuildingBlocks/
      Apteka.SharedKernel/            # Money, UnitPrice, Quantity, Entity, Result, IUnitOfWork, ICurrentUser, BulgarianTime
      Apteka.Infrastructure.Common/   # ModuleDbContext, AddModuleDbContext, audit interceptor, обща транзакция (UnitOfWork)
    Modules/
      Catalog/      # стоки, мерки, баркодове, производители      (схема catalog)
      Purchasing/   # доставчици, доставки                         (схема purchasing)
      Inventory/    # партиди, движения, наличност, инвентаризация (схема inventory)
      Sales/        # каса, продажби, сторно, клиенти              (схема sales)
      Identity/     # потребители, роли, вход                      (схема identity)
      Audit/        # одит лог                                     (схема audit)
        # всеки модул има 4 проекта:
        #   Apteka.<Module>.Contracts        публичен интерфейс към другите модули
        #   Apteka.<Module>.Domain           ентитети и правила, без EF/ASP.NET
        #   Apteka.<Module>.Application      use cases (команди/заявки)
        #   Apteka.<Module>.Infrastructure   EF Core, собствена DB схема, Add<Module>Module()
  tests/
    Modules/<Module>/Apteka.<Module>.Tests   # unit тестове
    BuildingBlocks/Apteka.SharedKernel.Tests
    Apteka.IntegrationTests                  # Testcontainers + PostgreSQL
    Apteka.ArchitectureTests                 # проверява правилата по-долу
```

### Правила на архитектурата (проверяват се от `Apteka.ArchitectureTests`)

- Модул НЕ вижда `Domain`/`Application`/`Infrastructure` на друг модул, само неговия `Contracts` проект.
- `Domain` не зависи от нищо освен `SharedKernel`; няма EF Core, Npgsql или ASP.NET пакети.
- `Contracts` зависи само от `SharedKernel`. `Application` не реферира `Infrastructure`.
- `BuildingBlocks` не реферират модули; `SharedKernel` не реферира нищо.
- Всеки модул има собствена PostgreSQL схема (catalog, purchasing, inventory, sales, identity, audit) и собствени миграции (историята също е в неговата схема).
- Само `Apteka.Web` регистрира модулите (всеки дава `Add<Module>Module()`).
- Бъдещите модули (Prescriptions, Nhif, Fiscal, Verification, Reporting, Agent) НЕ се създават преди време, а само се описват в `docs/BACKLOG.md`.
- Тестовете се пускат с `dotnet test --solution Apteka.sln` (xunit v3 работи през Microsoft Testing Platform, избрана в `global.json`).

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

- `Product` — Id, Code (вътрешен код от sequence `catalog.product_codes`, уникален), Name, LatinName?, GenericName?, ActiveSubstance?, AtcCode?, DispensingMode (OverTheCounter / Prescription / Narcotic), VatRate, IsActive, NhifCode?, ManufacturerId?, ProductGroupId?, ShelfId?, DefaultSaleUnitId?, MarkupPercent?, MinStock?, MaxStock? (в най-малка мярка)
- `Manufacturer`, `ProductGroup`, `Shelf` — Id, Name (уникално), IsActive; не се трият, а се деактивират; `ProductGroup` има и MarkupPercent?
- Надценка по подразбиране при доставка: на стоката, иначе на групата, иначе 25 %
- `ProductUnit` — Id, ProductId, Name, Factor (int ≥ 1); точно една мярка с Factor = 1 на стока
- `Barcode` — Id, ProductId, ProductUnitId, Code (уникален)
- `Supplier` — Id, Name (уникално), Eik (9/13 цифри с контролна цифра, уникален), PaymentTermDays, IsActive
- `Delivery` — Id, Number (№ на складовата разписка, от sequence `purchasing.delivery_numbers`), SupplierId, InvoiceNo (уникален за доставчика), InvoiceDate, PaymentTermDays (по подразбиране от доставчика; падеж = дата на фактурата + дни), ReceivedAt, UserId, Status (Draft/Posted), PostedAt
- `DeliveryLine` — Id, DeliveryId, Position, ProductId, ProductUnitId, UnitFactor, Quantity (в тази мярка), UnitPurchasePrice (Цена ТЕ, без ДДС), SupplierDiscountPercent (Отстъпка ТЕ %; покупна цена = цена ТЕ × (1 − отстъпка), до 4 знака — тя влиза в сумата, партидата и надценката), MarkupPercent?, SalePrice (с ДДС), BatchNo, ExpiryDate, IsRemoved (премахнат ред от чернова)
- `Batch` — Id, ProductId, BatchNo, ExpiryDate, PurchasePrice (за най-малка мярка, без ДДС), SalePrice (за най-малка мярка, с ДДС), PricedUnitFactor + PricedUnitSalePrice (точната цена на мярката, в която е въведена), StockType, SourceDocumentType/Id, SourceLineId (уникален)
- `StockMovement` — Id, BatchId, QuantityDelta (int, в най-малка мярка), Kind (Delivery/Sale/Reversal/WriteOff/Correction), DocumentType, DocumentId, UserId, CreatedAt
- `WriteOff` + `WriteOffLine` — бракуване: Number (sequence `inventory.write_off_numbers`), Reason?, UserId, CreatedAt; редове с ProductName, UnitName, UnitFactor, Quantity, BatchId, BatchNo, ExpiryDate, PurchasePrice (за най-малка мярка). Само за добавяне (тригер); движение WriteOff от избраната партида (и изтекла)
- `DeliveryCorrection` + `DeliveryCorrectionLine` — корекционна складова разписка към приключена доставка: Number (sequence `purchasing.delivery_correction_numbers`), DeliveryId, NoteNo?, NoteDate, Reason?; редове DeliveryLineId, QuantityDelta (±, в мярката на реда), UnitPurchasePrice (нетна). Само за добавяне; движение Correction върху партидата на реда
- `Stocktaking` + `StocktakingLine` — инвентаризация (всички / група / рафт): Number, Scope, Status (Open/Closed), StartedAt/By, ClosedAt/By; редове по партида с ExpectedQuantity, CountedQuantity?, Difference?. Най-много една отворена; докато е отворена, стоките ѝ не се продават, заприхождават, коригират и бракуват; приключването прави движения Correction; приключената не се променя (тригер)
- `Sale` — Id, Number (от sequence), SaleType, UserId, CreatedAt, Total, VatTotal, PaymentMethod (Cash/Card), CashReceived?, Status (Completed/Reversed)
- `SaleLine` — Id, SaleId, Position, ProductId, ProductName, ProductUnitId, UnitName, UnitFactor, Quantity, UnitPrice (с ДДС), DiscountPercent, DiscountAmount, VatRate, LineTotal, VatAmount
- `SaleLineBatch` — Id, SaleLineId, BatchId, BaseQuantity (от кои партиди е изписан редът; FEFO може да го раздели)
- `Reversal` — Id, SaleId (уникален), Reason?, UserId, CreatedAt (сторно на цялата продажба)
- `User` — Id, UserName (малки букви, уникално), DisplayName, PasswordHash (никога в одита), Role (Manager/Pharmacist), IsActive, FailedLoginCount, LockedUntil, LastLoginAt
- `AuditLog` — Id, UserId, At, Action, Entity, EntityId, OldValues (jsonb), NewValues (jsonb)

Публикуването на доставка (`Draft → Posted`) създава партидите и движенията в една транзакция (`IUnitOfWork`, виж `docs/architecture/0003-cross-module-transactions.md`); публикувана доставка не се редактира, само се коригира.

Цени: покупната цена от фактурата е **без ДДС**, продажната е **с ДДС**. Предложена продажна цена = покупна × (1 + надценка %) × (1 + ДДС %), закръглена до цент; може да се смени ръчно.

## Екрани в демото

1. **Вход** — потребител + парола (две роли). Управител: всичко. Фармацевт: без „Потребители“ и без сторно. 5 грешни опита → заключване за 5 минути.
Подредбата (меню, клавиши, думи) следва Аптека Експерт 2: `docs/legacy/apteka-expert-2-screens.md`.

2. **Номенклатура** — списък (активни / неактивни / всички), търсене по код/име/вещество/баркод; F5 нова, F6 редакция;
   „Данни за стоката“ в табове: Основни данни · Цени и запаси · Мерки (F11) · Баркодове.
3. **Складова разписка** (Ctrl+F1) — 1) доставчик, фактура, дни за плащане → „Потвърди“ (чернова); 2) редове един по един
   (стока, мярка F12, количество, цена ТЕ, отстъпка ТЕ, надценка, продажна цена, партида, срок): Enter добавя и записва,
   F6 поправя, F8 трие, Ctrl+Enter два пъти приключва (публикува).
4. **Наличност** — по стока и по партида, с филтър „изтича до 90 дни“.
5. **Директна продажба** (Ctrl+F3) — поле с постоянен фокус: код, баркод или име + Enter; `3*` количество; ↑/↓ ред;
   F4 отстъпка на реда; `%10` обща отстъпка за цялата продажба; F12 мярка; F8 изтрий; F10 плащане (Enter в брой, F10 с карта);
   Ctrl+Enter приключи в брой; Esc назад, два пъти отказ. Изцяло без мишка. Rx стоки — с потвърждение на рецептата; упойващи — забранени.
6. **Директни продажби (за деня)** — списък и сторно на продажба; F5 нова.
7. **Справки** — картон на стока, стокова наличност (филтър по група), дневен опис (печалба), срок до 90 дни,
   изтекъл срок към дата, под минимален запас.
8. **Бракуване на стоки** (Продажби) — стока по код/баркод/име, избор на партида (и изтекла), количество; Ctrl+Enter два пъти.
9. **Корекционна складова разписка** (Доставки) — към приключена доставка, ± по редовете; Ctrl+Enter два пъти.
10. **Инвентаризация** (Настройки → Склад) — протокол по партиди, „Намерено“ в кутии + бройки, записва се веднага;
    Ctrl+Enter два пъти приключва.

## Тестове (задължителни преди всяко „готово“)

- Unit: преобразуване на мерки; изчисляване на ред и обща сума с ДДС и отстъпка; закръгляне; FEFO избор на партида.
- Integration (PostgreSQL в Testcontainers): публикуване на доставка → наличност; продажба намалява наличността; продажба над наличността се отказва; **две паралелни продажби на последната кутия — само едната минава**; сторно връща наличността; запис в `AuditLog`.
- Команда: `dotnet test` трябва да минава зелено.

## Как работим

- Малки стъпки: една функционалност → тестове → `dotnet build` и `dotnet test` → кратко резюме.
- Всеки модул в свой git клон (`claude/<module>-module`), сливане в `main` с PR. Вътре в модула всеки модел е в своя папка
  на всяко ниво (`Domain/<Model>/`, `Application/<Model>/`, …). Виж `docs/architecture/0002-module-layout-and-branches.md`.
- Преди голяма промяна (нова таблица, промяна на модела) — първо предложи план и изчакай одобрение.
- Дизайн: общите стилове и цветове в `wwwroot/app.css`, стиловете на един екран до него (`<Екран>.razor.css`);
  без шрифтове и библиотеки от интернет. Виж `docs/architecture/0004-ui-design-system.md`.
- Не добавяй функционалност извън демото (виж „Какво правим“). Идеи за по-късно — в `docs/BACKLOG.md`.
- README.md на български: изисквания, как се пуска, как се тества, тестови потребители (от seed).
- В нова сесия (напр. на друг компютър) първо прочети `docs/NEXT-STEPS.md` и `docs/PROJECT-CONTEXT.md`: там е къде сме спрели и какво е решено.
- Старата програма „Аптека Експерт 2“ е описана в `docs/legacy/apteka-expert-2-analysis.md`; при нов модул сравни с нея.
