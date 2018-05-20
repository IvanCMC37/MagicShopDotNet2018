using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Houdini.Models;
using Houdini.Data;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Identity;

using System.Diagnostics;

namespace Houdini.Controllers
{
    [Authorize(Roles = Constants.FranchiseRole)]
    public class FranchiseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
      
        public FranchiseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager )
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Franchise
        [HttpGet]
        public async Task<IActionResult> Index()
        {  
            var user = await _userManager.GetUserAsync(User);
            var query = _context.StoreInventory.Include(x => x.Product).Include(x => x.Store).Select(x => x);
            query = query.Where(x => x.StoreID == user.StoreID);
           
            return View(await query.ToListAsync()); 
        }

        // GET: Franchise/Details/5
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

        // GET: Franchise/Create
        public IActionResult Create()
        {
            PopulateProductDropDownList();
            return View();
        }


        // GET: Franchise/Create
        public IActionResult CreateNewItem()
        {
            PopulateProductDropDownList();
            return View();
        }

        private void PopulateProductDropDownList(object selectedProduct = null){

            //var products = from p in _context.Products
            //join si in _context.StoreInventory
            //on p.ProductID equals si.ProductID
            //join s in _context.Stores
            //on si.StoreID equals s.StoreID
            //where s.StoreID == user.StoreID
            //select p;

                var products = _context.Products.Select(x => x);
                ViewBag.ProductID = new SelectList(products.AsNoTracking(), "ProductID", "Name", selectedProduct);
 
        } 

        public async Task<IActionResult> StockRequest()
        {
            var user = await _userManager.GetUserAsync(User);
            var query = _context.StockRequests.Include(x => x.Product)
                                .Select(x => x).Where(x => x.StoreID == user.StoreID);

            return View(await query.ToListAsync());
        }

        // POST: Franchise/Cancel/5
        [HttpPost, ActionName("DeleteStockRequest")]    
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var stockRequest = await _context.StockRequests.SingleOrDefaultAsync(m => m.StockRequestID == id);
            _context.StockRequests.Remove(stockRequest);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(StockRequest));
        }


        // GET: Franchise/Delete/5
        public async Task<IActionResult> DeleteStockRequest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stockRequest = await _context.StockRequests
                .SingleOrDefaultAsync(m => m.StockRequestID == id);
            
            var item = await _context.Products.SingleOrDefaultAsync(x => x.ProductID == id);

            ViewData["itemName"] = item.Name;
            if (stockRequest == null)
            {
                return NotFound();
            }

            return View(stockRequest);
        }

        // POST: Franchise/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductID, Quantity")] StockRequest stockRequest)
        {
            var user = await _userManager.GetUserAsync(User);
            stockRequest.StoreID = user.StoreID;

            if (ModelState.IsValid)
            {
                _context.Add(stockRequest);
               
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PopulateProductDropDownList(stockRequest.ProductID);
            return View();
        }


        // GET: Franchise/Edit/5
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

        // POST: Franchise/Edit/5
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

        // GET: Franchise/Delete/5
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

        // POST: Franchise/Delete/5
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
    }
}
