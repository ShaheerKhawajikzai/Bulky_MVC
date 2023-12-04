using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Build.Experimental.ProjectCache;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IShoppingCartRepository _shoppingCartRepo;
        private readonly IApplicationUserRepository _applicationUserRepo;
        private readonly IOrderHeaderRepository _orderHeaderRepo;
        private readonly IOrderDetailRepository _orderDetailRepo;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IShoppingCartRepository shoppingCartRepository,
            IApplicationUserRepository applicationUserRepository,
            IOrderHeaderRepository orderHeaderRepository,
            IOrderDetailRepository orderDetailRepository)
        {
            _shoppingCartRepo = shoppingCartRepository;
            _applicationUserRepo = applicationUserRepository;
            _orderHeaderRepo = orderHeaderRepository;
            _orderDetailRepo = orderDetailRepository;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new ShoppingCartVM()
            {
                ShoppingCartList = _shoppingCartRepo.
                GetAll(o => o.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _shoppingCartRepo.GetAll(u =>
                u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _applicationUserRepo.Get(u => u.Id == userId);


            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }


        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM.ShoppingCartList = _shoppingCartRepo.GetAll(u =>
            u.ApplicationUserId == userId, includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

             ApplicationUser applicationUser = _applicationUserRepo.Get(u => u.Id == userId);

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //it is a regular customer account 
                ShoppingCartVM.OrderHeader.PaymentStatus = StaticData.StatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = StaticData.StatusPending;

            }
            else
            {
                //its a company user
                ShoppingCartVM.OrderHeader.PaymentStatus = StaticData.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = StaticData.StatusPending;

            }
            _orderHeaderRepo.Add(ShoppingCartVM.OrderHeader);
            _orderHeaderRepo.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };

                _orderDetailRepo.Add(orderDetail);
                _orderDetailRepo.Save();
            }
            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //its a regular customer and we will have to make payment 
                //Stripe payment
            }



            return RedirectToAction(nameof(OrderConformation), new {id= ShoppingCartVM.OrderHeader.Id });
        }


        public IActionResult OrderConformation(int id)
        {

            return View(id);
        }















        public IActionResult Plus(int? cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);
            cartFromDb.Count += 1;

            _shoppingCartRepo.Update(cartFromDb);
            _shoppingCartRepo.Save();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);

            if (cartFromDb.Count <= 1)
            {
                _shoppingCartRepo.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count -= 1;
                _shoppingCartRepo.Update(cartFromDb);
            }

            _shoppingCartRepo.Save();
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int? cartId)
        {
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId);
            _shoppingCartRepo.Remove(cartFromDb);
            _shoppingCartRepo.Save();
            return RedirectToAction(nameof(Index));

        }

        public double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
                return shoppingCart.Product.Price;
            else
            {
                if (shoppingCart.Count <= 100)
                    return shoppingCart.Product.Price50;
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }
        }
    }
}
