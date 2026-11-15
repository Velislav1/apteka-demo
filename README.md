# Аптечна програма (демо)

Нова програма за българска аптека. Сега е само **скелет**: модулен монолит, който се компилира,
има тестове и показва начална страница, свързана с PostgreSQL. Бизнес функционалност още няма.

Подробности за правилата и модела: [CLAUDE.md](CLAUDE.md). Защо е така: [docs/architecture/0001-modular-monolith.md](docs/architecture/0001-modular-monolith.md).

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
4. Отвори http://localhost:5080

## Как се тества

```powershell
dotnet test --solution Apteka.sln
```

Интеграционните тестове стартират собствена PostgreSQL чрез Testcontainers, затова Docker Desktop трябва да върви.
Архитектурните правила се проверяват от `tests/Apteka.ArchitectureTests`.

## Структура

Виж раздела „Структура“ в [CLAUDE.md](CLAUDE.md).

## Тестови потребители

Още няма (seed данни ще се добавят с модул Identity).
