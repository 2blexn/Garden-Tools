using System.Text.Json;
using GardenTolls.Web.Data;
using GardenTolls.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using GardenTolls.Web.Models;
using GardenTolls.Web.Repositories;
using GardenTolls.Web.ViewModels.Admin;
using GardenTolls.Web.ViewModels.Auth;
using GardenTolls.Web.ViewModels.Cart;
using GardenTolls.Web.ViewModels.Orders;
using GardenTolls.Web.ViewModels.Products;
using GardenTolls.Web.ViewModels.Promotions;

namespace GardenTolls.Web.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;

    public AuthService(IUserRepository users, ICustomerRepository customers)
    {
        _users = users;
        _customers = customers;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model)
    {
        if (await _users.EmailExistsAsync(model.Email))
            return (false, "Користувач з такою електронною поштою вже існує");

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            FirstName = model.FirstName,
            LastName = model.LastName,
            Phone = model.Phone,
            Role = UserRole.Customer,
            IsActive = true,
            RegistrationDate = DateTime.UtcNow
        };
        await _users.AddAsync(user);

        var customer = new Customer
        {
            FirstName = model.FirstName ?? model.Username,
            LastName = model.LastName ?? "",
            Email = model.Email,
            Phone = model.Phone ?? "",
            RegistrationDate = DateTime.UtcNow
        };
        await _customers.AddAsync(customer);
        user.CustomerId = customer.CustomerID;
        await _users.UpdateAsync(user);

        return (true, "Реєстрація успішна");
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model)
    {
        var user = await _users.GetByEmailAsync(model.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            return (false, "Невірна електронна пошта або пароль", null);
        if (!user.IsActive)
            return (false, "Обліковий запис заблоковано", null);

        user.LastLoginDate = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        return (true, "Вхід успішний", user);
    }

    public Task<(bool Success, string Message)> RequestPasswordResetAsync(ForgotPasswordViewModel model, ISession session)
    {
        session.SetString(SessionKeys.PasswordResetEmail, model.Email);
        session.SetString($"{SessionKeys.PasswordResetEmail}:token", Guid.NewGuid().ToString("N"));
        return Task.FromResult((true, "Посилання для відновлення пароля згенеровано. Перейдіть на сторінку скидання."));
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordViewModel model, ISession session)
    {
        var storedEmail = session.GetString(SessionKeys.PasswordResetEmail);
        var token = session.GetString($"{SessionKeys.PasswordResetEmail}:token");
        if (storedEmail != model.Email || token != model.Token)
            return (false, "Невірний токен або email");

        var user = await _users.GetByEmailAsync(model.Email);
        if (user == null) return (false, "Користувача не знайдено");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await _users.UpdateAsync(user);
        session.Remove(SessionKeys.PasswordResetEmail);
        return (true, "Пароль оновлено");
    }

    public async Task<ProfileEditViewModel?> GetProfileAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null) return null;
        return new ProfileEditViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            Address = user.Address,
            City = user.City,
            Country = user.Country,
            PostalCode = user.PostalCode,
            ProfileImageBase64 = user.ProfileImageBase64
        };
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(ProfileEditViewModel model, IFormFile? profileImage = null)
    {
        var user = await _users.GetByIdAsync(model.UserId);
        if (user == null) return (false, "Користувача не знайдено");

        if (await _users.EmailExistsAsync(model.Email, model.UserId))
            return (false, "Email вже зайнятий");

        user.Username = model.Username;
        user.Email = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Phone = model.Phone;
        user.Address = model.Address;
        user.City = model.City;
        user.Country = model.Country;
        user.PostalCode = model.PostalCode;

        if (model.RemoveProfileImage)
            user.ProfileImageBase64 = null;
        else if (profileImage != null && profileImage.Length > 0)
        {
            if (profileImage.Length > 512_000)
                return (false, "Зображення занадто велике (макс. 500 КБ)");
            if (!profileImage.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return (false, "Дозволені лише файли зображень");

            await using var ms = new MemoryStream();
            await profileImage.CopyToAsync(ms);
            user.ProfileImageBase64 = Convert.ToBase64String(ms.ToArray());
        }

        if (!string.IsNullOrEmpty(model.NewPassword))
        {
            if (string.IsNullOrEmpty(model.CurrentPassword) || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                return (false, "Невірний поточний пароль");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        }

        await _users.UpdateAsync(user);
        return (true, "Профіль оновлено");
    }
}

public class CatalogService : ICatalogService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ISupplierRepository _suppliers;
    private readonly IPromotionRepository _promotions;
    private readonly IReviewRepository _reviews;
    private readonly IUserRepository _users;
    private readonly IContentModerationService _moderation;

    public CatalogService(
        IProductRepository products,
        ICategoryRepository categories,
        ISupplierRepository suppliers,
        IPromotionRepository promotions,
        IReviewRepository reviews,
        IUserRepository users,
        IContentModerationService moderation)
    {
        _products = products;
        _categories = categories;
        _suppliers = suppliers;
        _promotions = promotions;
        _reviews = reviews;
        _users = users;
        _moderation = moderation;
    }

    public async Task<ProductListViewModel> GetCatalogAsync(ProductFilterViewModel filter)
    {
        var (items, total) = await _products.GetFilteredAsync(filter);
        var vm = new ProductListViewModel
        {
            Filter = filter,
            TotalCount = total,
            Categories = (await _categories.GetAllActiveAsync()).Select(c => new CategoryOption { Id = c.CategoryID, Name = c.Name }).ToList(),
            Suppliers = (await _suppliers.GetActiveAsync()).Select(s => new SupplierOption { Id = s.SupplierID, Name = s.CompanyName }).ToList()
        };

        foreach (var p in items)
        {
            var discount = await _promotions.GetActiveDiscountPercentAsync(p.ProductID);
            vm.Products.Add(MapCard(p, discount));
        }
        return vm;
    }

    public async Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id, int? fromPromotionId = null)
    {
        var product = await _products.GetByIdAsync(id, includeReviews: true);
        if (product == null) return null;

        var discount = await _promotions.GetActiveDiscountPercentAsync(id);
        var flatReviews = await _reviews.GetAllApprovedForProductAsync(id);
        var reviews = BuildReviewTree(flatReviews);
        var recs = await _products.GetRecommendationsAsync(product.CategoryID, id);

        string? promoName = null;
        if (fromPromotionId.HasValue)
        {
            var promo = await _promotions.GetByIdAsync(fromPromotionId.Value);
            if (promo != null) promoName = promo.PromotionName;
            else fromPromotionId = null;
        }

        var vm = new ProductDetailsViewModel
        {
            ProductId = product.ProductID,
            Name = product.ProductName,
            Description = product.Description ?? "",
            CategoryName = product.Category?.Name ?? "",
            SupplierName = product.Supplier?.CompanyName ?? "",
            UnitPrice = product.UnitPrice,
            DiscountPercent = discount,
            DiscountedPrice = discount.HasValue ? product.UnitPrice * (1 - discount.Value / 100m) : null,
            Sku = product.SKU,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            ImageBase64 = product.ImageBase64,
            Stock = product.Inventory == null ? -1 : product.Inventory.QuantityInStock,
            AverageRating = flatReviews.Count > 0 && flatReviews.Any(r => r.Rating.HasValue)
                ? flatReviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                : null,
            Reviews = reviews,
            FromPromotionId = fromPromotionId,
            FromPromotionName = promoName
        };

        foreach (var r in recs)
        {
            var d = await _promotions.GetActiveDiscountPercentAsync(r.ProductID);
            vm.Recommendations.Add(MapCard(r, d));
        }
        return vm;
    }

    public async Task<string> AddReviewAsync(int productId, int userId, ReviewInputViewModel input)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("Користувача не знайдено");
        if (!user.CanWriteReviews)
            throw new InvalidOperationException("Вам обмежено можливість писати відгуки");

        byte? rating = input.ParentReviewId.HasValue ? null : input.Rating;
        if (input.ParentReviewId.HasValue)
        {
            var parent = await _reviews.GetByIdAsync(input.ParentReviewId.Value);
            if (parent == null || parent.ProductID != productId)
                throw new InvalidOperationException("Батьківський відгук не знайдено");
        }

        var moderation = _moderation.Analyze(input.Comment);
        var autoApprove = !moderation.IsHighRisk && !moderation.IsMediumRisk;

        await _reviews.AddAsync(new Review
        {
            ProductID = productId,
            UserID = userId,
            ParentReviewID = input.ParentReviewId,
            Rating = rating,
            Comment = input.Comment,
            ReviewDate = DateTime.UtcNow,
            IsApproved = autoApprove
        });

        if (!autoApprove)
            return "Надіслано на модерацію. З'явиться після перевірки адміністратором.";
        return input.ParentReviewId.HasValue ? "Відповідь опубліковано" : "Відгук опубліковано";
    }

    private static List<ReviewViewModel> BuildReviewTree(List<Review> all, int? parentId = null) =>
        all.Where(r => r.ParentReviewID == parentId)
            .OrderByDescending(r => r.ReviewDate)
            .Select(r =>
            {
                var vm = MapReview(r);
                vm.Replies = BuildReviewTree(all, r.ReviewID);
                return vm;
            })
            .ToList();

    public async Task<PromotionCatalogViewModel?> GetPromotionCatalogAsync(int promotionId, ProductFilterViewModel filter)
    {
        var promotion = await _promotions.GetByIdAsync(promotionId);
        if (promotion == null) return null;

        var items = await _promotions.GetProductsForPromotionAsync(promotionId);
        var cards = new List<PromotionProductCardViewModel>();

        foreach (var pp in items)
        {
            var p = pp.Product;
            if (p == null) continue;
            var card = MapCard(p, pp.DiscountPercentage);
            cards.Add(new PromotionProductCardViewModel
            {
                ProductId = card.ProductId,
                Name = card.Name,
                Description = card.Description,
                CategoryName = card.CategoryName,
                SupplierName = card.SupplierName,
                UnitPrice = card.UnitPrice,
                DiscountPercent = pp.DiscountPercentage,
                DiscountedPrice = p.UnitPrice * (1 - pp.DiscountPercentage / 100m),
                AverageRating = card.AverageRating,
                ImageBase64 = card.ImageBase64,
                Stock = card.Stock,
                PromotionDiscountPercent = pp.DiscountPercentage
            });
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var q = filter.Search.Trim();
            cards = cards.Where(c =>
                c.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (c.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        if (filter.MinPrice.HasValue)
            cards = cards.Where(c => c.DisplayPrice >= filter.MinPrice.Value).ToList();
        if (filter.MaxPrice.HasValue)
            cards = cards.Where(c => c.DisplayPrice <= filter.MaxPrice.Value).ToList();

        cards = filter.SortBy switch
        {
            "price" => filter.SortDesc
                ? cards.OrderByDescending(c => c.DisplayPrice).ToList()
                : cards.OrderBy(c => c.DisplayPrice).ToList(),
            _ => filter.SortDesc
                ? cards.OrderByDescending(c => c.Name).ToList()
                : cards.OrderBy(c => c.Name).ToList()
        };

        var isNight = promotion.PromotionName.Contains("Нічна", StringComparison.OrdinalIgnoreCase);

        return new PromotionCatalogViewModel
        {
            PromotionId = promotion.PromotionID,
            PromotionName = promotion.PromotionName,
            Description = promotion.Description,
            StartDate = promotion.StartDate,
            EndDate = promotion.EndDate,
            MaxDiscountPercent = items.Count > 0 ? items.Max(pp => pp.DiscountPercentage) : null,
            IsNightFlash = isNight,
            Filter = filter,
            Products = cards
        };
    }

    private static ReviewViewModel MapReview(Review r) => new()
    {
        ReviewId = r.ReviewID,
        UserId = r.UserID,
        Author = FormatUserName(r.User),
        AuthorInitial = GetUserInitial(r.User),
        Rating = r.Rating,
        Comment = r.Comment,
        ReviewDate = r.ReviewDate,
        IsVerifiedPurchase = r.IsVerifiedPurchase,
        ProfileImageBase64 = r.User?.ProfileImageBase64,
        Replies = []
    };

    private static string FormatUserName(User? user)
    {
        if (user == null) return "Користувач";
        var full = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrEmpty(full) ? user.Username : full;
    }

    private static string GetUserInitial(User? user)
    {
        var name = FormatUserName(user);
        return string.IsNullOrEmpty(name) ? "К" : name[..1].ToUpperInvariant();
    }

    private static ProductCardViewModel MapCard(Product p, decimal? discount) => new()
    {
        ProductId = p.ProductID,
        Name = p.ProductName,
        Description = p.Description,
        CategoryName = p.Category?.Name ?? "",
        SupplierName = p.Supplier?.CompanyName ?? "",
        UnitPrice = p.UnitPrice,
        DiscountPercent = discount,
        DiscountedPrice = discount.HasValue ? p.UnitPrice * (1 - discount.Value / 100m) : null,
        AverageRating = p.Reviews.Count > 0 ? p.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value) : null,
        ImageBase64 = p.ImageBase64,
        Stock = p.Inventory == null ? -1 : p.Inventory.QuantityInStock
    };
}

public class CartService : ICartService
{
    private readonly IProductRepository _products;
    private readonly IPromotionRepository _promotions;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CartService(IProductRepository products, IPromotionRepository promotions)
    {
        _products = products;
        _promotions = promotions;
    }

    public CartViewModel GetCart(ISession session)
    {
        var items = GetCartItems(session);
        return new CartViewModel { Items = items };
    }

    public async Task<bool> AddToCartAsync(ISession session, int productId, int quantity = 1)
    {
        var product = await _products.GetByIdAsync(productId);
        if (product == null) return false;

        var stock = product.Inventory == null ? -1 : product.Inventory.QuantityInStock;
        if (stock == 0) return false;

        var discount = await _promotions.GetActiveDiscountPercentAsync(productId) ?? 0;
        var items = GetCartItems(session);
        var existing = items.FirstOrDefault(i => i.ProductId == productId);
        var newQty = (existing?.Quantity ?? 0) + quantity;
        if (stock > 0 && newQty > stock) newQty = stock;

        if (existing != null)
            existing.Quantity = newQty;
        else
            items.Add(new CartItemViewModel
            {
                ProductId = productId,
                ProductName = product.ProductName,
                Description = product.Description,
                ImageBase64 = product.ImageBase64,
                UnitPrice = product.UnitPrice,
                DiscountPercent = discount,
                Quantity = newQty,
                Stock = stock > 0 ? stock : 999
            });
        SaveCart(session, items);
        return true;
    }

    public Task UpdateQuantityAsync(ISession session, int productId, int quantity)
    {
        var items = GetCartItems(session);
        var item = items.FirstOrDefault(i => i.ProductId == productId);
        if (item == null) return Task.CompletedTask;
        if (quantity <= 0) items.Remove(item);
        else item.Quantity = quantity;
        SaveCart(session, items);
        return Task.CompletedTask;
    }

    public Task RemoveFromCartAsync(ISession session, int productId)
    {
        var items = GetCartItems(session);
        items.RemoveAll(i => i.ProductId == productId);
        SaveCart(session, items);
        return Task.CompletedTask;
    }

    public void ClearCart(ISession session) => session.Remove(SessionKeys.Cart);

    public List<int> GetWishlist(ISession session)
    {
        var json = session.GetString(SessionKeys.Wishlist);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<int>>(json, JsonOpts) ?? [];
    }

    public void ToggleWishlist(ISession session, int productId)
    {
        var list = GetWishlist(session);
        if (list.Contains(productId)) list.Remove(productId);
        else list.Add(productId);
        session.SetString(SessionKeys.Wishlist, JsonSerializer.Serialize(list, JsonOpts));
    }

    public int GetWishlistCount(ISession session) => GetWishlist(session).Count;

    public HashSet<int> GetCartProductIds(ISession session) =>
        GetCartItems(session).Select(i => i.ProductId).ToHashSet();

    public async Task<WishlistViewModel> GetWishlistPageAsync(ISession session)
    {
        var ids = GetWishlist(session);
        var vm = new WishlistViewModel();
        foreach (var id in ids)
        {
            var p = await _products.GetByIdAsync(id);
            if (p == null) continue;
            var discount = await _promotions.GetActiveDiscountPercentAsync(id);
            vm.Items.Add(MapCard(p, discount));
        }
        return vm;
    }

    private static ProductCardViewModel MapCard(Product p, decimal? discount) => new()
    {
        ProductId = p.ProductID,
        Name = p.ProductName,
        Description = p.Description,
        CategoryName = p.Category?.Name ?? "",
        SupplierName = p.Supplier?.CompanyName ?? "",
        UnitPrice = p.UnitPrice,
        DiscountPercent = discount,
        DiscountedPrice = discount.HasValue ? p.UnitPrice * (1 - discount.Value / 100m) : null,
        AverageRating = p.Reviews.Count > 0 ? p.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value) : null,
        ImageBase64 = p.ImageBase64,
        Stock = p.Inventory == null ? -1 : p.Inventory.QuantityInStock
    };

    private static List<CartItemViewModel> GetCartItems(ISession session)
    {
        var json = session.GetString(SessionKeys.Cart);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItemViewModel>>(json, JsonOpts) ?? [];
    }

    private static void SaveCart(ISession session, List<CartItemViewModel> items) =>
        session.SetString(SessionKeys.Cart, JsonSerializer.Serialize(items, JsonOpts));
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IUserRepository _users;
    private readonly IProductRepository _products;
    private readonly IInventoryRepository _inventory;
    private readonly ICartService _cart;

    public OrderService(
        IOrderRepository orders,
        ICustomerRepository customers,
        IUserRepository users,
        IProductRepository products,
        IInventoryRepository inventory,
        ICartService cart)
    {
        _orders = orders;
        _customers = customers;
        _users = users;
        _products = products;
        _inventory = inventory;
        _cart = cart;
    }

    public async Task<CheckoutViewModel?> BuildCheckoutAsync(ISession session, int userId)
    {
        var cart = _cart.GetCart(session);
        if (cart.Items.Count == 0) return null;

        var user = await _users.GetByIdAsync(userId);
        return new CheckoutViewModel
        {
            TotalAmount = cart.Subtotal,
            ShippingAddress = user?.Address ?? "",
            ShippingCity = user?.City ?? "",
            ShippingCountry = user?.Country ?? "Україна",
            ShippingPostalCode = user?.PostalCode,
            Lines = cart.Items.Select(i => new CheckoutLineViewModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ImageBase64 = i.ImageBase64,
                Quantity = i.Quantity,
                UnitPrice = i.EffectiveUnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(CheckoutViewModel model, ISession session, int userId)
    {
        var cart = _cart.GetCart(session);
        if (cart.Items.Count == 0) return (false, "Кошик порожній", null);

        var user = await _users.GetByIdAsync(userId);
        if (user == null) return (false, "Користувача не знайдено", null);

        Customer? customer = null;
        if (user.CustomerId.HasValue)
            customer = await _customers.GetByUserIdAsync(userId);
        if (customer == null)
        {
            customer = await _customers.GetByEmailAsync(user.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    FirstName = user.FirstName ?? user.Username,
                    LastName = user.LastName ?? "",
                    Email = user.Email,
                    Phone = user.Phone ?? "",
                    RegistrationDate = DateTime.UtcNow
                };
                await _customers.AddAsync(customer);
            }
            user.CustomerId = customer.CustomerID;
            await _users.UpdateAsync(user);
        }

        var order = new Order
        {
            CustomerID = customer.CustomerID,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = "Pending",
            TotalAmount = cart.Subtotal,
            ShippingAddress = model.ShippingAddress,
            ShippingCity = model.ShippingCity,
            ShippingCountry = model.ShippingCountry,
            ShippingPostalCode = model.ShippingPostalCode,
            Notes = model.Notes
        };

        foreach (var item in cart.Items)
        {
            var inv = await _inventory.GetByProductIdAsync(item.ProductId);
            if (inv != null && inv.QuantityInStock < item.Quantity)
                return (false, $"Недостатньо товару на складі: {item.ProductName}", null);

            order.OrderDetails.Add(new OrderDetail
            {
                ProductID = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = item.DiscountPercent
            });
        }

        await _orders.AddAsync(order);

        foreach (var item in cart.Items)
        {
            var inv = await _inventory.GetByProductIdAsync(item.ProductId);
            if (inv != null)
            {
                inv.QuantityInStock -= item.Quantity;
                await _inventory.UpsertAsync(item.ProductId, inv.QuantityInStock, inv.ReorderLevel);
            }
        }

        _cart.ClearCart(session);
        return (true, "Замовлення створено", order.OrderID);
    }

    public async Task<(bool Success, string Message)> SimulatePaymentAsync(PaymentViewModel model)
    {
        var order = await _orders.GetByIdAsync(model.OrderId);
        if (order == null) return (false, "Замовлення не знайдено");

        var cardDigits = new string(model.CardNumber.Where(char.IsDigit).ToArray());
        if (cardDigits.Length < 12)
            return (false, "Невірні дані картки");

        order.PaymentStatus = "Paid";
        order.Status = "Processing";
        await _orders.UpdateAsync(order);
        return (true, "Оплату успішно симульовано");
    }

    public async Task<OrderHistoryViewModel> GetHistoryAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user?.CustomerId == null) return new OrderHistoryViewModel();

        var orders = await _orders.GetByCustomerIdAsync(user.CustomerId.Value);
        return new OrderHistoryViewModel
        {
            Orders = orders.Select(o => new OrderSummaryViewModel
            {
                OrderId = o.OrderID,
                OrderDate = o.OrderDate,
                Status = o.Status,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderDetails.Count
            }).ToList()
        };
    }

    public async Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId, int userId, bool isAdmin)
    {
        var order = await _orders.GetByIdAsync(orderId);
        if (order == null) return null;

        if (!isAdmin)
        {
            var user = await _users.GetByIdAsync(userId);
            if (user?.CustomerId != order.CustomerID) return null;
        }

        var customer = order.Customer;
        var customerName = customer != null
            ? $"{customer.FirstName} {customer.LastName}".Trim()
            : "";

        return new OrderDetailsViewModel
        {
            OrderId = order.OrderID,
            OrderDate = order.OrderDate,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = FormatPaymentMethod(order.PaymentMethod),
            TotalAmount = order.TotalAmount,
            CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Клієнт" : customerName,
            ShippingAddress = order.ShippingAddress,
            ShippingCity = order.ShippingCity,
            ShippingCountry = order.ShippingCountry,
            ShippingPostalCode = order.ShippingPostalCode,
            Lines = order.OrderDetails.Select(d => new CheckoutLineViewModel
            {
                ProductId = d.ProductID,
                ProductName = d.Product?.ProductName ?? $"#{d.ProductID}",
                ImageBase64 = d.Product?.ImageBase64,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                LineTotal = d.Quantity * d.UnitPrice * (1 - d.Discount / 100m)
            }).ToList()
        };
    }

    private static string FormatPaymentMethod(string? method) => method switch
    {
        "Card" => "Банківська карта",
        "Cash" => "Готівка при отриманні",
        _ => method ?? "—"
    };
}

public class AdminService : IAdminService
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly ISupplierRepository _suppliers;
    private readonly IInventoryRepository _inventory;
    private readonly IUserRepository _users;
    private readonly IOrderRepository _orders;
    private readonly IReviewRepository _reviews;
    private readonly IPromotionRepository _promotions;
    private readonly IContentModerationService _moderation;
    private readonly AppDbContext _db;

    public AdminService(
        IProductRepository products,
        ICategoryRepository categories,
        ISupplierRepository suppliers,
        IInventoryRepository inventory,
        IUserRepository users,
        IOrderRepository orders,
        IReviewRepository reviews,
        IPromotionRepository promotions,
        IContentModerationService moderation,
        AppDbContext db)
    {
        _products = products;
        _categories = categories;
        _suppliers = suppliers;
        _inventory = inventory;
        _users = users;
        _orders = orders;
        _reviews = reviews;
        _promotions = promotions;
        _moderation = moderation;
        _db = db;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync()
    {
        var lowStock = await _db.Inventories.CountAsync(i => i.QuantityInStock <= i.ReorderLevel);
        var revenue = await _db.Orders
            .Where(o => o.PaymentStatus == "Paid" && o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        return new AdminDashboardViewModel
        {
            ProductCount = await _db.Products.CountAsync(p => !p.IsDiscontinued),
            UserCount = await _db.Users.CountAsync(),
            OrderCount = await _db.Orders.CountAsync(),
            PendingReviews = await _db.Reviews.CountAsync(r => !r.IsApproved),
            LowStockCount = lowStock,
            RevenueLast30Days = revenue
        };
    }

    public async Task<AdminProductsPageViewModel> GetProductsAdminPageAsync(AdminProductFilterViewModel filter)
    {
        var cats = (await _categories.GetAllForAdminAsync())
            .Select(c => new CategoryOption { Id = c.CategoryID, Name = c.Name }).ToList();
        var sups = (await _suppliers.GetActiveAsync())
            .Select(s => new SupplierOption { Id = s.SupplierID, Name = s.CompanyName }).ToList();

        var query = _db.Products
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => !p.IsDiscontinued)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(p =>
                p.ProductName.Contains(s) ||
                p.SKU.Contains(s) ||
                (p.Description != null && p.Description.Contains(s)));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryID == filter.CategoryId.Value);

        if (filter.SupplierId.HasValue)
            query = query.Where(p => p.SupplierID == filter.SupplierId.Value);

        var items = await query.ToListAsync();
        IEnumerable<Product> ordered = filter.SortBy?.ToLowerInvariant() switch
        {
            "price" => filter.SortDesc ? items.OrderByDescending(p => p.UnitPrice) : items.OrderBy(p => p.UnitPrice),
            "stock" => filter.SortDesc
                ? items.OrderByDescending(p => p.Inventory?.QuantityInStock ?? 0)
                : items.OrderBy(p => p.Inventory?.QuantityInStock ?? 0),
            "id" => filter.SortDesc ? items.OrderByDescending(p => p.ProductID) : items.OrderBy(p => p.ProductID),
            "sku" => filter.SortDesc ? items.OrderByDescending(p => p.SKU) : items.OrderBy(p => p.SKU),
            _ => filter.SortDesc ? items.OrderByDescending(p => p.ProductName) : items.OrderBy(p => p.ProductName)
        };

        var products = ordered.Select(p => new AdminProductRowViewModel
        {
            ProductId = p.ProductID,
            ProductName = p.ProductName,
            Sku = p.SKU,
            UnitPrice = p.UnitPrice,
            QuantityInStock = p.Inventory?.QuantityInStock ?? 0,
            ImageBase64 = p.ImageBase64,
            CategoryName = p.Category?.Name ?? "",
            SupplierName = p.Supplier?.CompanyName ?? ""
        }).ToList();

        return new AdminProductsPageViewModel
        {
            Filter = filter,
            Products = products,
            Categories = cats,
            Suppliers = sups
        };
    }

    public async Task<List<AdminProductEditViewModel>> GetProductsAdminAsync()
    {
        var page = await GetProductsAdminPageAsync(new AdminProductFilterViewModel());
        return page.Products.Select(p => new AdminProductEditViewModel
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Sku = p.Sku,
            UnitPrice = p.UnitPrice,
            ImageBase64 = p.ImageBase64,
            QuantityInStock = p.QuantityInStock
        }).ToList();
    }

    public async Task<AdminProductEditViewModel?> GetProductForEditAsync(int? id)
    {
        var cats = (await _categories.GetAllActiveAsync()).Select(c => new CategoryOption { Id = c.CategoryID, Name = c.Name }).ToList();
        var sups = (await _suppliers.GetActiveAsync()).Select(s => new SupplierOption { Id = s.SupplierID, Name = s.CompanyName }).ToList();

        if (!id.HasValue)
            return new AdminProductEditViewModel { Categories = cats, Suppliers = sups };

        var p = await _products.GetByIdAsync(id.Value);
        if (p == null) return null;

        return new AdminProductEditViewModel
        {
            ProductId = p.ProductID,
            ProductName = p.ProductName,
            CategoryId = p.CategoryID,
            SupplierId = p.SupplierID,
            Description = p.Description,
            UnitPrice = p.UnitPrice,
            Sku = p.SKU,
            ImageBase64 = p.ImageBase64,
            IsDiscontinued = p.IsDiscontinued,
            QuantityInStock = p.Inventory?.QuantityInStock ?? 0,
            ReorderLevel = p.Inventory?.ReorderLevel ?? 5,
            Categories = cats,
            Suppliers = sups
        };
    }

    public async Task SaveProductAsync(AdminProductEditViewModel model)
    {
        if (model.ProductId.HasValue)
        {
            var p = await _products.GetByIdAsync(model.ProductId.Value);
            if (p == null) return;
            p.ProductName = model.ProductName;
            p.CategoryID = model.CategoryId;
            p.SupplierID = model.SupplierId;
            p.Description = model.Description;
            p.UnitPrice = model.UnitPrice;
            p.SKU = model.Sku;
            if (!string.IsNullOrEmpty(model.ImageBase64)) p.ImageBase64 = model.ImageBase64;
            p.IsDiscontinued = model.IsDiscontinued;
            p.UpdatedAt = DateTime.UtcNow;
            await _products.UpdateAsync(p);
            await _inventory.UpsertAsync(p.ProductID, model.QuantityInStock, model.ReorderLevel);
        }
        else
        {
            var p = new Product
            {
                ProductName = model.ProductName,
                CategoryID = model.CategoryId,
                SupplierID = model.SupplierId,
                Description = model.Description,
                UnitPrice = model.UnitPrice,
                SKU = model.Sku,
                ImageBase64 = model.ImageBase64,
                CreatedAt = DateTime.UtcNow
            };
            await _products.AddAsync(p);
            await _inventory.UpsertAsync(p.ProductID, model.QuantityInStock, model.ReorderLevel);
        }
    }

    public Task DeleteProductAsync(int id) => _products.DeleteAsync(id);

    public async Task<List<AdminUserViewModel>> GetUsersAsync() =>
        (await _users.GetAllAsync()).Select(u =>
        {
            var name = $"{u.FirstName} {u.LastName}".Trim();
            if (string.IsNullOrEmpty(name)) name = u.Username;
            return new AdminUserViewModel
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                DisplayName = name,
                ProfileImageBase64 = u.ProfileImageBase64,
                AuthorInitial = string.IsNullOrEmpty(name) ? "К" : name[..1].ToUpperInvariant(),
                Role = u.Role,
                IsActive = u.IsActive,
                CanWriteReviews = u.CanWriteReviews,
                RegistrationDate = u.RegistrationDate
            };
        }).ToList();

    public async Task SetUserActiveAsync(int userId, bool isActive)
    {
        var u = await _users.GetByIdAsync(userId);
        if (u == null) return;
        u.IsActive = isActive;
        await _users.UpdateAsync(u);
    }

    public async Task<List<OrderSummaryViewModel>> GetOrdersAdminAsync() =>
        (await _orders.GetAllAsync()).Select(o => new OrderSummaryViewModel
        {
            OrderId = o.OrderID,
            OrderDate = o.OrderDate,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            TotalAmount = o.TotalAmount,
            ItemCount = o.OrderDetails.Count
        }).ToList();

    public async Task UpdateOrderStatusAsync(int orderId, string status)
    {
        var o = await _orders.GetByIdAsync(orderId);
        if (o == null) return;
        o.Status = status;
        await _orders.UpdateAsync(o);
    }

    public async Task<List<AdminReviewViewModel>> GetReviewsAsync()
    {
        var list = await _reviews.GetAllForModerationAsync();
        return list.Select(r =>
        {
            var mod = _moderation.Analyze(r.Comment);
            var author = $"{r.User?.FirstName} {r.User?.LastName}".Trim();
            if (string.IsNullOrEmpty(author)) author = r.User?.Username ?? "Користувач";
            return new AdminReviewViewModel
            {
                ReviewId = r.ReviewID,
                UserId = r.UserID,
                ParentReviewId = r.ParentReviewID,
                ProductName = r.Product?.ProductName ?? "",
                Author = author,
                AuthorAvatar = r.User?.ProfileImageBase64,
                AuthorInitial = string.IsNullOrEmpty(author) ? "К" : author[..1].ToUpperInvariant(),
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                UserCanWriteReviews = r.User?.CanWriteReviews ?? true,
                ReviewDate = r.ReviewDate,
                RiskScore = mod.RiskScore,
                ModerationFlags = mod.Flags
            };
        }).OrderByDescending(r => !r.IsApproved).ThenByDescending(r => r.RiskScore).ToList();
    }

    public async Task ApproveReviewAsync(int reviewId, bool approved)
    {
        var r = await _reviews.GetByIdAsync(reviewId);
        if (r == null) return;
        r.IsApproved = approved;
        await _reviews.UpdateAsync(r);
    }

    public Task DeleteReviewAsync(int reviewId) => _reviews.DeleteAsync(reviewId);

    public async Task HideReviewAsync(int reviewId)
    {
        var r = await _reviews.GetByIdAsync(reviewId);
        if (r == null) return;
        r.IsApproved = false;
        await _reviews.UpdateAsync(r);
    }

    public async Task DeleteReviewAndRestrictUserAsync(int reviewId)
    {
        var r = await _reviews.GetByIdAsync(reviewId);
        if (r == null) return;
        var userId = r.UserID;
        await _reviews.DeleteAsync(reviewId);
        await SetUserCanWriteReviewsAsync(userId, false);
    }

    public async Task SetUserCanWriteReviewsAsync(int userId, bool canWrite)
    {
        var u = await _users.GetByIdAsync(userId);
        if (u == null) return;
        u.CanWriteReviews = canWrite;
        await _users.UpdateAsync(u);
    }

    public async Task<List<AdminPromotionViewModel>> GetPromotionsAsync() =>
        await _db.Promotions.Include(p => p.Supplier).OrderByDescending(p => p.StartDate)
            .Select(p => new AdminPromotionViewModel
            {
                PromotionId = p.PromotionID,
                PromotionName = p.PromotionName,
                SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "",
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive
            }).AsNoTracking().ToListAsync();

    public async Task<AdminPromotionEditViewModel?> GetPromotionForEditAsync(int? id)
    {
        var sups = (await _suppliers.GetActiveAsync())
            .Select(s => new SupplierOption { Id = s.SupplierID, Name = s.CompanyName }).ToList();
        var products = await _db.Products.Where(p => !p.IsDiscontinued)
            .Select(p => new ProductOption { Id = p.ProductID, Name = p.ProductName, Price = p.UnitPrice })
            .AsNoTracking().ToListAsync();

        var cats = (await _categories.GetAllActiveAsync())
            .Select(c => new CategoryOption { Id = c.CategoryID, Name = c.Name }).ToList();
        var seasonOpts = SeasonFilter.Options.ToList();

        if (!id.HasValue)
            return new AdminPromotionEditViewModel
            {
                Suppliers = sups,
                AvailableProducts = products,
                Categories = cats,
                SeasonOptions = seasonOpts
            };

        var promo = await _db.Promotions.Include(p => p.PromotionProducts).ThenInclude(pp => pp.Product)
            .FirstOrDefaultAsync(p => p.PromotionID == id.Value);
        if (promo == null) return null;

        var inPromo = promo.PromotionProducts.Select(pp => pp.ProductID).ToHashSet();

        return new AdminPromotionEditViewModel
        {
            PromotionId = promo.PromotionID,
            PromotionName = promo.PromotionName,
            Description = promo.Description,
            SupplierId = promo.SupplierID,
            StartDate = promo.StartDate,
            EndDate = promo.EndDate,
            IsActive = promo.IsActive,
            Suppliers = sups,
            AvailableProducts = products.Where(p => !inPromo.Contains(p.Id)).ToList(),
            Categories = cats,
            SeasonOptions = seasonOpts,
            ProductLines = promo.PromotionProducts.Select(pp => new AdminPromotionProductLine
            {
                PromotionProductId = pp.PromotionProductID,
                ProductId = pp.ProductID,
                ProductName = pp.Product?.ProductName ?? "",
                UnitPrice = pp.Product?.UnitPrice ?? 0,
                DiscountPercentage = pp.DiscountPercentage,
                IsActive = pp.IsActive
            }).ToList()
        };
    }

    public async Task SavePromotionAsync(AdminPromotionEditViewModel model)
    {
        Promotion promo;
        if (model.PromotionId.HasValue)
        {
            promo = await _db.Promotions.Include(p => p.PromotionProducts)
                .FirstAsync(p => p.PromotionID == model.PromotionId.Value);
            promo.PromotionName = model.PromotionName;
            promo.Description = model.Description;
            promo.SupplierID = model.SupplierId;
            promo.StartDate = model.StartDate;
            promo.EndDate = model.EndDate;
            promo.IsActive = model.IsActive;
        }
        else
        {
            promo = new Promotion
            {
                PromotionName = model.PromotionName,
                Description = model.Description,
                SupplierID = model.SupplierId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                IsActive = model.IsActive
            };
            _db.Promotions.Add(promo);
            await _db.SaveChangesAsync();
        }

        foreach (var line in model.ProductLines)
        {
            var existing = promo.PromotionProducts.FirstOrDefault(pp => pp.PromotionProductID == line.PromotionProductId);
            if (existing != null)
            {
                existing.DiscountPercentage = line.DiscountPercentage;
                existing.IsActive = line.IsActive;
            }
            else
            {
                _db.PromotionProducts.Add(new PromotionProduct
                {
                    PromotionID = promo.PromotionID,
                    ProductID = line.ProductId,
                    DiscountPercentage = line.DiscountPercentage,
                    IsActive = line.IsActive
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public Task EnablePromotionAsync(int promotionId) =>
        _db.Promotions.Where(p => p.PromotionID == promotionId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true));

    public Task DisablePromotionAsync(int promotionId) =>
        _db.Promotions.Where(p => p.PromotionID == promotionId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

    public async Task RemovePromotionProductAsync(int promotionProductId)
    {
        var link = await _db.PromotionProducts.FindAsync(promotionProductId);
        if (link == null) return;
        _db.PromotionProducts.Remove(link);
        await _db.SaveChangesAsync();
    }

    public async Task<int> AddProductsToPromotionByFilterAsync(
        int promotionId, int? categoryId, int? supplierId, string? season, decimal discountPercent)
    {
        var promo = await _db.Promotions.FindAsync(promotionId);
        if (promo == null) return 0;

        var query = _db.Products.Where(p => !p.IsDiscontinued);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryID == categoryId.Value);
        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierID == supplierId.Value);

        var products = await query.AsNoTracking().ToListAsync();
        if (!string.IsNullOrWhiteSpace(season) &&
            SeasonFilter.Keywords.TryGetValue(season, out var keywords))
        {
            products = products.Where(p =>
                keywords.Any(k =>
                    p.ProductName.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    (p.Description?.Contains(k, StringComparison.OrdinalIgnoreCase) ?? false))).ToList();
        }

        var existing = await _db.PromotionProducts
            .Where(pp => pp.PromotionID == promotionId)
            .Select(pp => pp.ProductID)
            .ToListAsync();
        var existingSet = existing.ToHashSet();

        var added = 0;
        foreach (var p in products.Where(p => !existingSet.Contains(p.ProductID)))
        {
            _db.PromotionProducts.Add(new PromotionProduct
            {
                PromotionID = promotionId,
                ProductID = p.ProductID,
                DiscountPercentage = discountPercent,
                IsActive = true
            });
            added++;
        }

        if (added > 0)
            await _db.SaveChangesAsync();
        return added;
    }

    public async Task<List<AdminCategoryViewModel>> GetCategoriesAdminAsync()
    {
        var list = await _db.Categories
            .Select(c => new AdminCategoryViewModel
            {
                CategoryId = c.CategoryID,
                Name = c.Name,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryID,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(p => !p.IsDiscontinued)
            })
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
        return list;
    }

    public async Task<AdminCategoryViewModel?> GetCategoryForEditAsync(int? id)
    {
        var parents = await _db.Categories
            .Where(c => c.IsActive && (!id.HasValue || c.CategoryID != id.Value))
            .OrderBy(c => c.Name)
            .Select(c => new CategoryOption { Id = c.CategoryID, Name = c.Name })
            .AsNoTracking()
            .ToListAsync();

        if (!id.HasValue)
            return new AdminCategoryViewModel { ParentCategories = parents };

        var c = await _categories.GetByIdAsync(id.Value);
        if (c == null) return null;

        return new AdminCategoryViewModel
        {
            CategoryId = c.CategoryID,
            Name = c.Name,
            Description = c.Description,
            ParentCategoryId = c.ParentCategoryID,
            IsActive = c.IsActive,
            ParentCategories = parents
        };
    }

    public async Task SaveCategoryAsync(AdminCategoryViewModel model)
    {
        if (model.CategoryId.HasValue)
        {
            var c = await _categories.GetByIdAsync(model.CategoryId.Value);
            if (c == null) return;
            c.Name = model.Name;
            c.Description = model.Description;
            c.ParentCategoryID = model.ParentCategoryId;
            c.IsActive = model.IsActive;
            await _categories.UpdateAsync(c);
        }
        else
        {
            await _categories.AddAsync(new Category
            {
                Name = model.Name,
                Description = model.Description,
                ParentCategoryID = model.ParentCategoryId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryID == id && !p.IsDiscontinued);
        if (hasProducts)
        {
            await SetCategoryActiveAsync(id, false);
            return;
        }

        var hasChildren = await _db.Categories.AnyAsync(c => c.ParentCategoryID == id);
        if (hasChildren)
        {
            await SetCategoryActiveAsync(id, false);
            return;
        }

        await _categories.DeleteAsync(id);
    }

    public async Task SetCategoryActiveAsync(int categoryId, bool isActive)
    {
        var c = await _categories.GetByIdAsync(categoryId);
        if (c == null) return;
        c.IsActive = isActive;
        await _categories.UpdateAsync(c);
    }

    public async Task ApplyQuickDiscountAsync(AdminQuickDiscountViewModel model)
    {
        var product = await _products.GetByIdAsync(model.ProductId);
        if (product == null) return;

        var promoName = $"Знижка: {product.ProductName}";
        var promo = await _db.Promotions.FirstOrDefaultAsync(p => p.PromotionName == promoName);
        if (promo == null)
        {
            promo = new Promotion
            {
                PromotionName = promoName,
                Description = "Індивідуальна знижка на товар",
                SupplierID = product.SupplierID,
                StartDate = DateTime.UtcNow,
                EndDate = model.EndDate.ToUniversalTime(),
                IsActive = true
            };
            _db.Promotions.Add(promo);
            await _db.SaveChangesAsync();
        }
        else
        {
            promo.EndDate = model.EndDate.ToUniversalTime();
            promo.IsActive = true;
        }

        var link = await _db.PromotionProducts.FirstOrDefaultAsync(pp =>
            pp.PromotionID == promo.PromotionID && pp.ProductID == model.ProductId);
        if (link == null)
        {
            _db.PromotionProducts.Add(new PromotionProduct
            {
                PromotionID = promo.PromotionID,
                ProductID = model.ProductId,
                DiscountPercentage = model.DiscountPercent,
                IsActive = true
            });
        }
        else
        {
            link.DiscountPercentage = model.DiscountPercent;
            link.IsActive = true;
        }

        await _db.SaveChangesAsync();
    }
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analytics;

    public AnalyticsService(IAnalyticsRepository analytics) => _analytics = analytics;

    public async Task<AnalyticsViewModel> GetAnalyticsAsync()
    {
        var sales = await _analytics.GetSalesByMonthAsync(6);
        var popular = await _analytics.GetPopularProductsAsync(10);
        var stats = await _analytics.GetCustomerStatsAsync();

        return new AnalyticsViewModel
        {
            SalesByMonth = sales.Select(s => new SalesPointViewModel { Label = s.Label, Total = s.Total }).ToList(),
            PopularProducts = popular.Select(p => new PopularProductViewModel
            {
                ProductName = p.Name,
                QuantitySold = p.Qty,
                Revenue = p.Revenue
            }).ToList(),
            CustomerStats = new CustomerStatsViewModel
            {
                TotalCustomers = stats.Total,
                NewCustomersLast30Days = stats.New30,
                AverageOrderValue = stats.AvgOrder
            }
        };
    }
}
