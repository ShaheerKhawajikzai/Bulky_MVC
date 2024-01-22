using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IApplicationUserRepository _applicationUserReop;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICompanyRepository _companyRepo;
        public UserController(IApplicationUserRepository applicationUser, UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager, ICompanyRepository companyRepository)
        {
            _applicationUserReop = applicationUser;
            _userManager = userManager;
            _roleManager = roleManager;
            _companyRepo = companyRepository;

        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string userId)
        {

            //if (data.Company == null)
            //{
            //    data.Company = new Company() { Name = " " };
            //}

            RoleManagementVM roleManagementVM = new()
            {
                ApplicationUser = _applicationUserReop.Get(u => u.Id == userId, includeProperties: "Company"),
                listOfRoles = _roleManager.Roles.Select(r => new SelectListItem()
                {
                    Text = r.Name,
                    Value = r.Name
                }),
                listOfRegisteredCompanies = _companyRepo.GetAll().Select(c => new SelectListItem()
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                })
            };

            roleManagementVM.ApplicationUser.Role = _userManager.GetRolesAsync(roleManagementVM.ApplicationUser).
                                                     GetAwaiter().GetResult().FirstOrDefault();

            //just checking how GetRolesAsync Works,if you want to remove, remove it.
            var asd = _userManager.GetRolesAsync(_applicationUserReop.Get(x => x.Id == userId)).GetAwaiter().GetResult();

            return View(roleManagementVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
        {
            //My Logic agar change karna ho to course mai sai daikh lain. BTW WORKING FINE.

            var userFromDb = _applicationUserReop.Get(x => x.Id == roleManagementVM.ApplicationUser.Id);
            var oldRole = _userManager.GetRolesAsync(userFromDb).GetAwaiter().GetResult().FirstOrDefault();

            if (roleManagementVM.ApplicationUser.Role == SD.Role_Company)
            {
                userFromDb.CompanyId = Convert.ToInt32(roleManagementVM.ApplicationUser.CompanyId);
                _applicationUserReop.Update(userFromDb);
                _applicationUserReop.Save();
                _userManager.RemoveFromRoleAsync(userFromDb, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, SD.Role_Company).GetAwaiter().GetResult();
            }

            else
            {
                userFromDb.CompanyId = null;
                _applicationUserReop.Update(userFromDb);
                _applicationUserReop.Save();
                _userManager.RemoveFromRoleAsync(userFromDb, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, roleManagementVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            

            TempData["Success"] = "Operation Performed Successfully.";

            return RedirectToAction(nameof(Index));
        }


        #region API CALLS
        public IActionResult GetAll()
        {
            IEnumerable<ApplicationUser> applicationUsers = _applicationUserReop.GetAll(includeProperties: "Company");


            foreach (var user in applicationUsers)
            {
                if (user.Company == null)
                {
                    user.Company = new Company() { Name = "null" };
                }
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();

            }

            return Json(new { data = applicationUsers });
        }


        [HttpPost]
        public IActionResult LockUnlock(string id)
        {
            var user = _applicationUserReop.Get(x => x.Id == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them.
                user.LockoutEnd = DateTime.Now;
            }
            else
            {
                user.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _applicationUserReop.Update(user);
            _applicationUserReop.Save();

            return Json(new { success = true, message = "Operation Performed Successfully." });
        }

        #endregion

    }


}
