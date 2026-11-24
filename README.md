# Аптечна програма (демо)

Нова програма за българска аптека. Модулен монолит на .NET 10 + PostgreSQL.
Готово: **Номенклатура** (стоки, мерки, баркодове, справочници), **Доставки** (чернова → публикуване) и
**Наличност** (по партиди, изчислена от движенията), **Каса** (без мишка, FEFO), **Продажби за деня** със сторно и
**Вход** с две роли, с одит на всяка промяна (кой, кога, какво).

Подробности за правилата и модела: [CLAUDE.md](CLAUDE.md). Защо е така: [docs/architecture/0001-modular-monolith.md](docs/architecture/0001-modular-monolith.md),
подредба на модулите и клонове: [docs/architecture/0002-module-layout-and-branches.md](docs/architecture/0002-module-layout-and-branches.md).

## Изисквания

- .NET 10 SDK
- Docker Desktop (PostgreSQL за разработка и за интеграционните тестове)
- Git

Всичко това се инсталира със `setup.ps1`.

## Как се пуска

1. Копирай настройките за разработка (файлът е извън git, защото съдържа парола):

   ```powershell
   Copy-Item src\Host\Apteka.Web\appsettings.Development.example.json src\Host\Apteka.Web\appsettings.Development.json
   ```

2. Стартирай базата: `docker compose up -d`
3. Стартирай приложението: `dotnet run --project src\Host\Apteka.Web`
4. Отвори http://localhost:5080 → „Номенклатура“

При стартиране в Development приложението само прилага миграциите на всички модули и добавя демо данни
(50 измислени стоки с баркодове 200…, 3 доставчика, 2 публикувани доставки и 1 чернова), ако базата е празна.

Нова миграция за модул (пример за Catalog):

```powershell
dotnet ef migrations add <Име> --project src\Modules\Catalog\Apteka.Catalog.Infrastructure --startup-project src\Host\Apteka.Web --context CatalogDbContext --output-dir Persistence\Migrations
```

## Как се тества

```powershell
dotnet test --solution Apteka.sln
```

Интеграционните тестове стартират собствена PostgreSQL чрез Testcontainers, затова Docker Desktop трябва да върви.
Архитектурните правила се проверяват от `tests/Apteka.ArchitectureTests`.

## Структура

Виж раздела „Структура“ в [CLAUDE.md](CLAUDE.md).

## Тестови потребители

| Потребител | Роля | Може |
|---|---|---|
| `manager` | Управител | всичко, включително „Потребители“ и сторно |
| `pharmacist` | Фармацевт | каса, доставки, наличност, номенклатура (без потребители и сторно) |

Паролите **не са в git** (правило 8). Задайте ги в `appsettings.Development.json`, преди първото пускане:

```json
"Identity": { "Seed": { "ManagerPassword": "…", "PharmacistPassword": "…" } }
```

(поне 8 знака, буква и цифра). Ако не са зададени, при първото пускане програмата генерира пароли и ги
показва веднъж в конзолата (ред „Demo user … was created with the generated password …“).
Потребителите се създават само в празна база; после паролите се сменят от екрана „Потребители“.
