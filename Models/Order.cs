using System;
namespace Houdini.Models
{
    public class Order
    {
        public string OrderID { get; set; }
        public string Email { get; set; }
        public int Quantity { get; set; }
        public int ProductID { get; set; }
        public Product Product { get; set; }
    }
}
