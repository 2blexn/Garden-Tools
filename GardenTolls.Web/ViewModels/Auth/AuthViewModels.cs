using System.ComponentModel.DataAnnotations;

namespace GardenTolls.Web.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Неправильний формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обов'язковий")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ім'я користувача обов'язкове")]
    [StringLength(50)]
    [Display(Name = "Ім'я користувача")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обов'язковий")]
    [EmailAddress(ErrorMessage = "Неправильний формат email")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Пароль обов'язковий")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Мінімум 6 символів")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердження пароля обов'язкове")]
    [Compare(nameof(Password), ErrorMessage = "Паролі не збігаються")]
    [DataType(DataType.Password)]
    [Display(Name = "Підтвердження пароля")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ім'я обов'язкове")]
    [StringLength(50)]
    [Display(Name = "Ім'я")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Прізвище обов'язкове")]
    [StringLength(50)]
    [Display(Name = "Прізвище")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефон обов'язковий")]
    [Phone(ErrorMessage = "Невірний формат телефону")]
    [Display(Name = "Телефон")]
    public string Phone { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileEditViewModel
{
    public int UserId { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? ProfileImageBase64 { get; set; }
    public bool RemoveProfileImage { get; set; }

    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword))]
    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }
}
