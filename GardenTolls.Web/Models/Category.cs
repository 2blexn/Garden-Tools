using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        [Column("CategoryName")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryID { get; set; }
        
        [ForeignKey("ParentCategoryID")]
        public Category? ParentCategory { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Навігаційні властивості
        public ICollection<Product> Products { get; set; } = new List<Product>();
        
        public ICollection<Category> Subcategories { get; set; } = new List<Category>();
    }
} 
