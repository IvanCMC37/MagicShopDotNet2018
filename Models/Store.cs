using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Houdini.Models
{
    public class Store
    {
        public int StoreID { get; set; }

        [StringLength(30, MinimumLength = 4)]
        [Display(Name = "Store Location")]
        public string Name { get; set; }

        public ICollection<StoreInventory> StoreInventory { get; } = new List<StoreInventory>();
    }
}
