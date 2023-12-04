using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepo;
        private readonly IShoppingCartRepository _shoppingCartRepo;
        public HomeController(ILogger<HomeController> logger, IProductRepository productRepository, IShoppingCartRepository shoppingCartRepo)
        {
            _logger = logger;
            _productRepo = productRepository;
            _shoppingCartRepo = shoppingCartRepo;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _productRepo.GetAll(includeProperties: "Category");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart cart = new ShoppingCart
            {
                Product = _productRepo.Get(u => u.Id == id, includeProperties: "Category"),
                Count = 1,
                ProductId = id
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;


          //  shoppingCart.Id = 0;


            ShoppingCart cartFromDb = _shoppingCartRepo.Get(u => u.ApplicationUserId == userId &&
            u.ProductId == shoppingCart.ProductId);


            if (cartFromDb != null)
            {
                cartFromDb.Count += shoppingCart.Count;
                _shoppingCartRepo.Update(cartFromDb);
            }

            else
            {
                _shoppingCartRepo.Add(shoppingCart);
            }
            TempData["Success"] = "Cart Updated Successfully.";
            _shoppingCartRepo.Save();
            return RedirectToAction(nameof(Index));
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
