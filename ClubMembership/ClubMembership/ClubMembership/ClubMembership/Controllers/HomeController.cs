using ClubMembership.Data;
using ClubMembership.Filters;
using ClubMembership.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using log4net;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClubMembership.Controllers
{
    // Model for Event Interest Statistics
    public class EventInterestStat
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string EventLocation { get; set; }
        public int InterestedCount { get; set; }
        public int NotInterestedCount { get; set; }
        public int TotalResponses { get; set; }
    }

    // Simple DTOs for Notifications sidebar
    public class SimpleMember
    {
        public int MemberID { get; set; }
        public string Member_Name { get; set; }
        public string Member_Photo_Path { get; set; }
    }

    public class SpouseBirthday
    {
        public int MemberID { get; set; }
        public string Member_Name { get; set; }
        public string Spouse_Name { get; set; }
    }

    public class AnniversaryItem
    {
        public int MemberID { get; set; }
        public string Member_Name { get; set; }
        public string Spouse_Name { get; set; }
        public DateTime Date_Of_Marriage { get; set; }
    }

    // [AuthActionFilter]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        //private static readonly ILog log = LogManager.GetLogger(typeof(MembersController));

        public HomeController()
        {
            _db = new ApplicationDbContext();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendWish(int memberId, string type, string recipient, string note)
        {
            // type: "birthday" | "anniversary"
            // recipient: "member" | "spouse" (for spouse birthday) | "couple" (for anniversary)
            try
            {
                var member = _db.MemberShipMasters.FirstOrDefault(m => m.MemberID == memberId && m.DispStatus == 1);
                if (member == null)
                {
                    Response.StatusCode = 404;
                    return Json(new { success = false, message = "Member not found" });
                }

                var toEmail = member.Member_EmailID;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    Response.StatusCode = 400;
                    return Json(new { success = false, message = "Recipient has no email on file" });
                }

                // Use logged-in user's FirstName for personalization
                var uid = User.Identity.GetUserId();
                var senderUser = _db.Users.FirstOrDefault(u => u.Id == uid);
                var senderName = !string.IsNullOrWhiteSpace(senderUser?.FirstName) ? senderUser.FirstName : (User.Identity.GetUserName() ?? "A fellow member");
                string subject;
                string body;

                if (string.Equals(type, "birthday", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(recipient, "spouse", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(member.Spouse_Name))
                    {
                        subject = $"Warm Birthday Wishes, {member.Spouse_Name}!";
                        body = BuildWishEmailHtml(
                            $"Happy Birthday, {member.Spouse_Name}!",
                            $"Wishing you a day filled with joy and a wonderful year ahead.",
                            senderName,
                            member.Spouse_Name,
                            note
                        );
                    }
                    else
                    {
                        subject = $"Happy Birthday, {member.Member_Name}!";
                        body = BuildWishEmailHtml(
                            $"Happy Birthday, {member.Member_Name}!",
                            $"Have an amazing day and a fantastic year ahead.",
                            senderName,
                            member.Member_Name,
                            note
                        );
                    }
                }
                else
                {
                    // anniversary
                    var couple = !string.IsNullOrWhiteSpace(member.Spouse_Name)
                        ? $"{member.Member_Name} & {member.Spouse_Name}"
                        : member.Member_Name;
                    subject = $"Happy Anniversary, {couple}!";
                    body = BuildWishEmailHtml(
                        $"Happy Anniversary, {couple}!",
                        "Wishing you continued love and happiness together!",
                        senderName,
                        couple,
                        note
                    );
                }

                var sent = TrySendEmail(toEmail, subject, body, senderName, out var err);
                if (!sent)
                {
                    Response.StatusCode = 500;
                    return Json(new { success = false, message = err ?? "Failed to send email" });
                }

                return Json(new { success = true, message = "Wish sent successfully" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string BuildWishEmailHtml(string title, string message, string senderFirstName, string recipientName, string note)
        {
            // Lightweight, email-client friendly HTML with inline styles
            return $@"<!DOCTYPE html>
<html>
  <head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1' />
    <title>{System.Net.WebUtility.HtmlEncode(title)}</title>
  </head>
  <body style='margin:0;padding:0;background:#f6f7fb;'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='background:#f6f7fb;padding:24px 12px;'>
      <tr>
        <td align='center'>
          <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='max-width:560px;background:#ffffff;border-radius:12px;box-shadow:0 4px 16px rgba(0,0,0,0.06);overflow:hidden;'>
            <tr>
              <td style='background:#0d6efd;height:6px;'></td>
            </tr>
            <tr>
              <td style='padding:24px 24px 8px 24px;font-family:Segoe UI,Roboto,Arial,sans-serif;'>
                <h2 style='margin:0 0 10px 0;color:#0d6efd;font-size:22px;font-weight:700;'>{System.Net.WebUtility.HtmlEncode(title)}</h2>
                <p style='margin:0;color:#6c757d;font-size:14px;'>A warm message from a fellow club member</p>
              </td>
            </tr>
            <tr>
              <td style='padding:8px 24px 24px 24px;font-family:Segoe UI,Roboto,Arial,sans-serif;color:#212529;'>
                <p style='font-size:16px;line-height:1.6;margin:0 0 12px 0;'>Dear {System.Net.WebUtility.HtmlEncode(recipientName)},</p>
                <p style='font-size:16px;line-height:1.6;margin:0 0 12px 0;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
                {(string.IsNullOrWhiteSpace(note) ? "" : ("<div style='background:#f8f9fa;border:1px solid #eef2f4;border-radius:8px;padding:12px 14px;margin:0 0 12px 0;'><div style='color:#6c757d;font-size:12px;margin-bottom:6px;'>Personal message</div><div style='font-size:15px;line-height:1.6;color:#212529;'>" + System.Net.WebUtility.HtmlEncode(note) + "</div></div>"))}
                <p style='font-size:16px;line-height:1.6;margin:0 0 12px 0;'>Best regards,<br/><strong>{System.Net.WebUtility.HtmlEncode(senderFirstName)}</strong></p>
              </td>
            </tr>
            <tr>
              <td style='padding:14px 24px 22px 24px;font-family:Segoe UI,Roboto,Arial,sans-serif;border-top:1px solid #eef2f4;color:#6c757d;font-size:12px;'>
                <div>Sent via Club Membership Portal</div>
              </td>
            </tr>
          </table>
          <div style='color:#adb5bd;font-size:12px;margin-top:10px;font-family:Segoe UI,Roboto,Arial,sans-serif;'>
            &copy; {DateTime.Now:yyyy} Club Membership
          </div>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        private bool TrySendEmail(string to, string subject, string body, string senderDisplayName, out string error)
        {
            error = null;
            try
            {
                // Ensure TLS 1.2 for Gmail/modern SMTP
                try { System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12; } catch { }

                // Uses SMTP settings from Web.config <system.net><mailSettings><smtp>
                using (var mail = new System.Net.Mail.MailMessage())
                {
                    mail.To.Add(to);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true; // allow basic formatting

                    // Respect Web.config <system.net><mailSettings><smtp from="...">. If not present, use DEFAULT_FROM_EMAIL.
                    if (mail.From == null)
                    {
                        var fallbackFrom = System.Configuration.ConfigurationManager.AppSettings["DEFAULT_FROM_EMAIL"] ?? "support@fusiontec.com";
                        mail.From = new System.Net.Mail.MailAddress(fallbackFrom, string.IsNullOrWhiteSpace(senderDisplayName) ? "Club Membership" : senderDisplayName);
                    }

                    using (var smtp = new System.Net.Mail.SmtpClient())
                    {
                        smtp.Send(mail);
                    }
                }
                return true;
            }
            catch (System.Net.Mail.SmtpException smtpEx)
            {
                var code = smtpEx.StatusCode;
                var msg = smtpEx.Message;
                var inner = smtpEx.InnerException != null ? (": " + smtpEx.InnerException.Message) : string.Empty;
                error = $"SMTP error ({code}): {msg}{inner}";
                return false;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? (": " + ex.InnerException.Message) : string.Empty;
                error = ex.Message + inner;
                return false;
            }
        }

        public ActionResult AdminDashboard()
        {
            if (!User.IsInRole("Admin"))
            {
                return RedirectToAction("Index");
            }

            // Total members
            int totalMembers = _db.MemberShipMasters.Count();

            // Total amount received
            decimal totalAmount = _db.MemberShipPaymentDetails.Sum(p => (decimal?)p.Amount) ?? 0;

            // Removed members in last 15 days (assuming DispStatus = 0 means removed, adjust as needed)
            DateTime fifteenDaysAgo = DateTime.Now.AddDays(-15);
            int removedMembers = _db.MemberShipMasters.Count(m => m.DispStatus == 0 && m.Member_Edate >= fifteenDaysAgo);

            // Advanced stats
            DateTime today = DateTime.Now.Date;
            DateTime last30Days = today.AddDays(-30);
            DateTime next30Days = today.AddDays(30);

            int newMembersLast30 = _db.MemberShipMasters.Count(m => m.Member_Reg_Date >= last30Days);
            int upcomingRenewalsNext30 = 0; // will compute from payments below
            int renewalsLt15 = 0; // will compute from payments below
            int renewals15to30 = 0; // will compute from payments below
            int activeMembers = _db.MemberShipMasters.Count(m => m.DispStatus == 1);
            int inactiveMembers = totalMembers - activeMembers;
            decimal amountLast30Days = _db.MemberShipPaymentDetails.Where(p => p.Payment_Date >= last30Days).Sum(p => (decimal?)p.Amount) ?? 0;

            var paymentBreakdown = _db.MemberShipPaymentDetails
                .GroupBy(p => p.Payment_Type)
                .Select(g => new PaymentTypeTotal { Type = g.Key, Total = g.Sum(p => p.Amount) })
                .ToList();

            var recentPayments = _db.MemberShipPaymentDetails
                .OrderByDescending(p => p.Payment_Date)
                .Take(5)
                .ToList();

            // Upcoming renewals based on latest successful payment Renewal_Date per member
            var successRenewals = _db.MemberShipPaymentDetails
                .Where(p => p.Payment_Status == "Success")
                .GroupBy(p => p.MemberID)
                .Select(g => new { MemberID = g.Key, RenewalDate = g.Max(p => p.Renewal_Date) })
                .ToList();

            var allRenewals = _db.MemberShipPaymentDetails
                .GroupBy(p => p.MemberID)
                .Select(g => new { MemberID = g.Key, RenewalDate = g.Max(p => p.Renewal_Date) })
                .ToList();

            var preferredRenewals = successRenewals.ToDictionary(x => x.MemberID, x => x.RenewalDate);
            foreach (var r in allRenewals)
            {
                if (!preferredRenewals.ContainsKey(r.MemberID))
                {
                    preferredRenewals[r.MemberID] = r.RenewalDate;
                }
            }

            var activeMembersSlim = _db.MemberShipMasters
                .Where(m => m.DispStatus == 1)
                .Select(m => new { m.MemberID, m.Member_Name })
                .ToList();

            var upcomingRenewals = preferredRenewals
                .Select(kvp => new { MemberID = kvp.Key, RenewalDate = kvp.Value })
                .Join(activeMembersSlim, r => r.MemberID, m => m.MemberID, (r, m) => new RenewalInfo
                {
                    MemberID = m.MemberID,
                    MemberName = m.Member_Name,
                    RenewalDate = r.RenewalDate,
                    DaysRemaining = (int)(r.RenewalDate.Date - today).TotalDays
                })
                .Where(x => x.RenewalDate.Date >= today && x.RenewalDate.Date <= next30Days)
                .OrderBy(x => x.DaysRemaining)
                .ToList();

            upcomingRenewalsNext30 = upcomingRenewals.Count;
            renewalsLt15 = upcomingRenewals.Count(r => r.DaysRemaining < 15);
            renewals15to30 = upcomingRenewals.Count(r => r.DaysRemaining >= 15 && r.DaysRemaining <= 30);

            // Build last 7 days collections trend (successful payments only)
            var fromDate = today.AddDays(-6);
            var dailyRaw = _db.MemberShipPaymentDetails
                .Where(p => DbFunctions.TruncateTime(p.Payment_Date) >= fromDate && p.Payment_Status == "Success")
                .GroupBy(p => DbFunctions.TruncateTime(p.Payment_Date))
                .Select(g => new { Date = g.Key, Total = g.Sum(p => p.Amount) })
                .ToList();

            var dailyLabels = new List<string>();
            var dailyTotals = new List<decimal>();
            for (int i = 0; i < 7; i++)
            {
                var d = today.AddDays(-6 + i);
                dailyLabels.Add(d.ToString("dd MMM"));
                var match = dailyRaw.FirstOrDefault(x => x.Date.HasValue && x.Date.Value == d);
                decimal val = 0;
                if (match != null)
                {
                    val = match.Total;
                }
                dailyTotals.Add(val);
            }

            ViewBag.TotalMembers = totalMembers;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.RemovedMembers = removedMembers;
            ViewBag.NewMembersLast30 = newMembersLast30;
            ViewBag.UpcomingRenewalsNext30 = upcomingRenewalsNext30;
            ViewBag.RenewalsLt15 = renewalsLt15;
            ViewBag.Renewals15to30 = renewals15to30;
            ViewBag.ActiveMembers = activeMembers;
            ViewBag.InactiveMembers = inactiveMembers;
            ViewBag.AmountLast30Days = amountLast30Days;
            ViewBag.PaymentBreakdown = paymentBreakdown;
            ViewBag.RecentPayments = recentPayments;
            ViewBag.UpcomingRenewals = upcomingRenewals;
            ViewBag.DailyLabels = dailyLabels;
            ViewBag.DailyTotals = dailyTotals;

            // Event Interest Statistics - Using raw SQL to properly handle data type mismatch
            var eventInterestStats = new List<EventInterestStat>();
            try
            {
                // Use raw SQL to handle the GUID vs INT mismatch between EVENTINTEREST and EVENTMASTER
                // Since EVENTINTEREST.EventId is GUID and EVENTMASTER.EventId is INT, we need to handle this carefully
                var sql = @"
                    SELECT TOP 10
                        ei.EventId,
                        ISNULL(em.Heading, 'Event ID: ' + CAST(ei.EventId AS VARCHAR(50))) as EventName,
                        ISNULL(em.EventTime, GETDATE()) as EventDate,
                        ISNULL(em.EventLocation, 'Location TBA') as EventLocation,
                        SUM(CASE WHEN ei.State = 1 THEN 1 ELSE 0 END) as InterestedCount,
                        SUM(CASE WHEN ei.State = -1 THEN 1 ELSE 0 END) as NotInterestedCount,
                        COUNT(*) as TotalResponses
                    FROM EVENTINTEREST ei
                    LEFT JOIN EVENTMASTER em ON CASE 
                        WHEN ISNUMERIC(ei.EventId) = 1 THEN CAST(ei.EventId AS INT) = em.EventId 
                        ELSE 0 = 1 
                    END
                    GROUP BY ei.EventId, em.Heading, em.EventTime, em.EventLocation
                    ORDER BY COUNT(*) DESC, SUM(CASE WHEN ei.State = 1 THEN 1 ELSE 0 END) DESC";

                eventInterestStats = _db.Database.SqlQuery<EventInterestStat>(sql).ToList();

                // If the SQL query fails or returns no results, fall back to basic stats
                if (!eventInterestStats.Any())
                {
                    var basicStats = _db.eventinterests
                        .GroupBy(ei => ei.EventId)
                        .Select(g => new EventInterestStat
                        {
                            EventId = g.Key.ToString(),
                            EventName = "Event ID: " + g.Key.ToString(),
                            EventDate = DateTime.Now,
                            EventLocation = "Location TBA",
                            InterestedCount = g.Count(x => x.State == 1),
                            NotInterestedCount = g.Count(x => x.State == -1),
                            TotalResponses = g.Count()
                        })
                        .OrderByDescending(x => x.TotalResponses)
                        .ThenByDescending(x => x.InterestedCount)
                        .Take(10)
                        .ToList();

                    eventInterestStats = basicStats;
                }
            }
            catch (Exception ex)
            {
                // If everything fails, create basic stats from EVENTINTEREST only
                try
                {
                    var basicStats = _db.eventinterests
                        .GroupBy(ei => ei.EventId)
                        .Select(g => new EventInterestStat
                        {
                            EventId = g.Key.ToString(),
                            EventName = "Event ID: " + g.Key.ToString(),
                            EventDate = DateTime.Now,
                            EventLocation = "Location TBA",
                            InterestedCount = g.Count(x => x.State == 1),
                            NotInterestedCount = g.Count(x => x.State == -1),
                            TotalResponses = g.Count()
                        })
                        .OrderByDescending(x => x.TotalResponses)
                        .ThenByDescending(x => x.InterestedCount)
                        .Take(10)
                        .ToList();

                    eventInterestStats = basicStats;
                }
                catch
                {
                    eventInterestStats = new List<EventInterestStat>();
                }
            }

            var totalEventInterest = _db.eventinterests.Count();
            var totalInterested = _db.eventinterests.Count(x => x.State == 1);
            var totalNotInterested = _db.eventinterests.Count(x => x.State == -1);
            var totalEventsWithInterest = _db.eventinterests.Select(x => x.EventId).Distinct().Count();

            // Ensure we have valid data
            if (eventInterestStats == null)
            {
                eventInterestStats = new List<EventInterestStat>();
            }

            // Enrich with names/locations from EVENTMASTER when possible (handles string vs int id)
            try
            {
                var eventLookup = _db.eventmasters
                    .Select(e => new { e.EventId, e.Heading, e.EventLocation, e.EventTime })
                    .ToList()
                    .ToDictionary(x => x.EventId, x => x);

                foreach (var s in eventInterestStats)
                {
                    if (s == null || string.IsNullOrWhiteSpace(s.EventId)) { continue; }
                    int parsedId;
                    if (int.TryParse(s.EventId, out parsedId) && eventLookup.ContainsKey(parsedId))
                    {
                        var em = eventLookup[parsedId];
                        s.EventName = string.IsNullOrWhiteSpace(em.Heading) ? s.EventName : em.Heading;
                        s.EventLocation = string.IsNullOrWhiteSpace(em.EventLocation) ? s.EventLocation : em.EventLocation;
                        s.EventDate = em.EventTime != default(DateTime) ? em.EventTime : s.EventDate;
                    }
                }
            }
            catch { /* best-effort enrichment */ }

            // Debug: Log what we actually got
            System.Diagnostics.Debug.WriteLine($"Event Interest Stats Count: {eventInterestStats.Count}");
            foreach (var stat in eventInterestStats.Take(3))
            {
                System.Diagnostics.Debug.WriteLine($"Event: {stat.EventName}, Location: {stat.EventLocation}, Interested: {stat.InterestedCount}");
            }

            // Debug: Check what's actually in EVENTINTEREST table
            var sampleEventInterest = _db.eventinterests.FirstOrDefault();
            if (sampleEventInterest != null)
            {
                System.Diagnostics.Debug.WriteLine($"Sample EventInterest EventId: {sampleEventInterest.EventId}, Type: {sampleEventInterest.EventId.GetType().Name}");
            }

            // Debug: Check what's actually in EVENTMASTER table
            var sampleEventMaster = _db.eventmasters.FirstOrDefault();
            if (sampleEventMaster != null)
            {
                System.Diagnostics.Debug.WriteLine($"Sample EventMaster EventId: {sampleEventMaster.EventId}, Type: {sampleEventMaster.EventId.GetType().Name}");
            }

            ViewBag.EventInterestStats = eventInterestStats;
            ViewBag.TotalEventInterest = totalEventInterest;
            ViewBag.TotalInterested = totalInterested;
            ViewBag.TotalNotInterested = totalNotInterested;
            ViewBag.TotalEventsWithInterest = totalEventsWithInterest;

            return View();
        }

        [HttpGet]
        public ActionResult RenewalPopup(int memberId)
        {
            var member = _db.MemberShipMasters.FirstOrDefault(m => m.MemberID == memberId);
            if (member == null)
            {
                return HttpNotFound();
            }

            var latestPayment = _db.MemberShipPaymentDetails
                .Where(p => p.MemberID == memberId)
                .OrderByDescending(p => p.Payment_Date)
                .FirstOrDefault();

            var plans = _db.MemberShipTypeMasters
                .OrderBy(t => t.MemberTypeDescription)
                .ToList();

            var model = new RenewalPopupViewModel
            {
                MemberID = member.MemberID,
                MemberName = member.Member_Name,
                MemberNumber = member.MemberNo,
                CurrentPlanName = latestPayment != null ? latestPayment.Payment_Plan : "",
                CurrentRenewalDate = latestPayment != null ? (DateTime?)latestPayment.Renewal_Date : null,
                Plans = plans.Select(p => new SelectListItem
                {
                    Value = p.MemberTypeId.ToString(),
                    Text = p.MemberTypeDescription
                }).ToList()
            };

            return PartialView("_RenewalPopup", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitRenewal(RenewalSubmitRequest request)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "Invalid data" });
            }

            var member = _db.MemberShipMasters.FirstOrDefault(m => m.MemberID == request.MemberID);
            var plan = _db.MemberShipTypeMasters.FirstOrDefault(p => p.MemberTypeId == request.MemberTypeId);
            if (member == null || plan == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "Member or Plan not found" });
            }

            // Calculate renewal dates based on plan
            var paymentDate = DateTime.Now;
            var renewalDate = paymentDate.AddYears(plan.NoOfYears);

            // Generate dummy receipt number
            var receiptNo = $"MR/{DateTime.Now:yy-yy}/{new Random().Next(100, 999)}";

            var newPayment = new MemberShipPaymentDetail
            {
                MemberID = member.MemberID,
                Payment_Date = paymentDate,
                Renewal_Date = renewalDate,
                Amount = plan.MembershipFee,
                MemberTypeId = plan.MemberTypeId,
                MemberTypeAmount = plan.MembershipFee,
                
                // Dummy data for other fields
                UPI_ID = "dummy@upi",
                RRN_NO = Guid.NewGuid().ToString().Substring(0, 12),
                Payment_Type = "Online",
                Payment_Status = "Success",
                Payment_Plan = plan.MemberTypeDescription,
                Payment_Receipt_No = receiptNo,
                ReceiptSerialNo = new Random().Next(1, 999),
                ReceiptDocumentNo = DateTime.Now.ToString("yy-yy"),
                CompanyAccountingDetailId = 1
            };

            _db.MemberShipPaymentDetails.Add(newPayment);
            
            // Update member subscription dates in MemberShipMaster table
            member.Member_Sdate = paymentDate; // Plan start date
            member.Member_Edate = renewalDate; // Plan end date
            member.MemberTypeId = plan.MemberTypeId; // Update member type
            
            try
            {
                _db.SaveChanges();
                return Json(new { 
                    success = true, 
                    message = "Renewal completed successfully", 
                    renewalDate = renewalDate.ToString("dd-MMM-yyyy"),
                    receiptNo = receiptNo,
                    planName = plan.MemberTypeDescription
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "Database error: " + ex.Message });
            }
        }

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var userName = User.Identity.GetUserName();
            bool isAdmin = User.IsInRole("Admin");
            if (isAdmin)
            {
                return RedirectToAction("AdminDashboard");
            }

            // Get user member information if authenticated
            // Most databases store AspNet Identity UserId (GUID) in MemberShipMaster.UIserID.
            // Try by userId first, then fall back to userName for older data.
            var member = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == userId);
            if (member == null)
            {
                member = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == userName);
            }
            // Finally, try matching by email if UIserID linkage isn't present
            if (member == null)
            {
                var appUser = _db.Users.FirstOrDefault(u => u.Id == userId);
                var email = appUser != null ? appUser.Email : null;
                if (!string.IsNullOrEmpty(email))
                {
                    member = _db.MemberShipMasters.FirstOrDefault(m => m.Member_EmailID == email);
                }
            }
            // Expose user info for header
            ViewBag.UserName = userName;
            ViewBag.MemberId = member != null ? (int?)member.MemberID : null;
            ViewBag.MemberDNo = member != null ? member.MemberDNo : null;
            // Resolve last login to show using Session/Cookie
            try
            {
                DateTime? lastLogin = null;
                // Prefer previous login (so current session shows the prior time)
                var prevObj = Session["PreviousLoginTime"];
                if (prevObj is DateTime)
                {
                    lastLogin = (DateTime)prevObj;
                }
                else
                {
                    var cookie = Request.Cookies["LastLoginAt"];
                    if (cookie != null)
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(cookie.Value, out parsed))
                        {
                            lastLogin = parsed;
                        }
                    }
                }
                ViewBag.LastLoginAt = lastLogin;
            }
            catch { ViewBag.LastLoginAt = null; }
            
            // Build a combined user feed from active content
            var feed = new UserFeedViewModel();

            // Materialize each group first to avoid mixed projection restrictions in EF
            // Filter announcements by user's CateTid
            int? userCateTid = null;
            try
            {
                var appUser = _db.Users.FirstOrDefault(u => u.Id == userId);
                userCateTid = appUser?.CateTid;
            }
            catch { }

            var annsRaw = _db.announcementmasters
                .Where(a => a.Status == 0 &&
                            (userCateTid.HasValue ? ("," + (a.CategoryTypeIds ?? "") + ",").Contains("," + userCateTid.Value + ",") : false))
                .Select(a => new
                {
                    a.AnnouncementId,
                    a.Heading,
                    a.Caption,
                    a.Description,
                    a.MainImage,
                    a.AdditionalImages,
                    a.CreatedDate
                })
                .ToList();

            var evsRaw = _db.eventmasters
                .Where(e => e.Status == 0 &&
                            (userCateTid.HasValue ? ("," + (e.CategoryTypeIds ?? "") + ",").Contains("," + userCateTid.Value + ",") : false))
                .Select(e => new
                {
                    e.EventId,
                    e.Heading,
                    e.Caption,
                    e.Description,
                    e.MainImage,
                    e.AdditionalImages,
                    e.EventTime,
                    e.EventLocation
                })
                .ToList();

            var galsRaw = _db.gallerymasters
                .Where(g => g.Status == 0 &&
                            (userCateTid.HasValue ? ("," + (g.CategoryTypeIds ?? "") + ",").Contains("," + userCateTid.Value + ",") : false))
                .Select(g => new
                {
                    g.GalleryId,
                    g.Heading,
                    g.Caption,
                    g.Description,
                    g.MainImage,
                    g.AdditionalImages,
                    g.CreatedDate
                })
                .ToList();

            // Minutes of Meeting filtered by user's category
            var minsRaw = _db.minutesofmeetings
                .Where(m => (userCateTid.HasValue ? ("," + (m.CategoryTypeIds ?? "") + ",").Contains("," + userCateTid.Value + ",") : false))
                .Select(m => new
                {
                    m.MomId,
                    m.Heading,
                    m.Caption,
                    m.Description,
                    m.MeetingDateAndTime,
                    m.MeetingPlace
                })
                .ToList();

            // If no category-matching minutes found, show latest few as a fallback
            if (!minsRaw.Any())
            {
                minsRaw = _db.minutesofmeetings
                    .OrderByDescending(m => m.MeetingDateAndTime)
                    .Take(5)
                    .Select(m => new
                    {
                        m.MomId,
                        m.Heading,
                        m.Caption,
                        m.Description,
                        m.MeetingDateAndTime,
                        m.MeetingPlace
                    })
                    .ToList();
            }

            var anns = annsRaw.Select(a => new UserFeedItem
            {
                Type = "Announcement",
                Id = a.AnnouncementId,
                Heading = a.Heading,
                Caption = a.Caption,
                Description = a.Description,
                MainImage = a.MainImage,
                Date = a.CreatedDate,
                Location = null,
                AdditionalImagesCsv = a.AdditionalImages,
                LikeCount = 0
            });

            var evs = evsRaw.Select(e => new UserFeedItem
            {
                Type = "Event",
                Id = e.EventId,
                Heading = e.Heading,
                Caption = e.Caption,
                Description = e.Description,
                MainImage = e.MainImage,
                Date = e.EventTime,
                Location = e.EventLocation,
                AdditionalImagesCsv = e.AdditionalImages,
                LikeCount = _db.eventinterests.Count(ei => ei.EventId == e.EventId && ei.State == 1)
            });

            var gals = galsRaw.Select(g => new UserFeedItem
            {
                Type = "Gallery",
                Id = g.GalleryId,
                Heading = g.Heading,
                Caption = g.Caption,
                Description = g.Description,
                MainImage = g.MainImage,
                Date = g.CreatedDate,
                Location = null,
                AdditionalImagesCsv = g.AdditionalImages,
                LikeCount = 0
            });

            var mins = minsRaw.Select(m => new UserFeedItem
            {
                Type = "Minutes",
                Id = m.MomId,
                Heading = m.Heading,
                Caption = m.Caption,
                Description = m.Description,
                MainImage = null,
                Date = m.MeetingDateAndTime ?? DateTime.Now,
                Location = m.MeetingPlace,
                AdditionalImagesCsv = null,
                LikeCount = 0
            });

            feed.Items = anns.Concat(evs).Concat(gals).Concat(mins)
                .OrderByDescending(i => i.Date)
                .Take(100)
                .ToList();

            // Compute like counts for events and pass to view
            var eventLikeCounts = _db.eventinterests
                .Where(ei => ei.State == 1)
                .GroupBy(ei => ei.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.EventId, x => x.Count);
            
            // Debug: Log the like counts
            System.Diagnostics.Debug.WriteLine("=== EVENT LIKE COUNTS ===");
            foreach (var kvp in eventLikeCounts)
            {
                System.Diagnostics.Debug.WriteLine($"EventId: {kvp.Key} (Type: {kvp.Key.GetType().Name}), Likes: {kvp.Value}");
            }
            System.Diagnostics.Debug.WriteLine("=== END LIKE COUNTS ===");
            
            ViewBag.EventLikeCounts = eventLikeCounts;

            // Add user-specific data to ViewBag
            if (member != null)
            {
                ViewBag.MemberName = member.Member_Name;
                ViewBag.MemberNumber = member.MemberNo;
                ViewBag.MemberSince = member.Member_Reg_Date;
                ViewBag.MemberEmail = member.Member_EmailID;
                ViewBag.MemberMobile = member.Member_Mobile_No;
                ViewBag.MemberCity = member.Member_City;
                ViewBag.IsActive = member.DispStatus == 1;
                
                // Get latest payment info
                var latestPayment = _db.MemberShipPaymentDetails
                    .Where(p => p.MemberID == member.MemberID)
                    .OrderByDescending(p => p.Payment_Date)
                    .FirstOrDefault();
                
                if (latestPayment != null)
                {
                    ViewBag.LastPaymentDate = latestPayment.Payment_Date;
                    ViewBag.NextRenewalDate = latestPayment.Renewal_Date;
                    ViewBag.PaymentPlan = latestPayment.Payment_Plan;
                    ViewBag.PaymentAmount = latestPayment.Amount;
                    
                    // Calculate days to renewal
                    var daysToRenewal = (latestPayment.Renewal_Date.Date - DateTime.Now.Date).TotalDays;
                    ViewBag.DaysToRenewal = (int)daysToRenewal;
                    ViewBag.IsRenewalDue = daysToRenewal <= 30;
                }
                
                // Get member statistics
                ViewBag.TotalPayments = _db.MemberShipPaymentDetails.Count(p => p.MemberID == member.MemberID);
                ViewBag.TotalAmountPaid = _db.MemberShipPaymentDetails
                    .Where(p => p.MemberID == member.MemberID && p.Payment_Status == "Success")
                    .Sum(p => (decimal?)p.Amount) ?? 0;
                
                // Get family members count
                ViewBag.FamilyMembersCount = _db.MemberShipFamilyDetails.Count(f => f.MemberID == member.MemberID);
            }

            return View(feed);
        }

        [HttpGet]
        public ActionResult Notifications()
        {
            var userId = User.Identity.GetUserId();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // Events the current user is interested in (State = 1) happening today
            var interestedEventIds = _db.eventinterests
                .Where(ei => ei.UserId == userId && ei.State == 1)
                .Select(ei => ei.EventId)
                .ToList();

            var todaysInterestedEvents = _db.eventmasters
                .Where(em => interestedEventIds.Contains(em.EventId)
                             && em.Status == 0
                             && em.EventTime >= today
                             && em.EventTime < tomorrow)
                .OrderBy(em => em.EventTime)
                .ToList();

            // Build sidebar data: today's birthdays and anniversaries
            try
            {
                int mm = today.Month;
                int dd = today.Day;

                // Active members only
                var activeMembers = _db.MemberShipMasters.Where(m => m.DispStatus == 1);

                // Member birthdays today (use entity to avoid dynamic binder issues)
                var birthdaysMembers = activeMembers
                    .Where(m => m.Member_DOB.Month == mm && m.Member_DOB.Day == dd)
                    .OrderBy(m => m.Member_Name)
                    .ToList();

                // Spouse birthdays today
                var birthdaysSpouses = activeMembers
                    .Where(m => m.Spouse_DOB.HasValue && m.Spouse_DOB.Value.Month == mm && m.Spouse_DOB.Value.Day == dd)
                    .Select(m => new SpouseBirthday
                    {
                        MemberID = m.MemberID,
                        Member_Name = m.Member_Name,
                        Spouse_Name = m.Spouse_Name
                    })
                    .OrderBy(m => m.Spouse_Name)
                    .ToList();

                // Anniversaries today
                var anniversaries = activeMembers
                    .Where(m => m.Date_Of_Marriage.HasValue && m.Date_Of_Marriage.Value.Month == mm && m.Date_Of_Marriage.Value.Day == dd)
                    .Select(m => new AnniversaryItem
                    {
                        MemberID = m.MemberID,
                        Member_Name = m.Member_Name,
                        Spouse_Name = m.Spouse_Name,
                        Date_Of_Marriage = m.Date_Of_Marriage.Value
                    })
                    .OrderBy(m => m.Member_Name)
                    .ToList();

                ViewBag.BirthdaysMembers = birthdaysMembers;
                ViewBag.BirthdaysSpouses = birthdaysSpouses;
                ViewBag.Anniversaries = anniversaries;
            }
            catch { /* best effort; sidebar is optional */ }

            return View(todaysInterestedEvents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AcceptNotification(int eventId)
        {
            var userId = User.Identity.GetUserId();
            var interest = _db.eventinterests.FirstOrDefault(e => e.EventId == eventId && e.UserId == userId);
            if (interest == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "Interest not found" });
            }

            // Keep/state as interested
            interest.State = 1;
            interest.ModifiedDate = DateTime.Now;
            _db.SaveChanges();

            var ev = _db.eventmasters.FirstOrDefault(e => e.EventId == eventId);
            if (ev == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "Event not found" });
            }

            // Build Google Calendar URL
            // Dates must be in UTC in format yyyyMMddTHHmmssZ
            var startUtc = ev.EventTime.ToUniversalTime();
            // Assume 1 hour duration if no explicit end
            var endUtc = startUtc.AddHours(1);
            string dates = startUtc.ToString("yyyyMMdd'T'HHmmss'Z'") + "/" + endUtc.ToString("yyyyMMdd'T'HHmmss'Z'");
            string title = Uri.EscapeDataString(ev.Heading ?? "Event");
            string details = Uri.EscapeDataString(string.IsNullOrWhiteSpace(ev.Description) ? (ev.Caption ?? "") : ev.Description);
            string location = Uri.EscapeDataString(ev.EventLocation ?? "");
            string calendarUrl = "https://calendar.google.com/calendar/render?action=TEMPLATE&trp=false&text=" + title + "&dates=" + dates + "&details=" + details + "&location=" + location;

            return Json(new { success = true, calendarUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeclineNotification(int eventId)
        {
            var userId = User.Identity.GetUserId();
            var interest = _db.eventinterests.FirstOrDefault(e => e.EventId == eventId && e.UserId == userId);
            if (interest == null)
            {
                Response.StatusCode = 404;
                return Json(new { success = false, message = "Interest not found" });
            }

            interest.State = -1;
            interest.ModifiedDate = DateTime.Now;
            _db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public ActionResult UserDashboard()
        {
            var userName = User.Identity.GetUserName();
            if (string.IsNullOrWhiteSpace(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == userName);
            if (member == null)
            {
                ViewBag.Message = "No member record found";
                // Return a view with an empty model instead of no model
                var emptyModel = new UserDashboardViewModel
                {
                    Member = null,
                    RecentPayments = new List<MemberShipPaymentDetail>(),
                    Children = new List<MemberShipFamilyDetail>(),
                    OrganizationDetails = new List<MemberShipODetail>(),
                    CurrentPlan = null,
                    NextRenewalDate = null,
                    DaysToRenewal = 0,
                    TotalPaid = 0,
                    TotalPayments = 0,
                    MemberSince = DateTime.Now,
                    IsActive = false
                };
                return View(emptyModel);
            }

            var latestPayment = _db.MemberShipPaymentDetails
                .Where(p => p.MemberID == member.MemberID)
                .OrderByDescending(p => p.Payment_Date)
                .FirstOrDefault();

            var recentPayments = _db.MemberShipPaymentDetails
                .Where(p => p.MemberID == member.MemberID)
                .OrderByDescending(p => p.Payment_Date)
                .Take(10)
                .ToList();

            var orgDetails = _db.MemberShipODetails
                .Where(o => o.MemberID == member.MemberID)
                .ToList();

            var children = _db.MemberShipFamilyDetails
                .Where(c => c.MemberID == member.MemberID)
                .ToList();

            var currentPlan = latestPayment != null
                ? _db.MemberShipTypeMasters.FirstOrDefault(t => t.MemberTypeId == latestPayment.MemberTypeId)
                : _db.MemberShipTypeMasters.FirstOrDefault(t => t.MemberTypeId == member.MemberTypeId);

            // Determine next renewal date
            // Prefer latest payment's renewal date; if not available, fall back to member's end date
            DateTime? nextRenewal = latestPayment != null
                ? (DateTime?)latestPayment.Renewal_Date
                : (member.Member_Edate != default(DateTime) ? (DateTime?)member.Member_Edate : null);

            // Calculate remaining days (never negative)
            int daysToRenewal = nextRenewal.HasValue ? (int)(nextRenewal.Value.Date - DateTime.Now.Date).TotalDays : 0;
            if (daysToRenewal < 0) daysToRenewal = 0;

            var totalPaid = _db.MemberShipPaymentDetails
                .Where(p => p.MemberID == member.MemberID && p.Payment_Status == "Success")
                .Select(p => (decimal?)p.Amount).Sum() ?? 0;

            var totalPayments = _db.MemberShipPaymentDetails.Count(p => p.MemberID == member.MemberID);

            var model = new UserDashboardViewModel
            {
                Member = member,
                LatestPayment = latestPayment,
                RecentPayments = recentPayments,
                Children = children,
                OrganizationDetails = orgDetails,
                CurrentPlan = currentPlan,
                NextRenewalDate = nextRenewal,
                DaysToRenewal = daysToRenewal,
                TotalPaid = totalPaid,
                TotalPayments = totalPayments,
                MemberSince = member.Member_Reg_Date,
                IsActive = member.DispStatus == 1
            };

            return View(model);
        }
    }
}