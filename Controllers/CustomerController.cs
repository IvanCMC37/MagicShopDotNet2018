using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Houdini.Models;
using Houdini.Data;
using Microsoft.AspNetCore.Http;
using Stripe;

namespace Houdini.Controllers
{
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
            return View(await PaginatedList<StoreInventory>
                .CreateAsync(products.AsNoTracking(), page ?? 1, pagesize));
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productID, int storeID, int quantity){
            //if (!ModelState.IsValid)
            //{
            //    return View();
            //}

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
            //var storeInventory = await _context.StoreInventory.Include(x => x.Product).Include(x => x.Store).
                //SingleAsync(x => x.ProductID == productID && x.StoreID == storeID);

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
            //order order = new order { items = sessioncart.items };


            return View(sessionCart);
        }

        // GET: Customer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .SingleOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Customer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customer/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID,Name,Price")] Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Customer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.SingleOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Customer/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,Name,Price")] Product product)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.ProductID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Customer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .SingleOrDefaultAsync(m => m.ProductID == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.SingleOrDefaultAsync(m => m.ProductID == id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }

        public IActionResult Charge(string stripeEmail, string stripeToken, int totalPrice)
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

            ClearSession();
            return View(cart);
        }

        protected void ClearSession()
        {
            var cart = HttpContext.Session.GetCart();
            cart.RemoveAll();
            HttpContext.Session.SetCart(cart);
        }
    }
}
