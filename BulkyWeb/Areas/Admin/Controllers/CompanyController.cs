using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly ICompanyRepository _companyRepo;

        public CompanyController(ICompanyRepository companyRepository)
        {
            _companyRepo = companyRepository;
        }
        public IActionResult Index()
        {
            List<Company> listOfCompanyObjs = _companyRepo.GetAll().ToList();

            return View(listOfCompanyObjs);
        }
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                //Create
                return View(new Company());

            }
            else
            {
                //update
                Company obj = _companyRepo.Get(c => c.Id == id);
                return View(obj);

            }
        }

        [HttpPost]
        public IActionResult Upsert(Company obj)
        {
            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            if (obj.Id == 0)
            {
                _companyRepo.Add(obj);
                TempData["Success"] = "Company Added Successfully.";
            }
            else
            {
                _companyRepo.Update(obj);
                TempData["Success"] = "Company Updated Successfully.";
            }
            _companyRepo.Save();

            return RedirectToAction("Index");
        }


        #region API Calls

        public IActionResult GetAll()
        {
            List<Company> listofCompanyObjs = _companyRepo.GetAll().ToList();

            return Json(new { data = listofCompanyObjs });
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return Json(new { success = false, message = "Error while Deleting" });
            }

            Company obj = _companyRepo.Get(c => c.Id == id);

            if (obj == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            _companyRepo.Remove(obj);
            _companyRepo.Save();

            return Json(new { success = true, message = "Deleted Successfully." });

        }

        #endregion
    }
}
