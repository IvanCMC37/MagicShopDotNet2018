using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Houdini.Models
{
    public class OrderViewModel
    {
        public List<Order> Orders { get; set; }
        public List<Product> Products { get; set; }
    }
}
