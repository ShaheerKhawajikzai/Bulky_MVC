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
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IProductImageRepository _productImageRepos;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IProductRepository productRepository,
            ICategoryRepository categoryRepo,
            IWebHostEnvironment webHostEnvironment, IProductImageRepository productImageRepository)
        {
            _productRepo = productRepository;
            _categoryRepo = categoryRepo;
            _webHostEnvironment = webHostEnvironment;
            _productImageRepos = productImageRepository;
        }
        public IActionResult Index()
        {

            return View();
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
                Product product = _productRepo.Get(p => p.Id == id, includeProperties: "ProductImages");
                productVM.Product = product;
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM obj, List<IFormFile> files)
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

            string wwwRootPath = _webHostEnvironment.WebRootPath;

            if (files != null)
            {
                foreach (var file in files)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = @"images\products\product-" + obj.Product.Id;
                    string finalPath = Path.Combine(wwwRootPath, productPath);

                    if (!Directory.Exists(finalPath))
                    {
                        Directory.CreateDirectory(finalPath);
                    }

                    using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    ProductImage productImage = new()
                    {
                        ImageUrl = @"\" + productPath + @"\" + fileName,
                        ProductId = obj.Product.Id,
                    };

                    if (obj.Product.ProductImages == null)
                    {
                        obj.Product.ProductImages = new List<ProductImage>();
                    }

                    obj.Product.ProductImages.Add(productImage);

                    _productImageRepos.Add(productImage);
                    _productImageRepos.Save();
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult DeleteImage(int imageId)
        {
            var productImageFromDb = _productImageRepos.Get(x => x.Id == imageId);
            if (productImageFromDb != null)
            {
                //delete the old image
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productImageFromDb.
                                                                                              ImageUrl.
                                                                                              TrimStart('\\'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }

                _productImageRepos.Remove(productImageFromDb);
                _productImageRepos.Save();
            }

            TempData["Success"] = "Image Deleted Successfully.";

            return RedirectToAction(nameof(Upsert), new { id = productImageFromDb.ProductId });
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

            string productPath = @"images\products\product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath.TrimStart('\\'));

            if (Directory.Exists(finalPath))
            {
                string[] file = Directory.GetFiles(finalPath);

                foreach (var item in file)
                {
                    System.IO.File.Delete(item);
                }
                Directory.Delete(finalPath);
            }


            _productRepo.Remove(productToBeDeleted);
            _productRepo.Save();

            return Json(new { success = true, message = "Product Deleted Successfully." });

            #endregion
        }
    }
}
