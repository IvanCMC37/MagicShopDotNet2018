using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Houdini.Models;
using Houdini.Data;
using Microsoft.AspNetCore.Http;
using Stripe;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Houdini.Controllers
{
    [Authorize(Roles = Constants.CustomerRole)]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
       
        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
          
        }

        // GET: Customer
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> StoreProducts(int id,
			string sortOrder,
            string currentFilter,
            string searchString,
            int? page)
        {
			ViewData["CurrentSort"] = sortOrder;
            ViewData["PriceSortParm"] = String.IsNullOrEmpty(sortOrder) ? "Price" : "";
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "Name" : "";
            ViewData["StockSortParm"] = String.IsNullOrEmpty(sortOrder) ? "Quantity" : "";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;

            var products = _context.StoreInventory.Include(o => o.Product).Select(o=>o).Where(o =>o.StoreID ==id);

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(s => s.Product.Name.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "Name":
                    products = products.OrderBy(s => s.Product.Name);
                    break;
                case "Quantity":
                    products = products.OrderBy(s => s.StockLevel);
                    break;
				case "Price":
                    products = products.OrderBy(s => s.Product.Price);
                    break;
                default:
                    products = products.OrderBy(s => s.Product.ProductID);
                    break;
            }
			int pagesize = 3;
            var store = await _context.Stores.SingleOrDefaultAsync(x => x.StoreID == id);
            ViewData["storeName"] = store.Name;
            return View(await PaginatedList<StoreInventory>
                .CreateAsync(products.AsNoTracking(), page ?? 1, pagesize));
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int storeID, int? quantity){
            if (quantity == null) {
                quantity = 1;
            }
               
            var cart = HttpContext.Session.GetCart();

            var storeInventory = await _context.StoreInventory.Include(x => x.Product).Include(x => x.Store).
                SingleAsync(x => x.ProductID == productID && x.StoreID == storeID);

            storeInventory.Store.StoreInventory.Clear();

            cart.AddItem(storeInventory, quantity);

            HttpContext.Session.SetCart(cart);

            return RedirectToAction("StoreProducts", new {id = storeID});
        }

        [HttpPost]
        public IActionResult RemoveFromCart( int itemIndex)
        {
            var cart = HttpContext.Session.GetCart();

            cart.RemoveItem(itemIndex);
        
            HttpContext.Session.SetCart(cart);
            return RedirectToAction("ShoppingCart");
        }

        public IActionResult ShoppingCart(){
            //type number min 1 for validation in quantity sizes
            return View(HttpContext.Session.GetCart());
        }

        public IActionResult RemoveAll(){

            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll();
            HttpContext.Session.SetCart(cart);

            return RedirectToAction("ShoppingCart");
        }

        public IActionResult Checkout(){
            
            var sessionCart = HttpContext.Session.GetCart();

            return View(sessionCart);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }

        public async Task<IActionResult> Charge(string stripeEmail, string stripeToken, int totalPrice, List<StoreInventory> itemList)
        {

            var customers = new StripeCustomerService();
            var charges = new StripeChargeService();

            var customer = customers.Create(new StripeCustomerCreateOptions
            {
                Email = stripeEmail,
                SourceToken = stripeToken
            });

            var charge = charges.Create(new StripeChargeCreateOptions
            {
                Amount = 500,
                Description = "Sample Charge",
                Currency = "usd",
                CustomerId = customer.Id
            });
            ViewData["totalPrice"] = totalPrice;

            var cart = HttpContext.Session.GetCart();

            foreach (var item in itemList)
            {
                var storeInventory = from si in _context.StoreInventory
                                     where si.ProductID == item.ProductID && si.StoreID == item.StoreID
                                     select si;
                foreach (StoreInventory si in storeInventory)
                {
                    si.StockLevel -= item.StockLevel;
                }
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            ////var order = new Order
            ////{
            ////    ProductID = itemList[0].ProductID,
            ////    Quantity = (int)itemList[0].StockLevel,
            ////    Email = stripeEmail,
            ////    OrderID = stripeToken
            ////};
            ////_context.Order.Add(order);
            ////_context.SaveChanges();

            ClearSession();
            await _context.SaveChangesAsync();

            return View(cart);

            //if (!itemList.Any())
            //{
            //    return RedirectToAction("ShoppingCart");
            //}
            //else { return RedirectToAction("index"); }
        }


        protected void ClearSession()
        {
            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll();
            HttpContext.Session.SetCart(cart);
        }
    }
}
