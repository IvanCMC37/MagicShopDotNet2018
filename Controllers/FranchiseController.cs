using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Houdini.Models;
using Houdini.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Houdini.Controllers
{
    //making sure only store owner can access all store related stuff
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

        //method to use the drop down list
        private void PopulateProductDropDownList(object selectedProduct = null){

                var products = _context.Products.Select(x => x);
                ViewBag.ProductID = new SelectList(products.AsNoTracking(), "ProductID", "Name", selectedProduct);
 
        } 

        //create new stock request 
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
            
            var item = await _context.Products.SingleOrDefaultAsync(x => x.ProductID == stockRequest.ProductID);

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
    }
}
