namespace InventoryApp.Models
{
    public class ProjectItem
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int? ComponentId { get; set; }
        public Component? Component { get; set; }

        public string? CustomName { get; set; }

        public int QuantityRequired { get; set; }

        public int QuantityFromStock { get; set; }

        public int QuantityToBuy { get; set; }

        public ICollection<SupplierOffer> SupplierOffers { get; set; } = new List<SupplierOffer>();
    }
}
