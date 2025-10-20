using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public class Location
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Regál")]
        public string Rack { get; set; }

        [Display(Name = "Šuplík")]
        public string? Drawer { get; set; }

        [Display(Name = "Krabička")]
        public string? Box { get; set; }

        // Navigační vlastnost – které součástky jsou zde uložené
        public ICollection<Component>? Components { get; set; }

        // Pomocná metoda – zobrazení ve formátu "A-2-B"
        public string DisplayName => $"{Rack}{(Drawer != null ? "-" + Drawer : "")}{(Box != null ? "-" + Box : "")}";
    }
}
