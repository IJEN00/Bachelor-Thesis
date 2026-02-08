using System.ComponentModel.DataAnnotations;

namespace InventoryApp.Models
{
    public enum ProjectItemType
    {
        [Display(Name = "Běžná součástka")]
        Standard,      

        [Display(Name = "Plošný spoj (PCB)")]
        PCB,          

        [Display(Name = "3D Tisk")]
        Print3D,      

        [Display(Name = "Jiné / Spotřební")]
        Other
    }

    public class ProjectItem
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public int? ComponentId { get; set; }
        public Component? Component { get; set; }

        public string? CustomName { get; set; }

        public ProjectItemType Type { get; set; } = ProjectItemType.Standard;

        public int QuantityRequired { get; set; }

        public int QuantityFromStock { get; set; }

        public int QuantityToBuy { get; set; }

        public bool IsFulfilled { get; set; } = false;

        public ICollection<SupplierOffer> SupplierOffers { get; set; } = new List<SupplierOffer>();
    }
}
