using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ComponentId { get; set; }
        public Component Component { get; set; } = null!;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = null!;

        [StringLength(200)]
        public string? FileName { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
