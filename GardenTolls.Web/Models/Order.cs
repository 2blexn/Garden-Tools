using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public Customer? Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        [Column(TypeName = "decimal(12, 2)")]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        [StringLength(200)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCity { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ShippingCountry { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ShippingPostalCode { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Навігаційні властивості
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
} 
