## Загальна схема

```
GardenTolls/
├── GardenTolls.Web/          ← основний сайт (ASP.NET Core 8 MVC)
└── (SQL Server, БД GardenTools)
```

**GardenTolls.Web**: сервер рендерить HTML-сторінки, кошик і wishlist зберігаються в **сесії** браузера, дані товарів/замовлень — у **SQL Server**.

---

## GardenTolls.Web — шари

| Папка / файл | Призначення |
|--------------|-------------|
| `Program.cs` | Запуск: DI, cookie-auth, сесія, маршрути, виклик `DbSeeder` при старті |
| `Controllers/` | HTTP-запити: що показати і куди перенаправити |
| `Views/` | Razor-шаблони сторінок (HTML) |
| `Areas/Admin/` | Окрема зона для адмін-панелі (`/Admin/Dashboard/...`) |
| `Services/` | Бізнес-логіка (кошик, каталог, замовлення, адмін, пошук, модерація) |
| `Repositories/` | Читання/запис у БД через Entity Framework |
| `Models/` | Таблиці БД (Product, Order, User, …) |
| `ViewModels/` | Дані для конкретної сторінки (не плутати з Models) |
| `Data/AppDbContext.cs` | EF Core — доступ до таблиць |
| `Data/DbSeeder.cs` | Початкові дані при старті (акції, сезонні товари, міграції колонок) |
| `Infrastructure/` | Допоміжне: час Києва, фільтр сезону, claims користувача |
| `ViewComponents/` | Невеликі UI-блоки (наприклад, підказки пошуку в навбарі) |
| `wwwroot/` | CSS, JS, картинки (статичні файли) |

**Потік запиту:** `Controller` → `Service` → `Repository` / `DbContext` → `View` + `ViewModel`.

---

## Публічні сторінки (Controllers + Views)

### `HomeController` — головна та загальне

| URL (приклад) | View | Що робить |
|---------------|------|-----------|
| `/` | `Views/Home/Index.cshtml` | Головна: категорії, рекомендовані товари, блок акцій |
| `/Home/Promotions` | `Promotions.cshtml` | Список усіх акцій |
| `/Home/Search?q=...` | `Search.cshtml` | Пошук товарів і акцій; лог запитів у `SearchQueryLogs` |
| `/Home/Contacts` | `Contacts.cshtml` | Форма зворотного зв’язку (симуляція) |

**Сервіси:** `ICatalogService`, `ICategoryRepository`, `IPromotionRepository`, `ISearchService`.

---

### `ProductsController` — каталог і картка товару

| URL | View | Що робить |
|-----|------|-----------|
| `/Products` | `Views/Products/Index.cshtml` | Каталог: фільтри (категорія, ціна, бренд, сезон), сортування, сітка карток |
| `/Products/Details/{id}` | `Details.cshtml` | Сторінка товару: фото, ціна, знижка, відгуки, кнопка кошика |
| POST `AddReview` | — | Додати відгук (авторизований користувач) |

**Картка в сітці:** `Views/Shared/_ProductCard.cshtml` (варіант `catalog`).

**Сервіс:** `ICatalogService` — фільтрація, акційні ціни, дерево відгуків.

**CSS:** `wwwroot/css/catalog.css`, `product-details.css`.

---

### `PromotionsController` — товари в акції

| URL | View | Що робить |
|-----|------|-----------|
| `/Promotions/Details/{id}` | `Views/Promotions/Details.cshtml` | Каталог тільки товарів акції + фільтри; `?promoId=` для повернення з картки товару |

---

### `CartController` — кошик і обране

| Дія | Що робить |
|-----|-----------|
| `Index` | `Views/Cart/Index.cshtml` — перегляд кошика, зміна кількості |
| `ToggleCart` | Додати в кошик **або** прибрати (якщо вже в кошику); toast + redirect назад |
| `Add` / `Remove` | Окремі дії (залишені для сумісності) |
| `Wishlist` | `Views/Cart/Wishlist.cshtml` — список бажань (ID в сесії) |
| `ToggleWishlist` | Додати/прибрати з обраного |

**Сервіс:** `ICartService` — JSON кошика в `Session`, методи `GetCartProductIds` для стану «В кошику» на картках.

**Дані кошика не в БД** до оформлення замовлення.

---

### `AccountController` — вхід і профіль

| Сторінка | Призначення |
|----------|-------------|
| `Login` / `Register` | Cookie-authentication |
| `Profile` | Редагування профілю, аватар (base64 у `Users`) |
| `ForgotPassword` / `ResetPassword` | Відновлення пароля (симуляція через сесію) |

**Сервіс:** `IAuthService`, `IUserRepository`.

---

### `OrdersController` — замовлення

| Сторінка | Призначення |
|----------|-------------|
| `Checkout` | Оформлення з кошика |
| `Payment` | Симуляція оплати карткою |
| `Success` / `Receipt` | Підтвердження і чек |
| `History` / `Details` | Історія замовлень користувача |

**Сервіс:** `IOrderService` — створення `Order` + `OrderDetails` у БД, списання складу.

---

## Спільні елементи UI

| Файл | Призначення |
|------|-------------|
| `Views/Shared/_Layout.cshtml` | Навбар, пошук, кошик, меню користувача, footer |
| `Views/Shared/_ProductCard.cshtml` | Картка товару (каталог / компактна) |
| `Views/Shared/_Alerts.cshtml` | Toast-повідомлення (успіх/помилка) |
| `Views/Shared/_ReviewThread.cshtml` | Вкладені відгуки |
| `ViewComponents/SearchSuggestions` | Випадаюче меню пошуку (шаблони, історія, популярні) |
| `wwwroot/css/site.css` | Глобальні стилі, навбар, пошук, меню користувача |
| `wwwroot/js/site.js` | Бургер, toast, панель пошуку |

---

## Адмін-панель (`Areas/Admin`)

**URL:** `/Admin/Dashboard/...`  
**Доступ:** ролі Admin або Manager (`Program.cs` → policy `AdminOrManager`).  
**Layout:** `Views/Shared/_AdminLayout.cshtml`.

| Дія | Сторінка | Навіщо |
|-----|----------|--------|
| `Index` | `Dashboard/Index.cshtml` | Зведення: товари, користувачі, замовлення, низький склад |
| `Products` | `Products.cshtml` | Список товарів: пошук, фільтр категорія/постачальник, сортування |
| `EditProduct` | `EditProduct.cshtml` | Створення/редагування товару, склад |
| `Categories` | `Categories.cshtml` | Категорії: CRUD, приховування |
| `EditCategory` | `EditCategory.cshtml` | Форма категорії |
| `Users` | `Users.cshtml` | Користувачі, блокування, відгуки |
| `Orders` | `Orders.cshtml` | Замовлення, зміна статусу |
| `Reviews` | `Reviews.cshtml` | Модерація відгуків + `ContentModerationService` |
| `Promotions` | `Promotions.cshtml` | Список акцій, увімк./вимк. |
| `EditPromotion` | `EditPromotion.cshtml` | Акція, товари, масове додавання за фільтром |
| `Analytics` | `Analytics.cshtml` | Графіки, звіти HTML/PDF |

**Сервіс:** `IAdminService` (в `Services/Services.cs` клас `AdminService`).

---

## Важливі сервіси

| Сервіс | Файл | Роль |
|--------|------|------|
| `CatalogService` | `Services/Services.cs` | Каталог, деталі товару, відгуки, каталог акції |
| `CartService` | там же | Сесійний кошик і wishlist |
| `OrderService` | там же | Checkout, оплата, історія |
| `AdminService` | там же | Усе для адмінки |
| `SearchService` | `Services/SearchService.cs` | Лог і підказки пошуку (історія 3, популярні 2; підказки з каталогу лише якщо текст збігається з товарами/категоріями/акціями) |
| `ContentModerationService` | `ContentModerationService.cs` | Авто-прапорці для відгуків (тригери) |
| `AnalyticsService` | `Services/Services.cs` | Дані для аналітики |
| `ReportExportService` | `ReportExportService.cs` | HTML-звіти |

---

## База даних

**Підключення:** `appsettings.json` → `ConnectionStrings:DefaultConnection`.

**Основні таблиці:** `Products`, `Categories`, `Suppliers`, `Inventories`, `Promotions`, `PromotionProducts`, `Orders`, `OrderDetails`, `Users`, `Customers`, `Reviews`, `SearchQueryLogs`.

**Особливості:**

- Знижка на товар у акції часто рахується **тригером** `tr_CalculatePromotionalPrice` на `PromotionProducts` — у `AppDbContext` для цієї таблиці вказано `HasTrigger`.
- `DbSeeder` при старті: колонки профілю, сезонні товари, акції 2026, таблиця логів пошуку.

---

## Авторизація

- Cookie scheme, логін через `AccountController`.
- У claims зберігається `UserId` (`Infrastructure/ClaimsExtensions.cs`).
- Адмін-меню в навбарі: `User.IsAdminOrManager()`.

---


## Як запустити веб-сайт

```bash
cd GardenTolls.Web
dotnet watch run або dotnet run
```

Зауваж, що після зміни коду `dotnet watch` перезбирає проєкт автоматично, а `dotnet run` потрібно зупинити і запустити знову.

---