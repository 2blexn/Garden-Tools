using System.Security.Claims;
using GardenTolls.Web.Models;

namespace GardenTolls.Web.Infrastructure;

public static class ClaimsExtensions
{
    public const string UserIdClaim = "UserId";

    public static int? GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(UserIdClaim) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid) ? uid : null;
    }

    public static bool IsAdminOrManager(this ClaimsPrincipal user) =>
        user.IsInRole(UserRole.Admin.ToString()) || user.IsInRole(UserRole.Manager.ToString());
}
