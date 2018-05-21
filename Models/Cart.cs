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

        //method of adding a item to cart
        public void AddItem(StoreInventory item, int? quantity )
        {
            item.StockLevel = quantity;
            var index = Items.FindIndex(x => x.ProductID == item.ProductID && x.StoreID == item.StoreID);
            if (index == -1)
                Items.Add(item);
            else
                Items[index].StockLevel += quantity;
        }

        //method fo removing a item from cart
        public void RemoveItem(int index)
        {
            Items.RemoveAt(index);
        }

        //remove all item from cart
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
