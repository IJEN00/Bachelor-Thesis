namespace InventoryApp.Services.Suppliers.Mouser.Models
{
    public class MouserPart
    {
        public string Availability { get; set; } = string.Empty;
        public string AvailabilityInStock { get; set; } = "0";
        public string FactoryStock { get; set; } = "0";
        public string Manufacturer { get; set; } = string.Empty;
        public string ManufacturerPartNumber { get; set; } = string.Empty;
        public string MouserPartNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DataSheetUrl { get; set; } = string.Empty;
        public string ProductDetailUrl { get; set; } = string.Empty;
        public string Min { get; set; } = "1";   
        public string Mult { get; set; } = "1";
        public List<MouserPriceBreak> PriceBreaks { get; set; } = new();
    }
}
