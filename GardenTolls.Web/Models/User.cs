using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    public enum UserRole
    {
        [Display(Name = "Customer")]
        Customer,
        [Display(Name = "Admin")]
        Admin,
        [Display(Name = "Manager")]
        Manager
    }

    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public UserRole Role { get; set; } = UserRole.Customer;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? PostalCode { get; set; }

        public string? ProfileImageBase64 { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        public bool IsActive { get; set; } = true;

        public bool CanWriteReviews { get; set; } = true;

        // Навігаційна властивість для зв'язку з таблицею Customers
        public int? CustomerId { get; set; }
        
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }
    }
} 
