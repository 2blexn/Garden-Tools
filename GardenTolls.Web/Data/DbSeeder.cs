using GardenTolls.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GardenTolls.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await EnsureUserColumnsAsync(db);
        await EnsureSearchQueryLogsTableAsync(db);
        await SeedSeasonalProductsAsync(db);
        await SeedPromotions2026Async(db);
    }

    private static async Task EnsureSearchQueryLogsTableAsync(AppDbContext db)
    {
        const string sql = """
            IF OBJECT_ID(N'SearchQueryLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE SearchQueryLogs (
                    SearchQueryLogId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NULL,
                    Query NVARCHAR(200) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT FK_SearchQueryLogs_Users FOREIGN KEY (UserId) REFERENCES Users(UserId)
                );
                CREATE INDEX IX_SearchQueryLogs_UserId ON SearchQueryLogs(UserId);
                CREATE INDEX IX_SearchQueryLogs_CreatedAt ON SearchQueryLogs(CreatedAt DESC);
            END
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedSeasonalProductsAsync(AppDbContext db)
    {
        if (!await db.Suppliers.AnyAsync())
            return;

        var supplierId = await db.Suppliers.Select(s => s.SupplierID).FirstAsync();

        var categoryDefs = new (string Name, string Desc)[]
        {
            ("Зимовий догляд", "Інструменти та обладнання для зими"),
            ("Насіння", "Насіння овочів та квітів"),
            ("Розсада", "Розсада для посадки в сезон")
        };

        var categoryIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, desc) in categoryDefs)
        {
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (cat == null)
            {
                cat = new Category { Name = name, Description = desc, IsActive = true, CreatedAt = DateTime.UtcNow };
                db.Categories.Add(cat);
                await db.SaveChangesAsync();
            }
            categoryIds[name] = cat.CategoryID;
        }

        var products = new (string Sku, string Name, string Cat, string Desc, decimal Price, int Stock)[]
        {
            ("GT-WIN-001", "Снігоприбиральна лопата з алюмінієвим ковшом", "Зимовий догляд", "Широкий ковш для прибирання снігу на доріжках і ділянках.", 649m, 40),
            ("GT-WIN-002", "Скребок для льоду та снігу 60 см", "Зимовий догляд", "Міцний скребок для очищення сходів і тротуарів взимку.", 289m, 55),
            ("GT-WIN-003", "Піч дизельна для теплиці 8 кВт", "Зимовий догляд", "Обігрів теплиці взимку, термостат, безпечне згоряння.", 12490m, 8),
            ("GT-WIN-004", "Обігрівач інфрачервоний для теплиці 3 кВт", "Зимовий догляд", "Зимовий захист рослин у теплиці, економний режим.", 4590m, 12),
            ("GT-WIN-005", "Сіль технічна для доріжок 25 кг", "Зимовий догляд", "Антиожеледь для садових доріжок взимку.", 399m, 30),
            ("GT-SPR-001", "Насіння томатів весняних F1 0.5 г", "Насіння", "Весняна посадка томатів у теплицю та відкритий ґрунт.", 89m, 120),
            ("GT-SPR-002", "Насіння огірків партенокарпічних", "Насіння", "Весняне насіння огірків для раннього врожаю.", 75m, 95),
            ("GT-SPR-003", "Насіння перцю солодкого весняного", "Насіння", "Насіння перцю для висіву навесні.", 68m, 80),
            ("GT-SPR-004", "Насіння моркви Нантська 3", "Насіння", "Весняний посів моркви на грядках.", 42m, 150),
            ("GT-SPR-005", "Насіння квітів однорічних мікс", "Насіння", "Весняний мікс насіння для клумб і балконів.", 55m, 70),
            ("GT-SUM-001", "Розсада перцю для посадки влітку", "Розсада", "Літня розсада перцю, готова до висадки в грунт.", 35m, 60),
            ("GT-SUM-002", "Розсада баклажанів літня", "Розсада", "Розсада баклажанів для посадки влітку.", 38m, 45),
            ("GT-SUM-003", "Розсада капусти білокачанної літня", "Розсада", "Літня розсада капусти для пізнього врожаю.", 32m, 50),
            ("GT-SUM-004", "Розсада томатів пізніх сортів", "Розсада", "Розсада томатів для літньої висадки.", 36m, 55),
            ("GT-SUM-005", "Розсада базиліку в горщиках", "Розсада", "Літня розсада пряних трав для кухонного саду.", 28m, 80)
        };

        foreach (var (sku, name, catName, desc, price, stock) in products)
        {
            if (await db.Products.AnyAsync(p => p.SKU == sku))
                continue;

            if (!categoryIds.TryGetValue(catName, out var catId))
                continue;

            var product = new Product
            {
                ProductName = name,
                CategoryID = catId,
                SupplierID = supplierId,
                Description = desc,
                UnitPrice = price,
                SKU = sku,
                CreatedAt = DateTime.UtcNow
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            db.Inventories.Add(new Inventory
            {
                ProductID = product.ProductID,
                QuantityInStock = stock,
                ReorderLevel = Math.Max(5, stock / 10)
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserColumnsAsync(AppDbContext db)
    {
        const string sql = """
            IF COL_LENGTH('Users', 'ProfileImageBase64') IS NULL
                ALTER TABLE Users ADD ProfileImageBase64 NVARCHAR(MAX) NULL;
            IF COL_LENGTH('Users', 'CanWriteReviews') IS NULL
                ALTER TABLE Users ADD CanWriteReviews BIT NOT NULL CONSTRAINT DF_Users_CanWriteReviews DEFAULT 1;
            """;
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task SeedPromotions2026Async(AppDbContext db)
    {
        if (!await db.Suppliers.AnyAsync())
            return;

        var supplierId = await db.Suppliers.Select(s => s.SupplierID).FirstAsync();
        var productIds = await db.Products.Select(p => p.ProductID).Take(24).ToListAsync();
        if (productIds.Count < 4)
            return;

        var now = DateTime.UtcNow;
        var nightEnd = now.AddHours(48);

        var definitions = new[]
        {
            ("Весняна акція 2026", "Знижки на інструменти для весняних робіт у саду.", new DateTime(2026, 3, 1), new DateTime(2026, 5, 31), 15m, false),
            ("Літня акція 2026", "Полив, насоси та садовий догляд — вигідні ціни влітку.", new DateTime(2026, 6, 1), new DateTime(2026, 8, 31), 20m, false),
            ("Осіння акція 2026", "Підготовка саду до зими: обрізка, компост, захист.", new DateTime(2026, 9, 1), new DateTime(2026, 11, 30), 18m, false),
            ("Зимова акція 2026–2027", "Зимовий догляд за технікою та зберігання інструментів.", new DateTime(2026, 12, 1), new DateTime(2027, 2, 28), 12m, false),
            ("Нічна акція", "Обмежена пропозиція лише на 48 годин — встигніть скористатися!", now.AddHours(-1), nightEnd, 25m, true)
        };

        for (var i = 0; i < definitions.Length; i++)
        {
            var (name, desc, start, end, discount, isNight) = definitions[i];
            var existing = await db.Promotions
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.PromotionName == name);

            if (existing != null && existing.PromotionProducts.Any())
                continue;

            if (existing != null)
            {
                await LinkProductsToPromotionAsync(db, existing, productIds, i, discount);
                if (isNight)
                {
                    existing.EndDate = nightEnd;
                    existing.StartDate = now.AddHours(-1);
                    await db.SaveChangesAsync();
                }
                continue;
            }

            var promotion = new Promotion
            {
                SupplierID = supplierId,
                PromotionName = name,
                Description = desc,
                StartDate = start,
                EndDate = end,
                IsActive = true
            };
            db.Promotions.Add(promotion);
            await db.SaveChangesAsync();

            await LinkProductsToPromotionAsync(db, promotion, productIds, i, discount);

            if (isNight)
            {
                promotion.EndDate = nightEnd;
                promotion.StartDate = now.AddHours(-1);
                await db.SaveChangesAsync();
            }
        }

        var existingNight = await db.Promotions.FirstOrDefaultAsync(p => p.PromotionName == "Нічна акція");
        if (existingNight != null)
        {
            existingNight.EndDate = nightEnd;
            existingNight.StartDate = now.AddHours(-1);
            existingNight.IsActive = true;
            await db.SaveChangesAsync();
        }
    }

    private static async Task LinkProductsToPromotionAsync(
        AppDbContext db,
        Promotion promotion,
        List<int> productIds,
        int index,
        decimal discount)
    {
        var slice = productIds.Skip((index * 4) % productIds.Count).Take(4).ToList();
        if (slice.Count < 4)
            slice = productIds.Take(4).ToList();

        foreach (var pid in slice)
        {
            if (await db.PromotionProducts.AnyAsync(pp =>
                    pp.PromotionID == promotion.PromotionID && pp.ProductID == pid))
                continue;

            db.PromotionProducts.Add(new PromotionProduct
            {
                PromotionID = promotion.PromotionID,
                ProductID = pid,
                DiscountPercentage = discount + (pid % 3),
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }
}
