using Microsoft.Build.Evaluation;

namespace InventoryApp.Models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectItem> Items { get; set; } = new List<ProjectItem>();
    }
}
