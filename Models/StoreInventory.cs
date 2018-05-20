
using System.ComponentModel.DataAnnotations;

namespace Houdini.Models
{
    public class StoreInventory
    {
        public int? StoreID { get; set; }
        public Store Store { get; set; }

        [Display(Name = "Product ID")]
        public int ProductID { get; set; }
        public Product Product { get; set; }

        [Required]
        [Display(Name = "Stock Level")]
        public int? StockLevel { get; set; }
    }
}
