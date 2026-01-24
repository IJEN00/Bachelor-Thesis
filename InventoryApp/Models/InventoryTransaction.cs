using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApp.Models
{
    public enum InventoryTransactionType
    {
        Add,     
        Use,        
        Adjust      
    }

    public class InventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        public int ComponentId { get; set; }

        [ForeignKey(nameof(ComponentId))]
        public Component Component { get; set; } = null!;

        public int DeltaQuantity { get; set; }

        public InventoryTransactionType Type { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Note { get; set; }
    }
}
