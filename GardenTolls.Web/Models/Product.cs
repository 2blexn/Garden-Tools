using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    [Table("Products")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }

        [Required]
        public int SupplierID { get; set; }

        [ForeignKey("SupplierID")]
        public Supplier? Supplier { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal? Weight { get; set; }

        [StringLength(50)]
        public string? Dimensions { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        public string? ImageBase64 { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDiscontinued { get; set; } = false;

        // Навігаційні властивості
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        
        public Inventory? Inventory { get; set; }
    }
} 
