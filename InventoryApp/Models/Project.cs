using Microsoft.Build.Evaluation;
using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public enum ProjectStatus
    {
        [Display(Name = "Ve vývoji (Plánování)")]
        Planning,

        [Display(Name = "Materiál objednán")]
        Ordered,

        [Display(Name = "Materiál naskladněn / Čeká na výrobu")]
        ReadyToBuild,

        [Display(Name = "Ve výrobě (Osazování/Tisk)")]
        InProduction,

        [Display(Name = "Hotovo")]
        Completed
    }

    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        [Display(Name = "Popis (Markdown)")]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Workflow
        public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
        public DateTime? ConsumedAt { get; set; } 
        public DateTime? OrderedAt { get; set; }  
        public DateTime? ReceivedAt { get; set; } 

        // Time Tracking
        [Display(Name = "Odhad pracnosti (hod)")]
        public double EstimatedHours { get; set; }

        [Display(Name = "Skutečná pracnost (hod)")]
        public double RealHours { get; set; }

        // Přílohy 
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<ProjectItem> Items { get; set; } = new List<ProjectItem>();
    }
}
