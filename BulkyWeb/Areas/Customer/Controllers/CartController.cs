using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Stripe.Checkout;
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
        private readonly IProductImageRepository _productImageRepo;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IShoppingCartRepository shoppingCartRepository,
            IApplicationUserRepository applicationUserRepository,
            IOrderHeaderRepository orderHeaderRepository,
            IOrderDetailRepository orderDetailRepository,
            IProductImageRepository productImageRepository)
        {
            _shoppingCartRepo = shoppingCartRepository;
            _applicationUserRepo = applicationUserRepository;
            _orderHeaderRepo = orderHeaderRepository;
            _orderDetailRepo = orderDetailRepository;
            _productImageRepo = productImageRepository;
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

            IEnumerable<ProductImage> productImages = _productImageRepo.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(x => x.ProductId == cart.ProductId).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
                ShoppingCartVM.ShoppingCartListCount++;
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
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.StatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;

            }
            else
            {
                //its a company user
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;

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

                var domain = "http://localhost:5260/";
                var option = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"customer/cart/OrderConformation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + "customer/cart/index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),  //20.50 =>2050
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title

                            }
                        },
                        Quantity = item.Count
                    };
                    option.LineItems.Add(sessionLineItem);
                }


                var service = new SessionService();
                Session session = service.Create(option);
                _orderHeaderRepo.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _orderHeaderRepo.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);

            }

            return RedirectToAction(nameof(OrderConformation), new { id = ShoppingCartVM.OrderHeader.Id });
        }


        public IActionResult OrderConformation(int id)
        {
            OrderHeader orderHeader = _orderHeaderRepo.Get(x => x.Id == id, includeProperties: "ApplicationUser");

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //this is an order by customer
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _orderHeaderRepo.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                    _orderHeaderRepo.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);

                    _orderHeaderRepo.Save();

                }
            }

            HttpContext.Session.Clear();
            List<ShoppingCart> shoppingCart = _shoppingCartRepo.GetAll(x =>
                                              x.ApplicationUserId == orderHeader.ApplicationUserId)
                                             .ToList();

            _shoppingCartRepo.RemoveRange(shoppingCart);
            _shoppingCartRepo.Save();

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
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId, IsTracked: true);

            if (cartFromDb.Count <= 1)
            {
                _shoppingCartRepo.Remove(cartFromDb);

                HttpContext.Session.SetInt32(SD.SessionCart,
                _shoppingCartRepo.GetAll(x => x.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
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
            var cartFromDb = _shoppingCartRepo.Get(u => u.Id == cartId, IsTracked: true);

            HttpContext.Session.SetInt32(SD.SessionCart,
            _shoppingCartRepo.GetAll(x => x.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);

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
