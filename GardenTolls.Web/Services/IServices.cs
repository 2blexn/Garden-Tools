using GardenTolls.Web.Models;
using GardenTolls.Web.ViewModels.Admin;
using GardenTolls.Web.ViewModels.Auth;
using GardenTolls.Web.ViewModels.Cart;
using GardenTolls.Web.ViewModels.Orders;
using GardenTolls.Web.ViewModels.Products;
using GardenTolls.Web.ViewModels.Promotions;

namespace GardenTolls.Web.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model);
    Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model);
    Task<(bool Success, string Message)> RequestPasswordResetAsync(ForgotPasswordViewModel model, ISession session);
    Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordViewModel model, ISession session);
    Task<ProfileEditViewModel?> GetProfileAsync(int userId);
    Task<(bool Success, string Message)> UpdateProfileAsync(ProfileEditViewModel model, IFormFile? profileImage = null);
}

public interface ICatalogService
{
    Task<ProductListViewModel> GetCatalogAsync(ProductFilterViewModel filter);
    Task<ProductDetailsViewModel?> GetProductDetailsAsync(int id, int? fromPromotionId = null);
    Task<string> AddReviewAsync(int productId, int userId, ReviewInputViewModel input);
    Task<PromotionCatalogViewModel?> GetPromotionCatalogAsync(int promotionId, ProductFilterViewModel filter);
}

public interface ICartService
{
    CartViewModel GetCart(ISession session);
    Task<bool> AddToCartAsync(ISession session, int productId, int quantity = 1);
    Task UpdateQuantityAsync(ISession session, int productId, int quantity);
    Task RemoveFromCartAsync(ISession session, int productId);
    void ClearCart(ISession session);
    HashSet<int> GetCartProductIds(ISession session);
    List<int> GetWishlist(ISession session);
    Task<WishlistViewModel> GetWishlistPageAsync(ISession session);
    int GetWishlistCount(ISession session);
    void ToggleWishlist(ISession session, int productId);
}

public interface IOrderService
{
    Task<CheckoutViewModel?> BuildCheckoutAsync(ISession session, int userId);
    Task<(bool Success, string Message, int? OrderId)> PlaceOrderAsync(CheckoutViewModel model, ISession session, int userId);
    Task<(bool Success, string Message)> SimulatePaymentAsync(PaymentViewModel model);
    Task<OrderHistoryViewModel> GetHistoryAsync(int userId);
    Task<OrderDetailsViewModel?> GetOrderDetailsAsync(int orderId, int userId, bool isAdmin);
}

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync();
    Task<AdminProductsPageViewModel> GetProductsAdminPageAsync(AdminProductFilterViewModel filter);
    Task<List<AdminProductEditViewModel>> GetProductsAdminAsync();
    Task<AdminProductEditViewModel?> GetProductForEditAsync(int? id);
    Task SaveProductAsync(AdminProductEditViewModel model);
    Task DeleteProductAsync(int id);
    Task<List<AdminUserViewModel>> GetUsersAsync();
    Task SetUserActiveAsync(int userId, bool isActive);
    Task<List<OrderSummaryViewModel>> GetOrdersAdminAsync();
    Task UpdateOrderStatusAsync(int orderId, string status);
    Task<List<AdminReviewViewModel>> GetReviewsAsync();
    Task ApproveReviewAsync(int reviewId, bool approved);
    Task HideReviewAsync(int reviewId);
    Task DeleteReviewAsync(int reviewId);
    Task DeleteReviewAndRestrictUserAsync(int reviewId);
    Task SetUserCanWriteReviewsAsync(int userId, bool canWrite);
    Task<List<AdminPromotionViewModel>> GetPromotionsAsync();
    Task<AdminPromotionEditViewModel?> GetPromotionForEditAsync(int? id);
    Task SavePromotionAsync(AdminPromotionEditViewModel model);
    Task EnablePromotionAsync(int promotionId);
    Task DisablePromotionAsync(int promotionId);
    Task RemovePromotionProductAsync(int promotionProductId);
    Task<int> AddProductsToPromotionByFilterAsync(int promotionId, int? categoryId, int? supplierId, string? season, decimal discountPercent);
    Task ApplyQuickDiscountAsync(AdminQuickDiscountViewModel model);
    Task<List<AdminCategoryViewModel>> GetCategoriesAdminAsync();
    Task<AdminCategoryViewModel?> GetCategoryForEditAsync(int? id);
    Task SaveCategoryAsync(AdminCategoryViewModel model);
    Task DeleteCategoryAsync(int id);
    Task SetCategoryActiveAsync(int categoryId, bool isActive);
}

public interface IAnalyticsService
{
    Task<AnalyticsViewModel> GetAnalyticsAsync();
}
