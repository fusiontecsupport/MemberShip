using ClubMembership.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using ClubMembership.Data;
using Microsoft.AspNet.Identity;

namespace ClubMembership.Controllers
{
    [SessionExpire]
    public class MinutesOfMeetingController : Controller
    {
        private readonly ApplicationDbContext context = new ApplicationDbContext();
        private readonly ClubMembershipDBEntities db = new ClubMembershipDBEntities();

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
                ViewBag.CategoryTypes = Enumerable.Empty<CategoryTypeRow>();
            }
        }

        [Authorize]
        public ActionResult Index(string q = null, DateTime? from = null, DateTime? to = null, string place = null)
        {
            var query = context.minutesofmeetings.AsQueryable();

            // Admin/Index view: show all minutes regardless of category

            // Diagnostics: show total records and a few IDs
            try
            {
                var total = context.minutesofmeetings.Count();
                var sampleIds = context.minutesofmeetings.Select(m => m.MomId).OrderByDescending(id => id).Take(5).ToList();
                ViewBag.MomTotal = total;
                ViewBag.MomSample = string.Join(",", sampleIds);
                System.Diagnostics.Debug.WriteLine("[MOM] Total=" + total + ", SampleIds=" + ViewBag.MomSample);
            }
            catch { /* ignore */ }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    (x.Heading != null && x.Heading.Contains(search)) ||
                    (x.Caption != null && x.Caption.Contains(search)) ||
                    (x.Description != null && x.Description.Contains(search)) ||
                    (x.MeetingPlace != null && x.MeetingPlace.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(place))
            {
                var p = place.Trim();
                query = query.Where(x => x.MeetingPlace != null && x.MeetingPlace.Contains(p));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.MeetingDateAndTime.HasValue && x.MeetingDateAndTime.Value >= from.Value);
            }

            if (to.HasValue)
            {
                // include the entire end day if only a date is supplied
                var toInclusive = to.Value;
                if (toInclusive.TimeOfDay.Ticks == 0)
                {
                    toInclusive = toInclusive.Date.AddDays(1).AddTicks(-1);
                }
                query = query.Where(x => x.MeetingDateAndTime.HasValue && x.MeetingDateAndTime.Value <= toInclusive);
            }

            var list = query
                .OrderByDescending(x => x.MeetingDateAndTime)
                .ToList();

            return View(list);
        }

        // Read-only list for regular users
        [Authorize]
        public ActionResult UserView(string q = null, DateTime? from = null, DateTime? to = null, string place = null)
        {
            var query = context.minutesofmeetings.AsQueryable();
            // Filter by logged-in user's category similar to announcements
            var userId = User.Identity.GetUserId();
            var user = context.Users.FirstOrDefault(u => u.Id == userId);
            int? userCateTid = user == null ? (int?)null : user.CateTid;
            if (userCateTid.HasValue)
            {
                string needle = "," + userCateTid.Value + ",";
                query = query.Where(m => ("," + ((m.CategoryTypeIds ?? "").Replace(" ", "")) + ",").Contains(needle)
                                    || string.IsNullOrEmpty(m.CategoryTypeIds)
                                    || (m.CategoryTypeIds.Replace(" ", "") == ""));
            }
            else
            {
                query = query.Where(m => string.IsNullOrEmpty(m.CategoryTypeIds) || (m.CategoryTypeIds.Replace(" ", "") == ""));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();
                query = query.Where(x =>
                    (x.Heading != null && x.Heading.Contains(search)) ||
                    (x.Caption != null && x.Caption.Contains(search)) ||
                    (x.Description != null && x.Description.Contains(search)) ||
                    (x.MeetingPlace != null && x.MeetingPlace.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(place))
            {
                var p = place.Trim();
                query = query.Where(x => x.MeetingPlace != null && x.MeetingPlace.Contains(p));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.MeetingDateAndTime.HasValue && x.MeetingDateAndTime.Value >= from.Value);
            }

            if (to.HasValue)
            {
                var toInclusive = to.Value;
                if (toInclusive.TimeOfDay.Ticks == 0)
                {
                    toInclusive = toInclusive.Date.AddDays(1).AddTicks(-1);
                }
                query = query.Where(x => x.MeetingDateAndTime.HasValue && x.MeetingDateAndTime.Value <= toInclusive);
            }

            var list = query
                .OrderByDescending(x => x.MeetingDateAndTime)
                .ToList();

            return View(list);
        }

        public ActionResult Form(int? id = 0)
        {
            MinutesOfMeeting model = new MinutesOfMeeting();
            if (id != null && id != 0)
            {
                model = context.minutesofmeetings.Find(id);
                if (model == null) return HttpNotFound();
            }
            else
            {
                model.MeetingDateAndTime = DateTime.Now;
            }
            PopulateCategoryTypes();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Form(MinutesOfMeeting model, int[] selectedCategoryIds = null,
            System.Web.HttpPostedFileBase Attachment1 = null,
            System.Web.HttpPostedFileBase Attachment2 = null,
            System.Web.HttpPostedFileBase Attachment3 = null,
            bool removeAttachment1 = false,
            bool removeAttachment2 = false,
            bool removeAttachment3 = false)
        {
            if (!ModelState.IsValid)
            {
                PopulateCategoryTypes();
                return View(model);
            }

            // Validate attachments: types and sizes
            long size1 = Attachment1?.ContentLength ?? 0;
            long size2 = Attachment2?.ContentLength ?? 0;
            long size3 = Attachment3?.ContentLength ?? 0;
            long totalSize = size1 + size2 + size3;
            if (size1 > 0 && size1 > 2 * 1024 * 1024) ModelState.AddModelError("", "Attachment 1 exceeds 2 MB.");
            if (size2 > 0 && size2 > 2 * 1024 * 1024) ModelState.AddModelError("", "Attachment 2 exceeds 2 MB.");
            if (size3 > 0 && size3 > 2 * 1024 * 1024) ModelState.AddModelError("", "Attachment 3 exceeds 2 MB.");
            if (totalSize > 6 * 1024 * 1024) ModelState.AddModelError("", "Total attachments exceed 6 MB.");

            string[] allowedExt = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            Func<System.Web.HttpPostedFileBase, bool> extOk = f =>
            {
                if (f == null || f.ContentLength == 0) return true;
                var ext = System.IO.Path.GetExtension(f.FileName)?.ToLowerInvariant();
                return allowedExt.Contains(ext);
            };
            if (!extOk(Attachment1)) ModelState.AddModelError("", "Attachment 1 must be JPG, PNG or PDF.");
            if (!extOk(Attachment2)) ModelState.AddModelError("", "Attachment 2 must be JPG, PNG or PDF.");
            if (!extOk(Attachment3)) ModelState.AddModelError("", "Attachment 3 must be JPG, PNG or PDF.");

            if (!ModelState.IsValid)
            {
                PopulateCategoryTypes();
                return View(model);
            }

            // Handle category selection like announcements
            if (selectedCategoryIds != null && selectedCategoryIds.Any())
            {
                model.CategoryTypeIds = string.Join(",", selectedCategoryIds);
            }
            else
            {
                model.CategoryTypeIds = null;
            }

            if (model.MomId == 0)
            {
                // Generate next id since DB column does not auto-increment
                var maxId = 0;
                if (context.minutesofmeetings.Any())
                {
                    maxId = context.minutesofmeetings.Max(x => x.MomId);
                }
                model.MomId = maxId + 1;
                context.minutesofmeetings.Add(model);
                context.SaveChanges();

                // Save attachments after MomId is available
                SaveMomAttachments(model.MomId, ref model, Attachment1, Attachment2, Attachment3);
                context.Entry(model).State = System.Data.Entity.EntityState.Modified;
            }
            else
            {
                var existing = context.minutesofmeetings.Find(model.MomId);
                if (existing == null) return HttpNotFound();
                // Map fields explicitly
                existing.Heading = model.Heading;
                existing.Caption = model.Caption;
                existing.Description = model.Description;
                existing.MeetingDateAndTime = model.MeetingDateAndTime;
                existing.MeetingPlace = model.MeetingPlace;
                existing.ConductedBy = model.ConductedBy;
                existing.OrganizedBy = model.OrganizedBy;
                existing.MembersAttended = model.MembersAttended;
                existing.MembersInvited = model.MembersInvited;
                existing.CategoryTypeIds = model.CategoryTypeIds;

                // Note: Delete operations now handled by immediate AJAX delete buttons

                // Save/replace attachments if new files were uploaded
                SaveMomAttachments(existing.MomId, ref existing, Attachment1, Attachment2, Attachment3);
            }

            context.SaveChanges();
            TempData["SuccessMessage"] = "Saved successfully";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            return RedirectToAction("Form", new { id = id });
        }

        public ActionResult Delete(int id)
        {
            var existing = context.minutesofmeetings.Find(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Record not found";
                return RedirectToAction("Index");
            }

            context.minutesofmeetings.Remove(existing);
            context.SaveChanges();
            TempData["SuccessMessage"] = "Deleted successfully";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("UserView");
            }

            var item = context.minutesofmeetings.Find(id.Value);
            if (item == null)
            {
                return HttpNotFound();
            }

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Details", item);
            }
            return View("Details", item);
        }

        [HttpPost]
        public JsonResult DeleteAttachment(int momId, int attachmentSlot)
        {
            try
            {
                var entity = context.minutesofmeetings.Find(momId);
                if (entity == null)
                {
                    return Json(new { success = false, message = "Record not found" });
                }

                string pathToDelete = null;
                switch (attachmentSlot)
                {
                    case 1:
                        pathToDelete = entity.Attachment1Path;
                        entity.Attachment1Path = null;
                        break;
                    case 2:
                        pathToDelete = entity.Attachment2Path;
                        entity.Attachment2Path = null;
                        break;
                    case 3:
                        pathToDelete = entity.Attachment3Path;
                        entity.Attachment3Path = null;
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid attachment slot" });
                }

                // Delete physical file
                if (!string.IsNullOrWhiteSpace(pathToDelete))
                {
                    TryDeletePhysical(pathToDelete);
                }

                // Update database
                context.SaveChanges();

                return Json(new { success = true, message = "Attachment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting attachment: " + ex.Message });
            }
        }

        private void SaveMomAttachments(int momId, ref MinutesOfMeeting entity,
            System.Web.HttpPostedFileBase a1,
            System.Web.HttpPostedFileBase a2,
            System.Web.HttpPostedFileBase a3)
        {
            try
            {
                var baseDir = Server.MapPath("~/Uploads/MinutesOfMeeting/" + momId);
                System.IO.Directory.CreateDirectory(baseDir);

                Func<System.Web.HttpPostedFileBase, string> saveOne = (file) =>
                {
                    if (file == null || file.ContentLength == 0) return null;
                    var ext = System.IO.Path.GetExtension(file.FileName);
                    var name = Guid.NewGuid().ToString("N") + ext;
                    var fullPath = System.IO.Path.Combine(baseDir, name);
                    file.SaveAs(fullPath);
                    var rel = "~/Uploads/MinutesOfMeeting/" + momId + "/" + name;
                    return rel;
                };

                var p1 = saveOne(a1);
                if (!string.IsNullOrEmpty(p1))
                {
                    // replace: delete old physical if exists
                    if (!string.IsNullOrWhiteSpace(entity.Attachment1Path)) TryDeletePhysical(entity.Attachment1Path);
                    entity.Attachment1Path = p1;
                }
                var p2 = saveOne(a2);
                if (!string.IsNullOrEmpty(p2))
                {
                    if (!string.IsNullOrWhiteSpace(entity.Attachment2Path)) TryDeletePhysical(entity.Attachment2Path);
                    entity.Attachment2Path = p2;
                }
                var p3 = saveOne(a3);
                if (!string.IsNullOrEmpty(p3))
                {
                    if (!string.IsNullOrWhiteSpace(entity.Attachment3Path)) TryDeletePhysical(entity.Attachment3Path);
                    entity.Attachment3Path = p3;
                }
            }
            catch (Exception ex)
            {
                // Swallow errors but log to debug to avoid breaking save
                System.Diagnostics.Debug.WriteLine("SaveMomAttachments error: " + ex);
            }
        }

        private void TryDeletePhysical(string relativePath)
        {
            try
            {
                var path = relativePath;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    // normalize
                    var rooted = path.StartsWith("/") || path.StartsWith("~") ? path : ("~/" + path.TrimStart('~','/'));
                    var full = Server.MapPath(rooted);
                    if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("TryDeletePhysical error: " + ex);
            }
        }
    }
}
