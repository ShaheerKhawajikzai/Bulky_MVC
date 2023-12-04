using Bulky.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Bulky.Models.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;


namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Roles = StaticData.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepo;
        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepo = categoryRepository;
        }
        public IActionResult Index()
        {
            List<Category> categoryList = _categoryRepo.GetAll()
                                          .OrderBy(c => c.DisplayOrder)
                                          .ToList();
            return View(categoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("DisplayOrder", "The Display Order cannot exactly match the Name.");
            }

            if (obj.DisplayOrder == null)
            {
                ModelState.AddModelError("DisplayOrder", "The Display Order field cannot be empty.");
            }

            if (!ModelState.IsValid)
                return View();

            _categoryRepo.Add(obj);
            _categoryRepo.Save();
            TempData["Success"] = "Category Created Successfully.";
            return RedirectToAction("Index", "Category");
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            Category? categoryFromDB = _categoryRepo.Get(u => u.Id == id);

            if (categoryFromDB == null)
                return NotFound();

            return View(categoryFromDB);
        }

        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (!ModelState.IsValid)
                return View(obj);

            _categoryRepo.Update(obj);
            _categoryRepo.Save();
            TempData["Success"] = "Category Updated Successfully.";

            return RedirectToAction("Index");

        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
                return NotFound();

            Category? categoryFromDB = _categoryRepo.Get(u => u.Id == id);

            if (categoryFromDB == null)
                return NotFound();

            return View(categoryFromDB);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePost(int? id)
        {
            Category? obj = _categoryRepo.Get(u => u.Id == id);
            if (obj == null)
                return NotFound();


            _categoryRepo.Remove(obj);
            _categoryRepo.Save();
            TempData["Success"] = "Category Deleted Successfully.";
            return RedirectToAction("Index");
        }
    }
}
