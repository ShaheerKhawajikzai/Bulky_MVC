using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderDetailRepository _orderDetailRepo;
        private readonly IOrderHeaderRepository _orderHeaderRepo;

        [BindProperty]
        public OrderVM orderVM { get; set; }

        public OrderController(IOrderDetailRepository orderDetailRepository, IOrderHeaderRepository orderHeaderRepository)
        {
            _orderDetailRepo = orderDetailRepository;
            _orderHeaderRepo = orderHeaderRepository;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
            OrderVM orderVM = new()
            {
                OrderHeader = _orderHeaderRepo.Get(x => x.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _orderDetailRepo.GetAll(x => x.OrderHeaderId == orderId, includeProperties: "Product")
            };

            return View(orderVM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderHeaderFromDb = _orderHeaderRepo.Get(x => x.Id == orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderVM.OrderHeader.City;
            orderHeaderFromDb.State = orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderVM.OrderHeader.PostalCode;

            if (!String.IsNullOrEmpty(orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = orderVM.OrderHeader.Carrier;
            }
            if (!String.IsNullOrEmpty(orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            }

            TempData["Success"] = "Order Details Updated Successfully.";

            _orderHeaderRepo.Update(orderHeaderFromDb);
            _orderHeaderRepo.Save();

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _orderHeaderRepo.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _orderHeaderRepo.Save();

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderVM.OrderHeader.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _orderHeaderRepo.Get(x => x.Id == orderVM.OrderHeader.Id);

            orderHeader.TrackingNumber = orderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = orderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _orderHeaderRepo.Update(orderHeader);
            _orderHeaderRepo.Save();

            TempData["Success"] = "Order Shipped Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
        }

        [HttpDelete]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(int orderHeaderId)
        {
            var orderHeader = _orderHeaderRepo.Get(x => x.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId,
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _orderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefund);

            }
            else
            {
                _orderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _orderHeaderRepo.Save();

            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_Pay_Now()
        {
            orderVM.OrderHeader = _orderHeaderRepo.Get(x => x.Id == orderVM.OrderHeader.Id,includeProperties:"ApplicationUser");
            orderVM.OrderDetail = _orderDetailRepo.GetAll(x => x.OrderHeaderId == orderVM.OrderHeader.Id,includeProperties:"Product");

            var domain = "http://localhost:5260/";
            var option = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={orderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderVM.OrderDetail)
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
            _orderHeaderRepo.UpdateStripePaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _orderHeaderRepo.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _orderHeaderRepo.Get(x => x.Id == orderHeaderId);

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is an order by company

                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _orderHeaderRepo.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _orderHeaderRepo.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);

                    _orderHeaderRepo.Save();

                }
            }

            //List<ShoppingCart> shoppingCart = _shoppingCartRepo.GetAll(x =>
            //                                  x.ApplicationUserId == orderHeader.ApplicationUserId)
            //                                 .ToList();

            //_shoppingCartRepo.RemoveRange(shoppingCart);
            //_shoppingCartRepo.Save();

            return View(orderHeaderId);
        }
        #region API CALLS
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeader;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeader = _orderHeaderRepo.
                    GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orderHeader = _orderHeaderRepo.GetAll(x => x.ApplicationUserId == userId,
                    includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "PaymentPending":
                    orderHeader = orderHeader.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;

                case "InProcess":

                    orderHeader = orderHeader.Where(x => x.OrderStatus == SD.StatusInProcess);
                    break;

                case "Completed":
                    orderHeader = orderHeader.Where(x => x.OrderStatus == SD.StatusShipped);
                    break;

                case "Approved":
                    orderHeader = orderHeader.Where(x => x.OrderStatus == SD.StatusApproved);
                    break;

                default:
                    break;
            }


            return Json(new { data = orderHeader });
        }

        #endregion
    }
}
