using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using Bulky.Models.ViewModels;
using Bulky.Utitlity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Runtime.Remoting;

namespace BulkyWeb.Areas.Admin.Controllers
{

    [Area("Admin")]
   // [Authorize(Roles = StaticData.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IProductRepository productRepository,
            ICategoryRepository categoryRepo,
            IWebHostEnvironment webHostEnvironment)
        {
            _productRepo = productRepository;
            _categoryRepo = categoryRepo;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> productList = _productRepo.GetAll(includeProperties:"Category").ToList();
            return View(productList);
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> categoryList = _categoryRepo.
                                                         GetAll().
                                                         Select(u => new SelectListItem
                                                         {
                                                             Text = u.Name,
                                                             Value = u.Id.ToString()
                                                         });

            ProductVM productVM = new ProductVM()
            {
                CategoryList = categoryList,
                Product = new Product()
            };

            if (id == null || id == 0)
            {
                //create 
                return View(productVM);
            }
            else
            {
                //update
                Product product = _productRepo.Get(p => p.Id == id);
                productVM.Product = product;
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (!ModelState.IsValid)
            {
                IEnumerable<SelectListItem> categoryList = _categoryRepo.
                                                         GetAll().
                                                         Select(u => new SelectListItem
                                                         {
                                                             Text = u.Name,
                                                             Value = u.Id.ToString()
                                                         });
                obj.CategoryList = categoryList;

                return View(obj);
            }
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            if (file != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string productPath = Path.Combine(wwwRootPath, @"images\product");

                if (!string.IsNullOrEmpty(obj.Product.ImageUrl))
                {
                    //delete the old image
                    var oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }

                }

                using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
                obj.Product.ImageUrl = @"\images\product\" + fileName;
            }

            if (obj.Product.Id == 0)
            {
                _productRepo.Add(obj.Product);
                TempData["Success"] = "Product Created Successfully.";
            }
            else
            {
                _productRepo.Update(obj.Product);
                TempData["Success"] = "Product Updated Successfully.";

            }

            _productRepo.Save();
            return RedirectToAction("Index");
        }


        #region API CALLS
        public IActionResult GetAll()
        {
            List<Product> productList = _productRepo.GetAll(includeProperties: "Category").ToList();
            return Json(new { data = productList });
        }

        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _productRepo.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            //delete the old image
            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.
                                                                             ImageUrl.TrimStart('\\'));

            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _productRepo.Remove(productToBeDeleted);
            _productRepo.Save();

            return Json(new { success = true, message = "Product Deleted Successfully." });
            //}
            #endregion
        }
    }
}
