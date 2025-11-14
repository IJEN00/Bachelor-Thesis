namespace InventoryApp.Models
{
    public class SupplierOffer
    {
        public int Id { get; set; }

        public int ProjectItemId { get; set; }
        public ProjectItem ProjectItem { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public string? SupplierPartNumber { get; set; }

        public string Description { get; set; } = null!;

        public decimal UnitPrice { get; set; }

        public string Currency { get; set; } = "Kč";

        public bool InStock { get; set; }

        public int MinOrderQty { get; set; }

        public int? LeadTimeDays { get; set; }

        public string? ProductUrl { get; set; }

        public bool IsSelected { get; set; }
    }
}
