namespace InventoryApp.Services.Suppliers.Mouser.Models
{
    public class MouserSearchRequestRoot
    {
        public MouserPartSearchRequest SearchByPartRequest { get; set; }
        = new MouserPartSearchRequest();
    }
}
