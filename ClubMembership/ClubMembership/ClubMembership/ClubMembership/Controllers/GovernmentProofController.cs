using ClubMembership.Data;
using ClubMembership.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ClubMembership.Controllers
{
    // [Authorize(Roles = "Admin")]
    public class GovernmentProofController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: GovernmentProof
        public ActionResult Index()
        {
            try
            {
                // First, let's test if we can access the database
                var count = db.GovernmentProofs.Count();
                
                // If that works, get the data
                var governmentProofs = db.GovernmentProofs
                    .Include(g => g.MemberShipMaster)
                    .OrderByDescending(g => g.Id)
                    .ToList();
                
                return View(governmentProofs);
            }
            catch (Exception ex)
            {
                // Log the error and return a simple message for debugging
                return Content("Error: " + ex.Message + "<br/>Stack Trace: " + ex.StackTrace);
            }
        }

        // GET: GovernmentProof/Test
        public ActionResult Test()
        {
            try
            {
                // Test direct SQL query to see if the table exists and has data
                var result = db.Database.SqlQuery<object>("SELECT COUNT(*) FROM govrmnet_proof").FirstOrDefault();
                return Content("GovernmentProof Controller is working! Table count: " + result);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        // GET: GovernmentProof/RawData
        public ActionResult RawData()
        {
            try
            {
                // Get data using raw SQL
                var data = db.Database.SqlQuery<object>(@"
                    SELECT TOP (1000) [gp_id], [MemberID], [gov_path]
                    FROM [ClubMembershipDB].[dbo].[govrmnet_proof]
                ").ToList();
                
                var result = "Raw Data:<br/>";
                foreach (var item in data)
                {
                    result += item.ToString() + "<br/>";
                }
                
                return Content(result);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        // GET: GovernmentProof/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            GovernmentProof governmentProof = db.GovernmentProofs
                .Include(g => g.MemberShipMaster)
                .FirstOrDefault(g => g.Id == id);
                
            if (governmentProof == null)
            {
                return HttpNotFound();
            }
            
            return View(governmentProof);
        }

        // GET: GovernmentProof/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            GovernmentProof governmentProof = db.GovernmentProofs
                .Include(g => g.MemberShipMaster)
                .FirstOrDefault(g => g.Id == id);
                
            if (governmentProof == null)
            {
                return HttpNotFound();
            }
            
            return View(governmentProof);
        }

        // POST: GovernmentProof/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            GovernmentProof governmentProof = db.GovernmentProofs.Find(id);
            if (governmentProof != null)
            {
                // Delete the physical file
                string filePath = Server.MapPath(governmentProof.GovPath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                
                db.GovernmentProofs.Remove(governmentProof);
                db.SaveChanges();
            }
            
            return RedirectToAction("Index");
        }

        // GET: GovernmentProof/Download/5
        public ActionResult Download(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            GovernmentProof governmentProof = db.GovernmentProofs.Find(id);
            if (governmentProof == null)
            {
                return HttpNotFound();
            }
            
            string filePath = Server.MapPath(governmentProof.GovPath);
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound();
            }
            
            string fileName = Path.GetFileName(filePath);
            string contentType = GetContentType(fileName);
            
            return File(filePath, contentType, fileName);
        }

        private string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                default:
                    return "application/octet-stream";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
