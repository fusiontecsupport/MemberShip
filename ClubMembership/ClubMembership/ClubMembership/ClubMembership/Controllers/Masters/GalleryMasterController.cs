using ClubMembership.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClubMembership.Data;

namespace ClubMembership.Controllers.Masters
{
    [SessionExpire]
    public class GalleryMasterController : Controller
    {
        ApplicationDbContext context = new ApplicationDbContext();
        ClubMembershipDBEntities db = new ClubMembershipDBEntities();

        public class CategoryTypeRow
        {
            public int CateTid { get; set; }
            public string CateTDesc { get; set; }
        }

        private void PopulateCategoryTypes()
        {
            try
            {
                var sql = @"SELECT CateTid, CateTDesc 
                           FROM [CategoryTypeMaster] 
                           WHERE Dispstatus = 0 
                           ORDER BY CateTDesc";
                var categoryTypes = db.Database.SqlQuery<CategoryTypeRow>(sql).ToList();
                ViewBag.CategoryTypes = categoryTypes;
            }
            catch
            {
                ViewBag.CategoryTypes = new List<CategoryTypeRow>();
            }
        }

        [Authorize(Roles = "GalleryMasterIndex")]
        public ActionResult Index()
        {
            return View(context.gallerymasters.ToList());
        }

        [Authorize(Roles = "GalleryMasterCreate,GalleryMasterEdit")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]) == 0) 
            { 
                return RedirectToAction("Login", "Account"); 
            }

            GalleryMaster tab = new GalleryMaster();
          
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            selectedStatus.Add(new SelectListItem { Text = "Inactive", Value = "1", Selected = false });
            selectedStatus.Add(new SelectListItem { Text = "Active", Value = "0", Selected = true });
            ViewBag.Status = new SelectList(selectedStatus, "Value", "Text");

            tab.GalleryId = 0;
            tab.Status = 0;
            tab.CreatedDate = DateTime.Now;

            // Populate CategoryTypes for checkbox list
            PopulateCategoryTypes();

            if (id != 0)
            {
                tab = context.gallerymasters.Find(id);
                if (tab != null)
                {
                    List<SelectListItem> selectedStatus1 = new List<SelectListItem>();
                    selectedStatus1.Add(new SelectListItem { Text = "Inactive", Value = "1", Selected = tab.Status == 1 });
                    selectedStatus1.Add(new SelectListItem { Text = "Active", Value = "0", Selected = tab.Status == 0 });
                    ViewBag.Status = new SelectList(selectedStatus1, "Value", "Text", tab.Status.ToString());
                }
            }

            return View(tab);
        }

        [HttpPost]
        [Authorize(Roles = "GalleryMasterCreate,GalleryMasterEdit")]
        public ActionResult Form(GalleryMaster model, HttpPostedFileBase mainImage, HttpPostedFileBase[] additionalImages, bool removeMainImage = false, string[] deleteImages = null, int[] selectedCategoryIds = null)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle category selection
                    if (selectedCategoryIds != null && selectedCategoryIds.Any())
                    {
                        model.CategoryTypeIds = string.Join(",", selectedCategoryIds);
                    }
                    else
                    {
                        model.CategoryTypeIds = null;
                    }

                    // Handle main image upload
                    if (mainImage != null && mainImage.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(mainImage.FileName);
                        string uploadPath = Server.MapPath("~/Uploads/Gallery/");
                        
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }
                        
                        mainImage.SaveAs(Path.Combine(uploadPath, fileName));
                        // Store with ~ prefix for consistent path resolution
                        model.MainImage = "~/Uploads/Gallery/" + fileName;
                    }

                    // Handle additional images upload
                    List<string> additionalImagePaths = new List<string>();
                    if (additionalImages != null)
                    {
                        foreach (var image in additionalImages)
                        {
                            if (image != null && image.ContentLength > 0)
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                                string uploadPath = Server.MapPath("~/Uploads/Gallery/");
                                
                                if (!Directory.Exists(uploadPath))
                                {
                                    Directory.CreateDirectory(uploadPath);
                                }
                                
                                image.SaveAs(Path.Combine(uploadPath, fileName));
                                // Store with ~ prefix for consistent path resolution
                                additionalImagePaths.Add("~/Uploads/Gallery/" + fileName);
                            }
                        }
                    }
                    model.AdditionalImages = string.Join(",", additionalImagePaths);

                    if (model.GalleryId == 0)
                    {
                        // Create new
                        model.CreatedBy = User.Identity.Name;
                        model.CreatedDate = DateTime.Now;
                        model.CompanyId = Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]);
                        context.gallerymasters.Add(model);
                    }
                    else
                    {
                        // Update existing
                        var existing = context.gallerymasters.Find(model.GalleryId);
                        if (existing != null)
                        {
                            existing.Heading = model.Heading;
                            existing.Caption = model.Caption;
                            existing.Description = model.Description;
                            existing.CategoryTypeIds = model.CategoryTypeIds;
                            existing.Category = model.Category;
                            existing.Status = model.Status;
                            existing.ModifiedBy = User.Identity.Name;
                            existing.ModifiedDate = DateTime.Now;

                            // Main image: replace if new uploaded; remove if requested
                            if (!string.IsNullOrEmpty(model.MainImage))
                            {
                                // Delete previous main image file if being replaced
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(existing.MainImage))
                                    {
                                        var old = Server.MapPath(existing.MainImage);
                                        if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
                                    }
                                }
                                catch { }
                                existing.MainImage = model.MainImage;
                            }
                            else if (removeMainImage)
                            {
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(existing.MainImage))
                                    {
                                        var full = Server.MapPath(existing.MainImage);
                                        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                                    }
                                }
                                catch { }
                                existing.MainImage = null;
                            }

                            // Additional images: start from existing
                            var currentImages = new List<string>();
                            if (!string.IsNullOrEmpty(existing.AdditionalImages))
                            {
                                currentImages = existing.AdditionalImages.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
                            }

                            // Remove selected images
                            if (deleteImages != null && deleteImages.Length > 0)
                            {
                                var toRemove = new HashSet<string>(deleteImages);
                                foreach (var p in currentImages.Where(p => toRemove.Contains(p)).ToList())
                                {
                                    try
                                    {
                                        var full = Server.MapPath(p);
                                        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                                    }
                                    catch { }
                                }
                                currentImages = currentImages.Where(p => !toRemove.Contains(p)).ToList();
                            }

                            // Append newly uploaded images (already in model.AdditionalImages)
                            if (!string.IsNullOrEmpty(model.AdditionalImages))
                            {
                                var newOnes = model.AdditionalImages.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x));
                                currentImages.AddRange(newOnes);
                            }

                            existing.AdditionalImages = string.Join(",", currentImages);
                        }
                    }

                    context.SaveChanges();
                    TempData["SuccessMessage"] = "Gallery item saved successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving gallery item: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            selectedStatus.Add(new SelectListItem { Text = "Inactive", Value = "1", Selected = model.Status == 1 });
            selectedStatus.Add(new SelectListItem { Text = "Active", Value = "0", Selected = model.Status == 0 });
            ViewBag.Status = new SelectList(selectedStatus, "Value", "Text", model.Status.ToString());

            return View(model);
        }

        [Authorize(Roles = "GalleryMasterEdit")]
        public ActionResult Edit(int id)
        {
            return RedirectToAction("Form", new { id = id });
        }

        [Authorize(Roles = "GalleryMasterDelete")]
        public ActionResult Delete(int id)
        {
            try
            {
                var galleryItem = context.gallerymasters.Find(id);
                if (galleryItem != null)
                {
                    // Try deleting physical files for main and additional images
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(galleryItem.MainImage))
                        {
                            var mainPath = Server.MapPath(galleryItem.MainImage);
                            if (System.IO.File.Exists(mainPath)) System.IO.File.Delete(mainPath);
                        }
                        if (!string.IsNullOrWhiteSpace(galleryItem.AdditionalImages))
                        {
                            foreach (var p in galleryItem.AdditionalImages.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                            {
                                try
                                {
                                    var full = Server.MapPath(p);
                                    if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                                }
                                catch { }
                            }
                        }
                    }
                    catch { }
                    context.gallerymasters.Remove(galleryItem);
                    context.SaveChanges();
                    TempData["SuccessMessage"] = "Gallery item deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting gallery item: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "GalleryMasterIndex")]
        [Route("GalleryMaster/Details/{id:int}")]
        public ActionResult Details(int id)
        {
            var item = context.gallerymasters.Find(id);
            if (item == null) return HttpNotFound();
            if (Request != null && Request.IsAjaxRequest())
            {
                return PartialView("Details", item);
            }
            return View("Details", item);
        }

        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            var data = context.gallerymasters
                .Where(x => x.CompanyId == Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]))
                .OrderByDescending(x => x.CreatedDate)
                .Skip(param.iDisplayStart)
                .Take(param.iDisplayLength)
                .Select(d => new { 
                    d.GalleryId, 
                    d.Heading, 
                    d.Caption, 
                    d.MainImage,
                    Category = d.Category ?? "",
                    Status = d.Status == 0 ? "Active" : "Inactive",
                    d.CreatedDate 
                })
                .ToArray();

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}

