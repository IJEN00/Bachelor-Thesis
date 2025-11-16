namespace InventoryApp.Services.Suppliers.Mouser.Models
{
    public class MouserPart
    {
        public string Availability { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ManufacturerPartNumber { get; set; } = string.Empty;
        public string MouserPartNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataSheetUrl { get; set; } = string.Empty;
        public List<MouserPriceBreak> PriceBreaks { get; set; } = new();
    }
}
