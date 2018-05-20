using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Houdini.Models
{
    public class OwnerViewModel
    {
        public List<OwnerInventory> OwnerInventories { get; set; }
        public List<StockRequest> StockRequests { get; set; }
    }
}
