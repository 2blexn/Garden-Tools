using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models;

[Table("Promotions")]
public class Promotion
{
    [Key]
    public int PromotionID { get; set; }

    public int SupplierID { get; set; }

    [ForeignKey(nameof(SupplierID))]
    public Supplier? Supplier { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(200)]
    public string PromotionName { get; set; } = string.Empty;

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}

[Table("PromotionProducts")]
public class PromotionProduct
{
    [Key]
    public int PromotionProductID { get; set; }

    public int PromotionID { get; set; }

    [ForeignKey(nameof(PromotionID))]
    public Promotion? Promotion { get; set; }

    public int ProductID { get; set; }

    [ForeignKey(nameof(ProductID))]
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal DiscountPercentage { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public decimal? PromotionalPrice =>
        Product != null ? Product.UnitPrice * (1 - DiscountPercentage / 100) : null;
}
