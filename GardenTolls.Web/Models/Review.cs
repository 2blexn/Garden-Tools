using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int ReviewID { get; set; }

        [Required]
        public int ProductID { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }

        [Range(1, 5)]
        public byte? Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

        public bool IsVerifiedPurchase { get; set; } = false;

        public bool IsApproved { get; set; } = false;

        public int? ParentReviewID { get; set; }

        [ForeignKey("ParentReviewID")]
        public Review? ParentReview { get; set; }

        public ICollection<Review> Replies { get; set; } = new List<Review>();
    }
} 
