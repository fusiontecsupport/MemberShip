using ClubMembership.Models;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ClubMembership.Controllers
{
    [AllowAnonymous]
    public class RegisterController : Controller
    {
        private UserManager<ApplicationUser> UserManager { get; }
        private readonly ApplicationDbContext _db;
        public RegisterController()
            : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
            _db = new ApplicationDbContext();
        }

        public RegisterController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
            _db = new ApplicationDbContext();
        }

        [HttpGet]
        public ActionResult Index()
        {
            // Add membership types to ViewBag
            ViewBag.MemberTypes = new SelectList(
                _db.MemberShipTypeMasters.Where(m => m.DisplayStatus == 0).OrderBy(m => m.MembershipFee),
                "MemberTypeId",
                "MemberTypeDescription"
            );
            return View("Login_Register", new AccountViewModels.RegisterViewModel());
        }

        [HttpGet]
        public ActionResult TestGovernmentProof()
        {
            return View();
        }

        private string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SendOtp(string mobileNumber)
        {
            try
            {
                // Validate mobile number (10 digits)
                if (string.IsNullOrEmpty(mobileNumber) || !Regex.IsMatch(mobileNumber, @"^[0-9]{10}$"))
                {
                    return Json(new { success = false, message = "Invalid mobile number" });
                }

                // Check if number exists
                if (_db.Users.Any(u => u.MobileNo == mobileNumber) ||
                   _db.MemberShipMasters.Any(m => m.Member_Mobile_No == mobileNumber))
                {
                    return Json(new { success = false, message = "Mobile number already registered" });
                }

                // Generate OTP
                string otp = GenerateOTP();

                // Use the APPROVED template format
                string otpMessage = $"Welcome to SNGS Membership! Your OTP is {otp}. Valid for 2 minutes. Do not share this with anyone. Fusiontec Software";

                // Store OTP in session (2 minute expiry)
                HttpContext.Session[$"OTP_{mobileNumber}"] = otp;
                HttpContext.Session[$"OTP_Expiry_{mobileNumber}"] = DateTime.Now.AddMinutes(2).ToString("O");

                // Send SMS - Use CORRECT template ID
                Signremindermsg(mobileNumber, otpMessage);

                return Json(new { success = true, message = "OTP sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult VerifyOtp(string mobileNumber, string otp)
        {
            try
            {
                // Get stored OTP and expiry from session
                var storedOtp = HttpContext.Session[$"OTP_{mobileNumber}"] as string;
                var expiryString = HttpContext.Session[$"OTP_Expiry_{mobileNumber}"] as string;

                if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiryString))
                {
                    return Json(new { success = false, message = "OTP not found or expired" });
                }

                DateTime expiryTime;
                if (!DateTime.TryParse(expiryString, out expiryTime))
                {
                    return Json(new { success = false, message = "Invalid OTP session" });
                }

                if (DateTime.Now > expiryTime)
                {
                    // Clear expired OTP
                    HttpContext.Session.Remove($"OTP_{mobileNumber}");
                    HttpContext.Session.Remove($"OTP_Expiry_{mobileNumber}");
                    return Json(new { success = false, message = "OTP expired" });
                }

                if (storedOtp != otp)
                {
                    return Json(new { success = false, message = "Invalid OTP" });
                }

                // Mark as verified in session
                HttpContext.Session[$"OTP_Verified_{mobileNumber}"] = "true";

                return Json(new { success = true, message = "OTP verified successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public void Signremindermsg(string mobileNumber, string message)
        {
            try
            {
                // Use TLS 1.2
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Get credentials from config
                var smsuserid = ConfigurationManager.AppSettings["SMSUserID"]; // mudhra
                var smspwd = ConfigurationManager.AppSettings["SMSPWD"]; // mudhra123
                var senderid = ConfigurationManager.AppSettings["SMSSenderID"]; // FUSNTC

                // Use the APPROVED template ID
                var tempid = "1607100000000354440"; // OTP_SMS01 template

                // URL encode all parameters
                string encodedMessage = Uri.EscapeDataString(message);
                string encodedTime = Uri.EscapeDataString(DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"));

                // Construct API URL
                string apiUrl = $"http://www.smsintegra.com/api/smsapi.aspx?" +
                              $"uid={smsuserid}&" +
                              $"pwd={smspwd}&" +
                              $"mobile=91{mobileNumber}&" + // Add 91 prefix for India
                              $"msg={encodedMessage}&" +
                              $"sid={senderid}&" +
                              $"type=0&" + // 0 for text
                              $"dtTimeNow={encodedTime}&" +
                              $"entityid=&" + // Leave empty
                              $"tempid={tempid}&" +
                              $"dnd=1"; // Bypass DND for transactional SMS

                // Debug output
                System.Diagnostics.Debug.WriteLine("SMS API URL: " + apiUrl);

                // Send request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string responseText = reader.ReadToEnd();
                    System.Diagnostics.Debug.WriteLine("SMS Response: " + responseText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SMS Error: " + ex.ToString());
            }
        }

        [AllowAnonymous]
        public JsonResult GetMembershipDetails(int id)
        {
            try
            {
                var details = _db.MemberShipTypeMasters
                                .Where(m => m.MemberTypeId == id)
                                .Select(m => new {
                                    fee = m.MembershipFee,
                                    years = m.NoOfYears
                                })
                                .FirstOrDefault();

                if (details == null)
                {
                    return Json(new { success = false, message = "Membership type not found" }, JsonRequestBehavior.AllowGet);
                }

                // Debugging - check the values
                System.Diagnostics.Debug.WriteLine($"Fee: {details.fee}, Years: {details.years}");

                return Json(new
                {
                    success = true,
                    fee = details.fee,
                    years = details.years
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Index(AccountViewModels.RegisterViewModel model, int SelectedMemberTypeId, string otp)
        {
            // Verify OTP from session
            var isOtpVerified = HttpContext.Session[$"OTP_Verified_{model.MobileNo}"] as string == "true";

            if (!isOtpVerified)
            {
                ModelState.AddModelError("", "Mobile number not verified with OTP");
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
            }

            if (!string.IsNullOrEmpty(model.MobileNo) && !System.Text.RegularExpressions.Regex.IsMatch(model.MobileNo, @"^[0-9]{10}$"))
            {
                ModelState.AddModelError("MobileNo", "Please enter a valid 10-digit mobile number");
            }

            if (model.DOB > DateTime.Now)
            {
                ModelState.AddModelError("DOB", "Date of birth cannot be in the future");
            }

            // Handle Government Proof file upload (optional)
            try
            {
                if (model.GovernmentProofFile != null && model.GovernmentProofFile.ContentLength > 0)
                {
                    var allowedExt = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.GovernmentProofFile.FileName).ToLowerInvariant();
                    if (!allowedExt.Contains(ext))
                    {
                        ModelState.AddModelError("GovernmentProofFile", "Please submit the correct proof (PDF, JPG, JPEG, PNG).");
                    }
                    else
                    {
                        var uploadRoot = Server.MapPath("~/Uploads/GovProofs");
                        if (!Directory.Exists(uploadRoot)) Directory.CreateDirectory(uploadRoot);

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var fullPath = Path.Combine(uploadRoot, fileName);
                        model.GovernmentProofFile.SaveAs(fullPath);

                        // Store relative path for serving via the app
                        model.GovernmentProofPath = Url.Content($"~/Uploads/GovProofs/{fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("GovernmentProofFile", $"File upload failed: {ex.Message}");
            }

            // Unique Username, Email, and MobileNo validation
            using (var db = new ApplicationDbContext())
            {
                if (db.Users.Any(u => u.UserName == model.UserName))
                {
                    ModelState.AddModelError("UserName", "Username already exists!");
                }
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email Id already exists!");
                }
                if (db.Users.Any(u => u.MobileNo == model.MobileNo))
                {
                    ModelState.AddModelError("MobileNo", "Mobile number already exists!");
                }
            }

            if (ModelState.IsValid)
            {
                var user = model.GetUser();
                user.NPassword = model.Password;

                // Set CateTid to 3 as requested
                user.CateTid = 3;

                // Pre-create MemberShipMaster to get MemberID (AspNetUsers.MemberID is NOT NULL)
                using (var db = new ApplicationDbContext())
                {
                    // Get selected membership type with No_Of_Years
                    var memberType = db.MemberShipTypeMasters
                        .Where(m => m.MemberTypeId == SelectedMemberTypeId)
                        .Select(m => new {
                            m.MemberTypeId,
                            m.MembershipFee,
                            m.NoOfYears,
                            m.MemberTypeDescription
                        })
                        .FirstOrDefault();

                    if (memberType == null)
                    {
                        ModelState.AddModelError("", "Invalid membership type selected");
                        // Repopulate ViewBag and return
                        ViewBag.MemberTypes = new SelectList(
                            _db.MemberShipTypeMasters.Where(m => m.DisplayStatus == 0).OrderBy(m => m.MembershipFee),
                            "MemberTypeId",
                            "MemberTypeDescription",
                            SelectedMemberTypeId
                        );
                        return View("Login_Register", model);
                    }

                    // Generate next MemberNo
                    int memberNo = db.MemberShipMasters.Max(m => (int?)m.MemberNo) + 1 ?? 1000;
                    string registerYear = DateTime.Now.Year.ToString();
                    string dobYear = model.DOB.Year.ToString();
                    string memberNoStr = memberNo.ToString().PadLeft(3, '0');
                    string memberDNo = $"{registerYear}{dobYear}{memberNoStr}";

                    // Calculate age
                    int age = DateTime.Today.Year - model.DOB.Year;
                    if (model.DOB.Date > DateTime.Today.AddYears(-age)) age--;

                    var renewalDate = DateTime.Now.AddYears(memberType.NoOfYears);

                    // Get the category description for CateTid = 3
                    string cateDesc = "";
                    try
                    {
                        using (var clubDb = new ClubMembership.Data.ClubMembershipDBEntities())
                        {
                            var categoryType = clubDb.Database.SqlQuery<string>(
                                "SELECT CateTDesc FROM CategoryTypeMaster WHERE CateTid = 3"
                            ).FirstOrDefault();
                            cateDesc = categoryType ?? "Default Category";
                        }
                    }
                    catch
                    {
                        cateDesc = "Default Category";
                    }

                    // Create the member from the registration model (not from created user yet)
                    var preMember = new MemberShipMaster
                    {
                        UIserID = model.UserName,
                        RegstrId = "1",
                        MemberNo = memberNo,
                        MemberDNo = memberDNo,
                        Member_Reg_Date = DateTime.Now,
                        Member_Name = $"{model.FirstName} {model.LastName}",
                        Gender = model.Gender == "Male" ? 1 : 2,
                        Member_DOB = model.DOB,
                        Member_Age = age.ToString(),
                        BldGID = 1,
                        Member_Mobile_No = model.MobileNo,
                        Member_EmailID = model.Email,
                        Member_Addr1 = "Address1",
                        StateID = 1,
                        Member_Pincode = "000000",
                        Member_Country = "India",
                        Member_Per_Addr1 = "Address1",
                        PStateID = 1,
                        Member_Per_Pincode = "000000",
                        Member_Per_Country = "India",
                        CreatedBy = model.UserName,
                        CreatedDateTime = DateTime.Now,
                        DispStatus = 1,
                        Member_Sdate = DateTime.Now,
                        Member_Edate = renewalDate,
                        Reference_By = "Self",
                        Reference_Contact_No = model.MobileNo,
                        UserName = model.UserName,
                        // PasswordHash will be updated after user creation
                        NPassword = model.Password,
                        Member_Nativity = "Kerala",
                        MemberTypeId = memberType.MemberTypeId,
                        MemberTypeAmount = memberType.MembershipFee,
                        CateTDesc = cateDesc
                    };

                    db.MemberShipMasters.Add(preMember);
                    db.SaveChanges();

                    // Assign the generated MemberID to the user before creating identity record
                    user.MemberID = preMember.MemberID;
                }

                var result = await UserManager.CreateAsync(user, model.Password);
                HttpContext.Session.Remove($"OTP_{model.MobileNo}");
                HttpContext.Session.Remove($"OTP_Expiry_{model.MobileNo}");
                HttpContext.Session.Remove($"OTP_Verified_{model.MobileNo}");
                if (result.Succeeded)
                {
                    // Assign user to default group (GroupId = 4) using stored procedure with correct parameter names
                    var createdUser = await UserManager.FindByNameAsync(user.UserName);
                    if (createdUser != null)
                    {
                        using (var db = new ApplicationDbContext())
                        {
                            // Get selected membership type again for payment details
                            var memberType = db.MemberShipTypeMasters
                                .Where(m => m.MemberTypeId == SelectedMemberTypeId)
                                .Select(m => new {
                                    m.MemberTypeId,
                                    m.MembershipFee,
                                    m.NoOfYears,
                                    m.MemberTypeDescription
                                })
                                .FirstOrDefault();

                            db.Database.ExecuteSqlCommand(
                                "EXEC pr_Auto_User_Groups_Roles_Assgn @PUserId = {0}, @PGroupId = {1}",
                                createdUser.Id, 4
                            );

                            // Update the pre-created member with the identity PasswordHash
                            if (createdUser.MemberID.HasValue)
                            {
                                var member = db.MemberShipMasters.FirstOrDefault(m => m.MemberID == createdUser.MemberID.Value);
                                if (member != null)
                                {
                                    member.PasswordHash = createdUser.PasswordHash;
                                    member.UserName = createdUser.UserName;
                                    member.NPassword = createdUser.NPassword;
                                    db.SaveChanges();
                                }
                            }

                            // After saving member: compute compy_id, receipt serial, and FY string
                            var compyObj = Session["compyid"]; // set during Account login
                            int? compyId = compyObj != null ? (int?)Convert.ToInt32(compyObj) : null;
                            if (!compyId.HasValue)
                            {
                                var tmpDate = DateTime.Now.Date;
                                int LMNTH = tmpDate.Month;
                                int LYR = tmpDate.Year;
                                int PFYear = (LMNTH >= 4) ? LYR : LYR - 1;
                                int PTYear = PFYear + 1;
                                string PFDATE = $"01/04/{PFYear}";
                                string PTDATE = $"31/03/{PTYear}";
                                string GYrDesc = PFYear + " - " + PTYear;

                                var yrIds = db.Database.SqlQuery<int>("SELECT YRID FROM AccountingYear WHERE YrDesc = '" + GYrDesc + "'").ToList();
                                int yrId;
                                if (yrIds.Count == 0)
                                {
                                    db.Database.ExecuteSqlCommand("INSERT INTO AccountingYear (YrDesc, FDate, TDate, CUSRID, PRCSDATE) VALUES ('" + GYrDesc + "', '" + Convert.ToDateTime(PFDATE).ToString("MM/dd/yyyy") + "', '" + Convert.ToDateTime(PTDATE).ToString("MM/dd/yyyy") + "', '" + (Session["CUSRID"] ?? "system") + "', '" + DateTime.Now.ToString("MM-dd-yyyy") + "')");
                                    yrId = db.Database.SqlQuery<int>("SELECT MAX(YRID) FROM AccountingYear").FirstOrDefault();
                                }
                                else
                                {
                                    yrId = yrIds[yrIds.Count - 1];
                                }

                                int compId = 0;
                                int.TryParse(Convert.ToString(Session["COMPID"] ?? "0"), out compId);
                                if (compId == 0)
                                {
                                    var compList = db.Database.SqlQuery<int>("SELECT TOP 1 COMPID FROM companymaster ORDER BY COMPID").ToList();
                                    if (compList.Count > 0) compId = compList[0];
                                }

                                var compyList = db.Database.SqlQuery<int>("SELECT COMPYID FROM CompanyAccountingDetail WHERE CompId = " + compId + " AND YrId = " + yrId).ToList();
                                if (compyList.Count == 0)
                                {
                                    db.Database.ExecuteSqlCommand("INSERT INTO CompanyAccountingDetail (CompId, YrId, CUSRID, PRCSDATE) VALUES (" + compId + ", " + yrId + ", '" + (Session["CUSRID"] ?? "system") + "', '" + DateTime.Now.ToString("MM-dd-yyyy") + "')");
                                    compyId = db.Database.SqlQuery<int>("SELECT MAX(COMPYID) FROM CompanyAccountingDetail").FirstOrDefault();
                                }
                                else
                                {
                                    compyId = compyList[0];
                                }
                                Session["compyid"] = compyId;
                            }
                            var now = DateTime.Now;
                            int startYear = now.Month >= 4 ? now.Year : now.Year - 1;
                            int endYear = startYear + 1;
                            string fy = $"{startYear % 100:00}-{endYear % 100:00}";
                            int nextSerial = 1;
                            if (compyId.HasValue)
                            {
                                var maxSerial = db.MemberShipPaymentDetails
                                    .Where(p => p.CompanyAccountingDetailId == compyId)
                                    .Max(p => (int?)p.ReceiptSerialNo);
                                nextSerial = (maxSerial ?? 0) + 1;
                            }
                            var receiptNo = string.Format("MR/{0}/{1}", fy, nextSerial.ToString("D3"));

                            // Ensure we have the correct MemberID and renewal date in this scope
                            int memberId = createdUser.MemberID ??
                                (db.MemberShipMasters.Where(m => m.UserName == createdUser.UserName)
                                                     .Select(m => m.MemberID)
                                                     .FirstOrDefault());
                            var renewalDate = DateTime.Now.AddYears(memberType.NoOfYears);

                            var payment = new MemberShipPaymentDetail
                            {
                                MemberID = memberId,
                                MemberTypeId = memberType.MemberTypeId,
                                MemberTypeAmount = memberType.MembershipFee,
                                Payment_Date = DateTime.Now,
                                Renewal_Date = renewalDate,
                                UPI_ID = "NA", // Default or from form
                                RRN_NO = "NA", // Default or from form
                                Amount = memberType.MembershipFee, // Default or from form
                                Payment_Type = "Online", // Default or from form
                                Payment_Status = "Success", // Default or from form
                                Payment_Plan = "Standard", // Default or from form
                                Payment_Receipt_No = receiptNo,
                                ReceiptSerialNo = nextSerial,
                                ReceiptDocumentNo = fy,
                                CompanyAccountingDetailId = compyId
                            };
                            db.MemberShipPaymentDetails.Add(payment);
                            db.SaveChanges();

                            // Save government proof to govrmnet_proof table if file was uploaded
                            if (!string.IsNullOrEmpty(model.GovernmentProofPath) && createdUser.MemberID.HasValue)
                            {
                                var governmentProof = new GovernmentProof
                                {
                                    MemberID = createdUser.MemberID.Value,
                                    GovPath = model.GovernmentProofPath
                                };
                                db.GovernmentProofs.Add(governmentProof);
                                db.SaveChanges();
                            }
                        }
                    }

                    TempData["RegistrationSuccess"] = "Your account has been created successfully. You can now log in.";
                    return RedirectToAction("Login", "Account");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }
            // Repopulate ViewBag if validation fails
            ViewBag.MemberTypes = new SelectList(
                _db.MemberShipTypeMasters.Where(m => m.DisplayStatus == 0).OrderBy(m => m.MembershipFee),
                "MemberTypeId",
                "MemberTypeDescription",
                SelectedMemberTypeId
            );

            return View("Login_Register", model);
        }
    }
}