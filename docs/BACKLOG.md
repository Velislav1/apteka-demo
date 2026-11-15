# Backlog

Идеи и бъдещи модули. Не се правят в демото (виж `CLAUDE.md`).

## Бъдещи модули

Всеки следва шаблона `Contracts / Domain / Application / Infrastructure` със собствена схема
(виж `docs/architecture/0001-modular-monolith.md`).

| Модул | Какво прави |
|---|---|
| `Prescriptions` | Електронни рецепти и връзка с НЗИС |
| `Nhif` | Отчети към НЗОК, продажби по `SaleType.Nhif`, отделен склад по НЗОК |
| `Fiscal` | Фискален принтер и касови бележки |
| `Verification` | Проверка на лекарства (БОВЛ) |
| `Reporting` | Справки (оборот, наличности, срокове на годност) |
| `Agent` | Локален .NET агент на касата (принтер, четци и други устройства) |

## Технически бележки

- Общ `IUnitOfWork` в `Apteka.Infrastructure.Common`: една Npgsql връзка и транзакция, споделена между
  `DbContext`-ите на модулите (нужно за правило 4: заключване на партида и запис в една транзакция).
- EF interceptor за `AuditLog` (правило 7).
- Единични цени с 4 знака (`numeric(12,4)`): `Money` е на 2 знака, затова е нужен отделен тип за единична цена.
- Smart App Control на Windows може да блокира нови непознати DLL-и при `dotnet run` и `dotnet test`.
  Тестовете за типове пропускат блокирани сборки.
- bUnit тестове за Blazor компоненти — добавят се с първите екрани.
- Миграции по модул (`dotnet ef migrations add ... --project ...Infrastructure --startup-project src/Host/Apteka.Web`).
