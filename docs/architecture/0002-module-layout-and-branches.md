# 0002 — Подредба вътре в модула, одит и клонове

Статус: прието (2026-11-24)

## Контекст

Проектът ще е голям. При преглед на код трябва да се отваря само частта, която се променя, а не цялото решение.
Първият модул с данни (Catalog) налага и решения, които ще важат за всички следващи.

## Решение

### Папки вътре в модула: по една папка на модел (агрегат)

```
src/Modules/<Module>/
  Apteka.<Module>.Contracts/          публичният интерфейс (напр. ICatalogLookup)
  Apteka.<Module>.Domain/<Model>/     ентитети, правила, грешки на модела (напр. Products/)
  Apteka.<Module>.Application/<Model>/ use cases (Create…Handler, Search…Handler) и интерфейси към базата
  Apteka.<Module>.Infrastructure/
    Persistence/                      DbContext, Configurations/, Migrations/
    <Model>/                          репозиторита и заявки на модела
    Seed/                             демо данни и инициализация на базата
    <Module>Module.cs                 Add<Module>Module(): единственото място, което Host вика
src/Host/Apteka.Web/Features/<Module>/   екраните на модула
tests/Modules/<Module>/…/<Model>/        unit тестове на модела
tests/Apteka.IntegrationTests/<Module>/  интеграционни тестове на модула
tests/Host/Apteka.Web.Tests/Features/<Module>/  bUnit тестове на екраните
```

Нов модел в съществуващ модул (напр. „Производители“ в Catalog) получава своя папка на всяко от тези нива.
Преглед на модел = тези папки.

### Общи правила за базата (потвърдени)

- Ключове `uuid` (`Guid.CreateVersion7()`), генерирани в домейна (`ValueGeneratedNever`).
- Без външни ключове между схеми на различни модули; вътре в модула има външни ключове.
- Имена в базата: snake_case (`product_units.product_id`), общо за всички модули (`SnakeCaseNaming`).
- Регистрация: `AddModuleDbContext<T>()` от `Apteka.Infrastructure.Common` (своя схема, своя история на миграциите, одит).
- Миграции: по модул, в `Persistence/Migrations`. В Development се прилагат при стартиране (`IModuleDatabaseInitializer`, Audit първи), заедно със seed.

### Одит

- `AuditInterceptor` (в `Infrastructure.Common`) записва ред в `audit.audit_log` за всяка вмъкната, променена или изтрита
  стойност, **в същата транзакция** като промяната. Ако записът се провали, няма и одит.
- При промяна се записват само променените полета (стари и нови стойности като JSON).
- Таблицата е собственост на модул Audit (миграцията му). Тригер забранява `UPDATE`, `DELETE` и `TRUNCATE`.
- Потребителят идва от `ICurrentUser`. Докато няма вход (Identity), това е `system`.

### Git клонове: по един на модул

- `main`: само завършени и тествани модули.
- `claude/catalog-module`, `claude/purchasing-module`, …: работата по един модул. Сливане в `main` с pull request,
  когато `dotnet test --solution Apteka.sln` е зелен. Така прегледът на PR показва само този модул
  (плюс малки общи промени в `BuildingBlocks`, ако модулът ги налага).
- Модулите НЕ живеят постоянно в отделни клонове: всички са в едно решение, за да се проверяват заедно от архитектурните тестове.

## Последствия

- (+) Преглед по модул и по модел; ясно къде отива всеки нов файл.
- (+) Одитът не може да бъде забравен в нов модул: идва с `AddModuleDbContext`.
- (−) `Infrastructure.Common` знае формата на `audit.audit_log` (SQL). Пази се в синхрон от `AuditLogTests`.
- (−) Повече папки за малък модел.
