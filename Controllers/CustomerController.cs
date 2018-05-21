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
    //make sure only CustomerRole can use customer controller related stuffs
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

        //load store inventory with PaginatedList , referencing from lecture
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

        //add item from store to cart, preparing for check out later on
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

        //user decided to remove specific item from cart
        [HttpPost]
        public IActionResult RemoveFromCart( int itemIndex)
        {
            var cart = HttpContext.Session.GetCart();

            cart.RemoveItem(itemIndex);
        
            HttpContext.Session.SetCart(cart);
            return RedirectToAction("ShoppingCart");
        }

        //show cart  info
        public IActionResult ShoppingCart(){
            return View(HttpContext.Session.GetCart());
        }

        //user decided to remove all item from cart
        public IActionResult RemoveAll(){

            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll();
            HttpContext.Session.SetCart(cart);

            return RedirectToAction("ShoppingCart");
        }

        //user decided to check out with current cart item(s)
        public IActionResult Checkout(){
            
            var sessionCart = HttpContext.Session.GetCart();
            return View(sessionCart);
        }

        //check if product exist int the database
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }

        //the method to charge customer with the amount he is going to pay, as well as database update
        public async Task<IActionResult> Charge(string stripeEmail, string stripeToken, int totalPrice, List<StoreInventory> itemList)
        {
            var customers = new StripeCustomerService();
            var charges = new StripeChargeService();

            //setup to use stripe for CC checkout
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

            //save data for the view to use later on
            ViewData["totalPrice"] = totalPrice;
            String orderID = Guid.NewGuid().ToString();
            ViewData["OrderID"] = orderID;

            //get cart info from session
            var cart = HttpContext.Session.GetCart();

            //database update (storeInventory and add details to order table)
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

                //make sure similar item is being added up
                var orderCheck = from odr in _context.Order
                                 where odr.ProductID == item.ProductID && odr.OrderID == orderID
                                 select odr;
                if (orderCheck.Any())
                {
                    foreach (Order odr in orderCheck)
                    {
                        odr.Quantity += (int)item.StockLevel;
                    }
                }
                else
                {
                    var order = new Order
                    {
                        ProductID = item.ProductID,
                        Quantity = (int)item.StockLevel,
                        Email = stripeEmail,
                        OrderID = orderID
                    };
                    _context.Order.Add(order);
                    _context.SaveChanges();
                }
            }

            ClearSession();
            await _context.SaveChangesAsync();
            return View(cart);
        }

        //clear the cart session
        protected void ClearSession()
        {
            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll();
            HttpContext.Session.SetCart(cart);
        }
    }
}
