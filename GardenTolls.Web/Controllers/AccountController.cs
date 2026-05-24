using System.Security.Claims;
using GardenTolls.Web.Infrastructure;
using GardenTolls.Web.Models;
using GardenTolls.Web.Services;
using GardenTolls.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenTolls.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login(string? returnUrl) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message, user) = await _auth.LoginAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        await SignInAsync(user!);
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.RegisterStep = ModelState.Keys.Any(k =>
                k is nameof(RegisterViewModel.FirstName) or nameof(RegisterViewModel.LastName) or nameof(RegisterViewModel.Phone))
                ? 2 : 1;
            return View(model);
        }
        var (success, message) = await _auth.RegisterAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            ViewBag.RegisterStep = 1;
            return View(model);
        }
        TempData["Success"] = message;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await _auth.RequestPasswordResetAsync(model, HttpContext.Session);
        TempData[success ? "Success" : "Error"] = message;
        if (success)
        {
            var token = HttpContext.Session.GetString($"{SessionKeys.PasswordResetEmail}:token");
            return RedirectToAction(nameof(ResetPassword), new { email = model.Email, token });
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token) =>
        View(new ResetPasswordViewModel { Email = email, Token = token });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await _auth.ResetPasswordAsync(model, HttpContext.Session);
        TempData[success ? "Success" : "Error"] = message;
        return success ? RedirectToAction(nameof(Login)) : View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var id = User.GetUserId();
        if (!id.HasValue) return RedirectToAction(nameof(Login));
        var model = await _auth.GetProfileAsync(id.Value);
        return model == null ? NotFound() : View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileEditViewModel model, IFormFile? profileImage)
    {
        if (!ModelState.IsValid) return View(model);
        var (success, message) = await _auth.UpdateProfileAsync(model, profileImage);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }
        TempData["Success"] = message;
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimsExtensions.UserIdClaim, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
