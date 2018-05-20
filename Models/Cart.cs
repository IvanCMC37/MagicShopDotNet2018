using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Houdini.Models
{
    public class Cart
    {
        public int CartID { get; set; }
        public Guid GuID { get; set; }
        public const string CartSessionKey = "cart";
        public List<StoreInventory> Items { get; set; } = new List<StoreInventory>();



        public void AddItem(StoreInventory item, int? quantity )
        {

            item.StockLevel = quantity;
            var index = Items.FindIndex(x => x.ProductID == item.ProductID && x.StoreID == item.StoreID);
            if (index == -1)
                Items.Add(item);
            else
                Items[index].StockLevel += quantity;
        }

        //not working
        public void RemoveItem(int index)
        {

            Items.RemoveAt(index);
        }

        //not working
        public void RemoveAll()
        {

            Items.Clear();
        }
    }

    public static class CartExtensions
    {
        public static Cart GetCart(this ISession session)
        {
            return session.Get<Cart>(Cart.CartSessionKey) ?? new Cart();
        }

        public static void SetCart(this ISession session, Cart cart)
        {
            session.Set(Cart.CartSessionKey, cart);
        }
    }

}
