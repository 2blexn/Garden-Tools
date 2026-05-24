using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace GardenTolls.Web.Data;

/// <summary>
/// Створює базу GardenTools з вбудованого BACPAC, якщо її ще немає на SQL Server.
/// </summary>
public static class DatabaseBootstrap
{
    public static async Task EnsureDatabaseAsync(IConfiguration configuration, ILogger logger, CancellationToken ct = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Рядок підключення DefaultConnection не налаштовано.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            logger.LogWarning("У рядку підключення не вказано Database=.");
            return;
        }

        builder.InitialCatalog = "master";
        var masterConnectionString = builder.ConnectionString;

        if (await DatabaseExistsAsync(masterConnectionString, databaseName, ct))
        {
            logger.LogInformation("База даних {Database} вже існує — імпорт BACPAC пропущено.", databaseName);
            return;
        }

        var bacpacPath = ResolveBacpacPath();
        if (!File.Exists(bacpacPath))
        {
            throw new FileNotFoundException(
                $"Файл бази даних не знайдено: {bacpacPath}. Переконайтеся, що GardenTools.bacpac є в папці Database проєкту.",
                bacpacPath);
        }

        logger.LogInformation(
            "База {Database} не знайдена. Імпорт з BACPAC (таблиці, дані, тригери) — зазвичай 1–3 хв...",
            databaseName);

        var dac = new DacServices(masterConnectionString);
        dac.Message += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Message.Message))
                logger.LogInformation("[SQL] {Message}", e.Message.Message);
        };

        await Task.Run(() =>
        {
            using var package = BacPackage.Load(bacpacPath);
            dac.ImportBacpac(package, databaseName);
        }, ct);

        logger.LogInformation("База даних {Database} успішно створена з BACPAC.", databaseName);
    }

    private static async Task<bool> DatabaseExistsAsync(string masterConnectionString, string databaseName, CancellationToken ct)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @db";
        command.Parameters.AddWithValue("@db", databaseName);
        var count = (int)(await command.ExecuteScalarAsync(ct) ?? 0);
        return count > 0;
    }

    private static string ResolveBacpacPath()
    {
        var fileName = Path.Combine("Database", "GardenTools.bacpac");
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", fileName))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return candidates[0];
    }
}
