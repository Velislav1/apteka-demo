# Къде сме спрели и как да продължиш

Състояние към 2026-11-15.

## Готово

- Инсталирани инструменти: .NET 10 SDK, Git, Docker Desktop (с WSL 2), `dotnet-ef`, C# Dev Kit.
- Скелет на **модулен монолит**: 6 модула (Catalog, Purchasing, Inventory, Sales, Identity, Audit),
  по 4 проекта всеки (Contracts, Domain, Application, Infrastructure), SharedKernel, Infrastructure.Common,
  Blazor host с начална страница на български, свързана с PostgreSQL.
- 36 теста минават (unit, архитектурни, интеграционни с Testcontainers).
- Бизнес функционалност още няма: няма таблици, екрани, вход, seed.

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
3. Провери: `docker compose up -d`, после `dotnet test --solution Apteka.sln`. Трябва да са 36 зелени теста.
4. Отвори папката във VS Code, стартирай Claude Code. `CLAUDE.md` се чете автоматично.
5. Git идентичност на новия компютър: `git config --global user.name "..."` и `git config --global user.email "..."`.

## Какво следва

Промптовете са в `PROMPTS.md`. Продължаваме модул по модул, като към всеки промпт се добавя:
„Спазвай модулната структура и правилата в CLAUDE.md.“

| Стъпка | Модул | Промпт |
|---|---|---|
| 1 | Catalog | 2 |
| 2 | Purchasing и Inventory | 3 и 4 |
| 3 | Sales | 5 и 6 |
| 4 | Преглед | 7 |

Промпт 1 (общ план на базата) е прескочен. Промпт 2 трябва да включи и таблиците на Catalog,
първата миграция и seed с 50 измислени стоки. Claude първо дава кратък план и чака одобрение.

Решения, които важат за всички модули (приети като препоръка, виж `docs/architecture/0001-modular-monolith.md`):
- ключове `uuid` (`Guid.CreateVersion7()`), без външни ключове между схеми;
- наличност = сума от `StockMovement`, с заключване на реда на партидата;
- общ `IUnitOfWork` за транзакции между модули и общ audit interceptor, когато първата функционалност ги поиска.

## Известни особености

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
