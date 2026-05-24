using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        public int InventoryID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        [Required]
        [StringLength(100)]
        public string WarehouseLocation { get; set; } = string.Empty;

        [Required]
        public int QuantityInStock { get; set; }

        [Required]
        public int ReorderLevel { get; set; }

        public DateTime? LastRestocked { get; set; }

        public DateTime? NextDeliveryDate { get; set; }

        [StringLength(20)]
        public string? InventoryStatus { get; set; }
    }
} 
