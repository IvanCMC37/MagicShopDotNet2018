using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Houdini.Models;
using Houdini.Data;
using System;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Net.Http;
using System.Collections.Generic;

namespace Houdini.Controllers
{
    //making sure only owner can access owner related stuffs
    [Authorize(Roles = Constants.OwnerRole)]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OwnerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Owner
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.OwnerInventory.Include(o => o.Product);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: OwnerInventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ownerInventory = await _context.OwnerInventory.SingleOrDefaultAsync(m => m.ProductID == id);
            if (ownerInventory == null)
            {
                return NotFound();
            }
            var product = await _context.Products.SingleOrDefaultAsync(x => x.ProductID == id);

            ViewData["ProductName"] = product.Name;
            return View(ownerInventory);
        }

        // POST: OwnerInventories/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductID,StockLevel")] OwnerInventory ownerInventory)
        {
            if (id != ownerInventory.ProductID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ownerInventory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OwnerInventoryExists(ownerInventory.ProductID))
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
            ViewData["ProductID"] = new SelectList(_context.Products, "ProductID", "Name", ownerInventory.ProductID);
            return View(ownerInventory);
        }

        private bool OwnerInventoryExists(int id)
        {
            return _context.OwnerInventory.Any(e => e.ProductID == id);
        }

        // GET: StockRequest
        public async Task<IActionResult> StockRequest()
        {
            var applicationDbContext = _context.OwnerInventory.Include(o => o.Product);
            var applicationDbContext2 = _context.StockRequests.Include(s => s.Product).Include(s => s.Store);
            return View(new OwnerViewModel
            {
                OwnerInventories = await applicationDbContext.ToListAsync(),
                StockRequests = await applicationDbContext2.ToListAsync()
            });
        }

        //for processing the stock request 
        public async Task<IActionResult> Process(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockRequest = await _context.StockRequests
                .Include(s => s.Product)
                .Include(s => s.Store)
                .SingleOrDefaultAsync(m => m.StockRequestID == id);
            if (stockRequest == null)
            {
                return NotFound();
            }

            return View(stockRequest);
        }

        //using api to check all orders
        public async Task<IActionResult> OrderCheck()
        {
            using (var client = new HttpClient())
            {
                //call the api site to fetch data
                var result = await client.GetStringAsync("http://localhost:64495/api/orders");

                //make them into list via deserialize json
                var orders = JsonConvert.DeserializeObject<List<Order>>(result);

                //get product in oder to translate the product id to name
                var products = _context.Products.Select(x => x);

                //pass to viewModel
                return View(new OrderViewModel
                {
                    Orders = orders,
                    Products = await products.ToListAsync()
                });
            }
        }

        //the logic of processing
        [HttpPost, ActionName("Process")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessConfirmed(int id)
        {
            var stockRequest = await _context.StockRequests.SingleOrDefaultAsync(m => m.StockRequestID == id);
            var query = from si in _context.StoreInventory
                        where si.StoreID == stockRequest.StoreID && si.ProductID == stockRequest.ProductID
                        select si;
            var query_2 = from oi in _context.OwnerInventory
                          where oi.ProductID == stockRequest.ProductID
                          select oi;

            //need to check if the store already had the item, if not preform insert instead of updating
            if(!query.Any())
            {
                var storeInventory = new StoreInventory
                {
                    StoreID = stockRequest.StoreID,
                    ProductID = stockRequest.ProductID,
                    StockLevel = stockRequest.Quantity
                };
                _context.StoreInventory.Add(storeInventory);
                _context.SaveChanges();
            }
            else{
                foreach (StoreInventory si in query){
                    si.StockLevel += stockRequest.Quantity;
                }
            }

            //after store update, reduce owner stock
            foreach (OwnerInventory oi in query_2)
            {
                oi.StockLevel -= stockRequest.Quantity;
            }
            // Submit the changes to the database.
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            _context.StockRequests.Remove(stockRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StockRequest));
        }
    }
}
