using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GardenTolls.Web.Models;

[Table("SearchQueryLogs")]
public class SearchQueryLog
{
    [Key]
    public int SearchQueryLogId { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [StringLength(200)]
    public string Query { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
