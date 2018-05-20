using System.ComponentModel.DataAnnotations;

namespace Houdini.Models
{
    public class Product
    {
        [Display(Name = "Product ID")]
        public int ProductID { get; set; }

        [StringLength(60, MinimumLength = 2)]
        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Required]
        [Range(0.01, 10000)]
        [DataType(DataType.Currency)]
        public int Price { get; set; }

    }
}
