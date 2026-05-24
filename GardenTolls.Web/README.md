# Garden Tools — веб-застосунок

## Що потрібно на комп’ютері

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **SQL Server** — один із варіантів:
   - **LocalDB** (часто вже є з Visual Studio), або
   - **SQL Server Express**

Базу даних **створювати вручну не потрібно** — вона з’явиться автоматично при першому запуску.

## Запуск

```bash
cd GardenTolls.Web
dotnet run
```

При **першому** запуску (якщо бази `GardenTools` ще немає):

1. Застосунок підключиться до SQL Server з `appsettings.json`
2. Імпортує повну базу з файлу `Database/GardenTools.bacpac` (усі таблиці, дані, тригери, зв’язки)
3. Це займає приблизно **1–3 хвилини**
4. Далі відкрийте в браузері адресу з консолі `https://localhost:4цифри

При наступних запусках імпорт **пропускається** — база вже існує.

## Рядок підключення

За замовчуванням (`appsettings.json`):

```text
Server=(localdb)\mssqllocaldb;Database=GardenTools;...
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=ВАШ_КОМП'ЮТЕР\\SQLEXPRESS;Database=GardenTools;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

## Що в комплекті в репозиторії

| Файл | Опис |
|------|------|
| `Database/GardenTools.bacpac` | Повний знімок БД для автоматичного розгортання |
| `Data/DatabaseBootstrap.cs` | Імпорт BACPAC при першому `dotnet run` |
| `Data/DbSeeder.cs` | Додаткові оновлення (нові колонки/акції), якщо потрібно |

## Оновити BACPAC після змін у БД

На машині (потрібен [SqlPackage](https://learn.microsoft.com/sql/tools/sqlpackage)):

```bash
sqlpackage /Action:Export ^
  /SourceConnectionString:"Server=ВАШ_СЕРВЕР;Database=GardenTools;Trusted_Connection=True;TrustServerCertificate=True;" ^
  /TargetFile:"Database\GardenTools.bacpac"
```

Потім закомітьте оновлений `GardenTools.bacpac`.

## Документація по коду

Див. [DOKUMENTACIYA.md](./DOKUMENTACIYA.md) — структура проєкту, сторінки, сервіси.
