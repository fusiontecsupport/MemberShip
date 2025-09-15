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
    public class EventMasterController : Controller
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

        [Authorize(Roles = "EventMasterIndex,AnnouncementMasterIndex")]
        public ActionResult Index()
        {
            return View(context.eventmasters.ToList());
        }

        [Authorize(Roles = "EventMasterCreate,EventMasterEdit,AnnouncementMasterCreate,AnnouncementMasterEdit")]
        public ActionResult Form(int? id = 0)
        {
            if (Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]) == 0) 
            { 
                return RedirectToAction("Login", "Account"); 
            }

            EventMaster tab = new EventMaster();
            
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "Inactive", Value = "1", Selected = false };
            selectedStatus.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "Active", Value = "0", Selected = true };
            selectedStatus.Add(selectedItem);
            ViewBag.Status = selectedStatus;

            tab.EventId = 0;
            tab.Status = 0;
            tab.CreatedDate = DateTime.Now;
            tab.EventTime = DateTime.Now;

            // Populate CategoryTypes for checkbox list
            PopulateCategoryTypes();

            if (id != 0)
            {
                tab = context.eventmasters.Find(id);
                List<SelectListItem> selectedStatus1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.Status) == 1)
                {
                    SelectListItem selectedItemDSP = new SelectListItem { Text = "Inactive", Value = "1", Selected = true };
                    selectedStatus1.Add(selectedItemDSP);
                    selectedItemDSP = new SelectListItem { Text = "Active", Value = "0", Selected = false };
                    selectedStatus1.Add(selectedItemDSP);
                    ViewBag.Status = selectedStatus1;
                }
            }

            return View(tab);
        }

        [HttpPost]
        [Authorize(Roles = "EventMasterCreate,EventMasterEdit,AnnouncementMasterCreate,AnnouncementMasterEdit")]
        public ActionResult Form(EventMaster model, HttpPostedFileBase mainImage, bool removeMainImage = false, int[] selectedCategoryIds = null)
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

                    // Prepare upload folder
                    string uploadPath = Server.MapPath("~/Uploads/Events/");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    // Fetch existing for updates
                    EventMaster existing = null;
                    if (model.EventId != 0)
                    {
                        existing = context.eventmasters.Find(model.EventId);
                    }

                    // Additional images are not supported for events. Ensure cleared.
                    model.AdditionalImages = null;

                    // Handle main image
                    if (removeMainImage && existing != null && !string.IsNullOrWhiteSpace(existing.MainImage))
                    {
                        try
                        {
                            var phys = Server.MapPath("~/" + existing.MainImage.TrimStart('~','/'));
                            if (System.IO.File.Exists(phys)) System.IO.File.Delete(phys);
                        }
                        catch { /* ignore */ }
                        model.MainImage = null;
                    }
                    if (mainImage != null && mainImage.ContentLength > 0)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(mainImage.FileName);
                        mainImage.SaveAs(Path.Combine(uploadPath, fileName));
                        // Store with ~ prefix for consistent path resolution
                        model.MainImage = "~/Uploads/Events/" + fileName;
                    }

                    if (model.EventId == 0)
                    {
                        // Create new
                        model.CreatedBy = User.Identity.Name;
                        model.CreatedDate = DateTime.Now;
                        model.CompanyId = Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]);
                        context.eventmasters.Add(model);
                    }
                    else
                    {
                        // Update existing
                        if (existing != null)
                        {
                            existing.Heading = model.Heading;
                            existing.Caption = model.Caption;
                            existing.Description = model.Description;
                            existing.CategoryTypeIds = model.CategoryTypeIds;
                            existing.EventTime = model.EventTime;
                            existing.EventPlan = model.EventPlan;
                            existing.EventLocation = model.EventLocation;
                            existing.Status = model.Status;
                            existing.ModifiedBy = User.Identity.Name;
                            existing.ModifiedDate = DateTime.Now;

                            // Update main image (can be cleared if removed)
                            if (removeMainImage && string.IsNullOrEmpty(model.MainImage))
                                existing.MainImage = null;
                            else if (!string.IsNullOrEmpty(model.MainImage))
                                existing.MainImage = model.MainImage;

                            // Additional images are not used for events
                            existing.AdditionalImages = null;
                        }
                    }

                    context.SaveChanges();
                    TempData["SuccessMessage"] = "Event saved successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving event: " + ex.Message);
                }
            }

            // If we got this far, something failed, redisplay form
            List<SelectListItem> selectedStatus = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "Inactive", Value = "1", Selected = model.Status == 1 };
            selectedStatus.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "Active", Value = "0", Selected = model.Status == 0 };
            selectedStatus.Add(selectedItem);
            ViewBag.Status = selectedStatus;

            return View(model);
        }

        [Authorize(Roles = "EventMasterEdit,AnnouncementMasterEdit")]
        public ActionResult Edit(int id)
        {
            return RedirectToAction("Form", new { id = id });
        }

        [Authorize(Roles = "EventMasterDelete,AnnouncementMasterDelete")] 
        public ActionResult Delete(int id)
        {
            try
            {
                var eventItem = context.eventmasters.Find(id);
                if (eventItem == null)
                {
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction("Index");
                }

                // Attempt to delete associated image files from disk (ignore IO errors)
                try
                {
                    if (!string.IsNullOrWhiteSpace(eventItem.MainImage) && !eventItem.MainImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        var mainPath = Server.MapPath("~/" + eventItem.MainImage.TrimStart('~','/'));
                        if (System.IO.File.Exists(mainPath)) System.IO.File.Delete(mainPath);
                    }

                    if (!string.IsNullOrWhiteSpace(eventItem.AdditionalImages))
                    {
                        var images = eventItem.AdditionalImages.Split(',').Select(s => (s ?? string.Empty).Trim()).Where(s => !string.IsNullOrWhiteSpace(s));
                        foreach (var rel in images)
                        {
                            if (rel.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
                            var phys = Server.MapPath("~/" + rel.TrimStart('~','/'));
                            if (System.IO.File.Exists(phys)) System.IO.File.Delete(phys);
                        }
                    }
                }
                catch { /* ignore file IO errors during delete */ }

                context.eventmasters.Remove(eventItem);
                context.SaveChanges();
                TempData["SuccessMessage"] = "Event deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting event: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            var data = context.eventmasters
                .Where(x => x.CompanyId == Convert.ToInt32(System.Web.HttpContext.Current.Session["compyid"]))
                .OrderByDescending(x => x.CreatedDate)
                .Skip(param.iDisplayStart)
                .Take(param.iDisplayLength)
                .Select(d => new { 
                    d.EventId, 
                    d.Heading, 
                    d.Caption, 
                    d.MainImage,
                    EventTime = d.EventTime.ToString("dd/MM/yyyy HH:mm"),
                    EventLocation = d.EventLocation ?? "",
                    Status = d.Status == 0 ? "Active" : "Inactive",
                    d.CreatedDate 
                })
                .ToArray();

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }
    }
}

