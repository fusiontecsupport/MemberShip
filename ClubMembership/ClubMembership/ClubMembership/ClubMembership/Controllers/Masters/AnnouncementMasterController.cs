using ClubMembership.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClubMembership.Data;
using System.Data.SqlClient;

namespace ClubMembership.Controllers.Masters
{
    [SessionExpire]
    public class AnnouncementMasterController : Controller
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
            catch (Exception ex)
            {
                // Log error if needed
                ViewBag.CategoryTypes = new List<CategoryTypeRow>();
            }
        }

        [Authorize(Roles = "AnnouncementMasterIndex")]
        public ActionResult Index()
        {
            var announcements = context.announcementmasters.ToList();
            
            // Populate CategoryTypeDescriptions for each announcement
            foreach (var announcement in announcements)
            {
                if (!string.IsNullOrEmpty(announcement.CategoryTypeIds))
                {
                    try
                    {
                        var categoryIds = announcement.CategoryTypeIdList;
                        if (categoryIds.Any())
                        {
                            var placeholders = string.Join(",", categoryIds.Select((_, index) => $"@catId{index}"));
                            var sql = $@"SELECT CateTDesc FROM [CategoryTypeMaster] WHERE CateTid IN ({placeholders})";
                            
                            var parameters = categoryIds.Select((catId, index) => new SqlParameter($"@catId{index}", catId)).ToArray();
                            var categoryDescs = db.Database.SqlQuery<string>(sql, parameters).ToList();
                            
                            announcement.CategoryTypeDescriptions = categoryDescs;
                        }
                    }
                    catch
                    {
                        announcement.CategoryTypeDescriptions = new List<string> { "Error loading categories" };
                    }
                }
                else
                {
                    announcement.CategoryTypeDescriptions = new List<string>();
                }
            }
            
            return View(announcements);
        }

        [Authorize(Roles = "AnnouncementMasterCreate,AnnouncementMasterEdit")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]) == 0) 
            { 
                return RedirectToAction("Login", "Account"); 
            }

            AnnouncementMaster tab = new AnnouncementMaster();
            
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            selectedStatus.Add(new SelectListItem { Text = "Inactive", Value = "1", Selected = false });
            selectedStatus.Add(new SelectListItem { Text = "Active", Value = "0", Selected = true });
            ViewBag.Status = new SelectList(selectedStatus, "Value", "Text");

            // Populate CategoryTypes for checkbox list
            PopulateCategoryTypes();

            tab.AnnouncementId = 0;
            tab.Status = 0;
            tab.CreatedDate = DateTime.Now;

            if (id != 0)
            {
                tab = context.announcementmasters.Find(id);
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
        [Authorize(Roles = "AnnouncementMasterCreate,AnnouncementMasterEdit")]
        public ActionResult Form(AnnouncementMaster model, HttpPostedFileBase mainImage, bool removeMainImage = false, int[] selectedCategoryIds = null)
        {
            // Server-side validation for image file types (only JPG, JPEG, PNG allowed)
            if (mainImage != null && mainImage.ContentLength > 0)
            {
                var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { 
                    ".jpg", ".jpeg", ".png"
                };
                var ext = Path.GetExtension(mainImage.FileName) ?? string.Empty;
                var contentType = mainImage.ContentType ?? string.Empty;

                // Some browsers may send content types like image/pjpeg; extension check is primary
                var extensionAllowed = allowedExtensions.Contains(ext);
                var contentTypeAllowed = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                    (contentType.IndexOf("jpeg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     contentType.IndexOf("png", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!extensionAllowed || !contentTypeAllowed)
                {
                    ModelState.AddModelError("", "Only JPG, JPEG, PNG files are allowed for Main Image.");
                }
            }

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
                        string uploadPath = Server.MapPath("~/Uploads/Announcements/");
                        
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }
                        
                        mainImage.SaveAs(Path.Combine(uploadPath, fileName));
                        model.MainImage = "/Uploads/Announcements/" + fileName;
                    }

                    // Additional images are not supported for announcements. Ensure cleared.
                    model.AdditionalImages = null;

                    if (model.AnnouncementId == 0)
                    {
                        // Create new
                        model.CreatedBy = User.Identity.Name;
                        model.CreatedDate = DateTime.Now;
                        model.CompanyId = Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]);
                        context.announcementmasters.Add(model);
                    }
                    else
                    {
                        // Update existing
                        var existing = context.announcementmasters.Find(model.AnnouncementId);
                        if (existing != null)
                        {
                            existing.Heading = model.Heading;
                            existing.Caption = model.Caption;
                            existing.Description = model.Description;
                            existing.CategoryTypeIds = model.CategoryTypeIds;
                            existing.Status = model.Status;
                            existing.ModifiedBy = User.Identity.Name;
                            existing.ModifiedDate = DateTime.Now;

                            // Main image: replace if new uploaded; remove if requested
                            if (!string.IsNullOrEmpty(model.MainImage))
                            {
                                existing.MainImage = model.MainImage;
                            }
                            else if (removeMainImage)
                            {
                                // Try to delete the physical file if it exists, then clear the field
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(existing.MainImage))
                                    {
                                        var full = Server.MapPath(existing.MainImage);
                                        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                                    }
                                }
                                catch { /* swallow file IO issues but still clear field */ }
                                existing.MainImage = null;
                            }

                            // Additional images are not used for announcements
                            existing.AdditionalImages = null;
                        }
                    }

                    context.SaveChanges();
                    TempData["SuccessMessage"] = "Announcement saved successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving announcement: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            selectedStatus.Add(new SelectListItem { Text = "Inactive", Value = "1", Selected = model.Status == 1 });
            selectedStatus.Add(new SelectListItem { Text = "Active", Value = "0", Selected = model.Status == 0 });
            ViewBag.Status = new SelectList(selectedStatus, "Value", "Text", model.Status.ToString());

            // Repopulate CategoryTypes for checkbox list
            PopulateCategoryTypes();

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "AnnouncementMasterIndex")]
        [Route("AnnouncementMaster/Details/{id:int}")]
        public ActionResult Details(int id)
        {
            var item = context.announcementmasters.Find(id);
            if (item == null) return HttpNotFound();
            
            // Populate CategoryTypeDescriptions
            if (!string.IsNullOrEmpty(item.CategoryTypeIds))
            {
                try
                {
                    var categoryIds = item.CategoryTypeIdList;
                    if (categoryIds.Any())
                    {
                        var placeholders = string.Join(",", categoryIds.Select((_, index) => $"@catId{index}"));
                        var sql = $@"SELECT CateTDesc FROM [CategoryTypeMaster] WHERE CateTid IN ({placeholders})";
                        
                        var parameters = categoryIds.Select((catId, index) => new SqlParameter($"@catId{index}", catId)).ToArray();
                        var categoryDescs = db.Database.SqlQuery<string>(sql, parameters).ToList();
                        
                        item.CategoryTypeDescriptions = categoryDescs;
                    }
                }
                catch
                {
                    item.CategoryTypeDescriptions = new List<string> { "Error loading categories" };
                }
            }
            else
            {
                item.CategoryTypeDescriptions = new List<string>();
            }
            
            if (Request != null && Request.IsAjaxRequest())
            {
                return PartialView("Details", item);
            }
            return View("Details", item);
        }

        [Authorize(Roles = "AnnouncementMasterEdit")]
        public ActionResult Edit(int id)
        {
            return RedirectToAction("Form", new { id = id });
        }

        [Authorize(Roles = "AnnouncementMasterDelete")]
        public ActionResult Delete(int id)
        {
            try
            {
                var announcement = context.announcementmasters.Find(id);
                if (announcement != null)
                {
                    // Attempt to delete physical files for main and additional images
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(announcement.MainImage))
                        {
                            var mainPath = Server.MapPath(announcement.MainImage);
                            if (System.IO.File.Exists(mainPath)) System.IO.File.Delete(mainPath);
                        }
                        if (!string.IsNullOrWhiteSpace(announcement.AdditionalImages))
                        {
                            foreach (var p in announcement.AdditionalImages.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
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
                    context.announcementmasters.Remove(announcement);
                    context.SaveChanges();
                    TempData["SuccessMessage"] = "Announcement deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting announcement: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            var data = context.announcementmasters
                .Where(x => x.CompanyId == Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]))
                .OrderByDescending(x => x.CreatedDate)
                .Skip(param.iDisplayStart)
                .Take(param.iDisplayLength)
                .Select(d => new { 
                    d.AnnouncementId, 
                    d.Heading, 
                    d.Caption, 
                    d.MainImage,
                    Status = d.Status == 0 ? "Active" : "Inactive",
                    d.CreatedDate 
                })
                .ToArray();

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}
