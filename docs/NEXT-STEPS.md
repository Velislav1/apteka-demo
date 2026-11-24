# Къде сме спрели и как да продължиш

Състояние към 2026-11-24.

## Готово

- Инсталирани инструменти: .NET 10 SDK, Git, Docker Desktop (с WSL 2), `dotnet-ef`, C# Dev Kit.
- Скелет на **модулен монолит**: 6 модула (Catalog, Purchasing, Inventory, Sales, Identity, Audit),
  по 4 проекта всеки (Contracts, Domain, Application, Infrastructure), SharedKernel, Infrastructure.Common,
  Blazor host с начална страница на български, свързана с PostgreSQL.
- **Анализът на Аптека Експерт 2** е в `docs/legacy/apteka-expert-2-analysis.md` (без пароли, лични данни и адреси);
  бъдещите модули от него са в `docs/BACKLOG.md`.
- **Стъпка 1 (Catalog) е готова** в клон `claude/catalog-module`: таблици `catalog.products`, `product_units`, `barcodes`,
  `manufacturers`, `product_groups`, `shelves`; режим на отпускане (без рецепта / по лекарско / упойващо), код по НЗОК,
  мярка за продажба; екрани „Производители“, „Групи“, „Рафтове“;
  екрани „Номенклатура“ (списък, търсене по име/баркод) и „Нова стока“ / „Редакция“, 50 измислени стоки (seed),
  одит на всяка промяна в `audit.audit_log` (само за добавяне), `ICatalogLookup` за другите модули.
- **Стъпка 2 (Purchasing + Inventory) е готова** в клон `claude/purchasing-inventory-module`:
  доставчици (ЕИК с контролна цифра), доставки (чернова → публикуване), партиди и движения (само за добавяне),
  обща транзакция между модулите (`IUnitOfWork`, ADR 0003), екрани „Доставки“, „Доставка“, „Доставчици“, „Наличност“
  (в кутии и в най-малка мярка, филтър „изтича до 90 дни“), демо: 3 доставчика, 2 публикувани доставки и 1 чернова.
- **Стъпка 3 (Sales) е готова** в клон `claude/sales-module`: каса без мишка (баркод/име, F2, F3, F9, Esc),
  изписване по FEFO със заключване на партидите (две каси не продават една и съща последна кутия),
  Rx с потвърждение, упойващи забранени, отстъпка % на ред, сторно (връща в същите партиди), „Продажби за деня“.
- 202 теста минават (unit, архитектурни, интеграционни с Testcontainers, bUnit).
- Още няма: вход (Identity), доставки, наличности, каса.

## Как да продължиш на друг компютър

1. Инсталирай Git и Visual Studio Code, после:

   ```
   git clone https://github.com/Velislav1/apteka-demo
   cd apteka-demo
   powershell -ExecutionPolicy Bypass -File .\setup.ps1
   ```

   `setup.ps1` инсталира .NET 10 SDK, Docker Desktop, `dotnet-ef` и разширенията за VS Code.
   След Docker Desktop рестартирай, отвори го и изчакай „Engine running“.
   Ако Docker каже „Virtual Machine Platform is not enabled“, пусни в PowerShell като администратор:
   `Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform` и рестартирай.
   Ако иска WSL обновяване: `wsl --update`.
2. Настройки за разработка (файлът не е в git, защото съдържа парола):

   ```
   Copy-Item src\Host\Apteka.Web\appsettings.Development.example.json src\Host\Apteka.Web\appsettings.Development.json
   ```
3. Провери: `docker compose up -d`, после `dotnet test --solution Apteka.sln`. Всички тестове трябва да са зелени.
4. Отвори папката във VS Code, стартирай Claude Code. `CLAUDE.md` се чете автоматично.
5. Git идентичност на новия компютър: `git config --global user.name "..."` и `git config --global user.email "..."`.

## Какво следва

Промптовете са в `PROMPTS.md`. Продължаваме модул по модул, като към всеки промпт се добавя:
„Спазвай модулната структура и правилата в CLAUDE.md.“

| Стъпка | Модул | Промпт | Състояние |
|---|---|---|---|
| 1 | Catalog | 2 | ✅ клон `claude/catalog-module` |
| 2 | Purchasing и Inventory | 3 и 4 | ✅ клон `claude/purchasing-inventory-module` |
| 3 | Sales | 5 и 6 | ✅ клон `claude/sales-module` |
| 4 | Identity (вход, две роли) | 1-ви екран от CLAUDE.md | следва, клон `claude/identity-module` |
| 5 | Преглед | 7 | |

Всеки модул се прави в свой клон и се слива в `main` с pull request (виж `docs/architecture/0002-module-layout-and-branches.md`).
Вътре в модула всеки модел има своя папка на всяко ниво (Domain/Application/Infrastructure/Features/тестове).

Решения, които важат за всички модули:
- **потвърдено:** ключове `uuid` (`Guid.CreateVersion7()`), без външни ключове между схеми, snake_case имена,
  одит през общия interceptor (`AddModuleDbContext`), миграции по модул, прилагани при стартиране в Development;
- **потвърдено (стъпка 2):** наличност = сума от `StockMovement`; `IUnitOfWork` за транзакции между модули;
  покупна цена без ДДС, продажна с ДДС, надценка → предложена продажна цена;
  кодове при доставчика и корекционна разписка — по-късно (BACKLOG);
- **потвърдено (стъпка 3):** заключване на партидите (`SELECT … FOR UPDATE`), стоките се заключват винаги в един
  и същ ред (по Id), за да няма deadlock; цената на реда е от първата по FEFO партида;
- предстои: вход (Identity) — тогава `UserId` в продажби, доставки, движения и одит става истинският потребител.

## Известни особености

- Облачна сесия на Claude (Linux): .NET 10 се инсталира с `apt-get install -y dotnet-sdk-10.0`
  (сървърът на Microsoft е блокиран), Docker се пуска с `dockerd &`, `dotnet-ef` с `dotnet tool install --global dotnet-ef`.
- **Smart App Control (Windows)** може да блокира току-що компилирани празни DLL-и. Тестовете за типове ги пропускат.
  Ако `dotnet run` откаже да зареди DLL, това е причината.
- Тестовете се пускат с `dotnet test --solution Apteka.sln` (xunit v3 през Microsoft Testing Platform, виж `global.json`).
- Интеграционните тестове изискват работещ Docker Desktop.

## Включване на CI (GitHub Actions)

CI файлът е качен неактивен, защото токенът на `gh` нямаше разрешение `workflow`. За да го включиш:

```
gh auth refresh -h github.com -s workflow
mkdir .github\workflows
git mv docs\ci\ci.yml.disabled .github\workflows\ci.yml
git commit -am "Enable CI"
git push
```
