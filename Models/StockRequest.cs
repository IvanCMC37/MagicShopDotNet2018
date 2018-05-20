using System;
using System.ComponentModel.DataAnnotations;

namespace Houdini.Models
{
    public class StockRequest
    {
        [Display(Name = "Request ID")]
        public int StockRequestID { get; set; }

        [Display(Name = "Store ID")]
        public int? StoreID { get; set; }
        public Store Store { get; set; }

        [Display(Name = "Product ID")]
        public int ProductID { get; set; }
        public Product Product { get; set; }

        [Required]
        [Range(1, 200)]
        public int Quantity { get; set; }
    }
}
