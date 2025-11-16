namespace InventoryApp.Services.Suppliers.Mouser.Models
{
    public class MouserSearchResults
    {
        public int NumberOfResults { get; set; }
        public List<MouserPart> Parts { get; set; } = new();
    }
}
