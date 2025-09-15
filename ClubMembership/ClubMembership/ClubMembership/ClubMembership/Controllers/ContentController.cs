using ClubMembership.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace ClubMembership.Controllers
{
    [Authorize]
    public class ContentController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ContentController()
        {
            _db = new ApplicationDbContext();
        }

        // GET: /Content/Announcements
        public ActionResult Announcements()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            int? userCateTid = user?.CateTid;

            var items = new List<AnnouncementMaster>();
            if (userCateTid.HasValue)
            {
                string needle = "," + userCateTid.Value + ",";
                items = _db.announcementmasters
                    .Where(a => a.Status == 0 &&
                                ("," + (a.CategoryTypeIds ?? "") + ",").Contains(needle))
                    .OrderByDescending(a => a.CreatedDate)
                    .ToList();
            }

            return View(items);
        }

        // GET: /Content/Events
        public ActionResult Events()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            int? userCateTid = user?.CateTid;

            var items = new List<EventMaster>();
            if (userCateTid.HasValue)
            {
                string needle = "," + userCateTid.Value + ",";
                items = _db.eventmasters
                    .Where(e => e.Status == 0 &&
                                ("," + (e.CategoryTypeIds ?? "") + ",").Contains(needle))
                    .OrderByDescending(e => e.EventTime)
                    .ToList();
            }
            return View(items);
        }

        // GET: /Content/Gallery
        public ActionResult Gallery()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            int? userCateTid = user?.CateTid;

            var items = new List<GalleryMaster>();
            if (userCateTid.HasValue)
            {
                string needle = "," + userCateTid.Value + ",";
                items = _db.gallerymasters
                    .Where(g => g.Status == 0 &&
                                (string.IsNullOrEmpty(g.CategoryTypeIds) || 
                                 ("," + g.CategoryTypeIds + ",").Contains(needle)))
                    .OrderByDescending(g => g.CreatedDate)
                    .ToList();
            }
            return View(items);
        }

        // GET: /Content/Search?q=...
        public ActionResult Search(string q)
        {
            var normalized = (q ?? "").Trim();
            var model = new ContentSearchViewModel
            {
                Query = normalized,
                Announcements = new List<AnnouncementMaster>(),
                Events = new List<EventMaster>()
            };

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var userId = User.Identity.GetUserId();
                var user = _db.Users.FirstOrDefault(u => u.Id == userId);
                int? userCateTid = user?.CateTid;

                if (userCateTid.HasValue)
                {
                    string needle = "," + userCateTid.Value + ",";
                    model.Announcements = _db.announcementmasters
                        .Where(a => a.Status == 0 &&
                                    ("," + (a.CategoryTypeIds ?? "") + ",").Contains(needle) &&
                                    ((a.Heading ?? "").Contains(normalized) ||
                                     (a.Caption ?? "").Contains(normalized) ||
                                     (a.Description ?? "").Contains(normalized)))
                        .OrderByDescending(a => a.CreatedDate)
                        .Take(50)
                        .ToList();
                }

                if (userCateTid.HasValue)
                {
                    string needle = "," + userCateTid.Value + ",";
                    model.Events = _db.eventmasters
                        .Where(e => e.Status == 0 &&
                                    ("," + (e.CategoryTypeIds ?? "") + ",").Contains(needle) &&
                                    ((e.Heading ?? "").Contains(normalized) ||
                                     (e.Caption ?? "").Contains(normalized) ||
                                     (e.Description ?? "").Contains(normalized) ||
                                     (e.EventLocation ?? "").Contains(normalized)))
                        .OrderByDescending(e => e.EventTime)
                        .Take(50)
                        .ToList();
                }
            }

            return View(model);
        }

        // POST: /Content/SetEventInterest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SetEventInterest(int eventId, string state)
        {
            EnsureEventInterestTable();
            // state: "interested" | "not_interested" | "none"
            short code = 0;
            if (string.Equals(state, "interested", StringComparison.OrdinalIgnoreCase)) code = 1;
            else if (string.Equals(state, "not_interested", StringComparison.OrdinalIgnoreCase)) code = -1;
            else code = 0;

            var userId = User.Identity.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var existing = _db.eventinterests.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
            if (existing == null)
            {
                existing = new EventInterest
                {
                    EventId = eventId,
                    UserId = userId,
                    State = code,
                    CreatedDate = DateTime.UtcNow
                };
                _db.eventinterests.Add(existing);
            }
            else
            {
                existing.State = code;
                existing.ModifiedDate = DateTime.UtcNow;
            }

            _db.SaveChanges();

            var interestedCount = _db.eventinterests.Count(x => x.EventId == eventId && x.State == 1);
            var notInterestedCount = _db.eventinterests.Count(x => x.EventId == eventId && x.State == -1);

            return Json(new { success = true, eventId = eventId, state = code, interestedCount = interestedCount, notInterestedCount = notInterestedCount });
        }

        // GET: /Content/GetEventInterest?eventId=123
        [HttpGet]
        public JsonResult GetEventInterest(int eventId)
        {
            EnsureEventInterestTable();
            var userId = User.Identity.GetUserId();
            short code = 0;
            if (!string.IsNullOrEmpty(userId))
            {
                var existing = _db.eventinterests.FirstOrDefault(x => x.EventId == eventId && x.UserId == userId);
                if (existing != null)
                {
                    code = existing.State;
                }
            }

            var interestedCount = _db.eventinterests.Count(x => x.EventId == eventId && x.State == 1);
            var notInterestedCount = _db.eventinterests.Count(x => x.EventId == eventId && x.State == -1);

            return Json(new { success = true, eventId = eventId, state = code, interestedCount = interestedCount, notInterestedCount = notInterestedCount }, JsonRequestBehavior.AllowGet);
        }

        private void EnsureEventInterestTable()
        {
            var sql = @"
IF OBJECT_ID(N'[dbo].[EVENTINTEREST]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EVENTINTEREST](
        [EventInterestId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [EventId] INT NOT NULL,
        [UserId] NVARCHAR(128) NOT NULL,
        [State] SMALLINT NOT NULL,
        [CreatedDate] DATETIME NOT NULL,
        [ModifiedDate] DATETIME NULL
    );
    CREATE UNIQUE INDEX [IX_Event_User] ON [dbo].[EVENTINTEREST]([EventId], [UserId]);
END";
            _db.Database.ExecuteSqlCommand(sql);
        }
    }
}


