using Bulky.DataAccess.Repository.IRepository;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        public IShoppingCartRepository _shoppingCartRepo { get; set; }
        public ShoppingCartViewComponent(IShoppingCartRepository shoppingCartRepository)
        {
            _shoppingCartRepo = shoppingCartRepository;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) == null)
                {
                    HttpContext.Session.SetInt32(SD.SessionCart,
                   _shoppingCartRepo.GetAll(x => x.ApplicationUserId == claim.Value).Count());
                }

                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }



    }
}
