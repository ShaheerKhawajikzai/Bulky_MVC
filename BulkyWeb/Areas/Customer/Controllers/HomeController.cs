using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Utitlity;
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
            IEnumerable<Product> productList = _productRepo.GetAll(includeProperties: "Category,ProductImages");
            return View(productList);
        }
        public IActionResult Details(int Id)
        {
            ShoppingCart cart = new ShoppingCart
            {
                Product = _productRepo.Get(u => u.Id == Id, includeProperties: "Category,ProductImages"),
                Count = 1,
                ProductId = Id
            };

            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details([FromForm] ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = _shoppingCartRepo.Get(u => u.ApplicationUserId == userId &&
            u.ProductId == shoppingCart.ProductId);


            if (cartFromDb != null)
            {
                //shopping cart exsists
                cartFromDb.Count += shoppingCart.Count;
                _shoppingCartRepo.Update(cartFromDb);
                _shoppingCartRepo.Save();
            }

            else
            {
                //add 
                _shoppingCartRepo.Add(shoppingCart);
                _shoppingCartRepo.Save();
                HttpContext.Session.SetInt32(SD.SessionCart,
                _shoppingCartRepo.GetAll(x => x.ApplicationUserId == userId).Count());

            }
            TempData["Success"] = "Cart Updated Successfully.";
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
