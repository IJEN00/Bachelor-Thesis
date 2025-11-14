namespace InventoryApp.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!; 

        public string? WebsiteUrl { get; set; }

        public bool HasApi { get; set; }

        public string? ApiBaseUrl { get; set; }

        public string? ApiKey { get; set; } 

        public ICollection<SupplierOffer> Offers { get; set; } = new List<SupplierOffer>();
    }
}
