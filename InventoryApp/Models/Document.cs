using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public DateTime UploadedAt { get; set; }

        public int? ComponentId { get; set; }
        public Component? Component { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}
