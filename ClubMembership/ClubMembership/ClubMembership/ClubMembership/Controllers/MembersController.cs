using ClubMembership.Models;
using log4net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Web;
using System.Web.Mvc;
using static ClubMembership.Models.AccountViewModels;

namespace ClubMembership.Controllers
{
    public class MembersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private static readonly ILog log = LogManager.GetLogger(typeof(MembersController));
        public MembersController()
        {
            _db = new ApplicationDbContext();
        }

        // GET: Members
        public ActionResult Index(string q, int? page, int? pageSize)
        {
            int[] allowedSizes = new[] { 10, 20, 50, 100 };
            int size = pageSize.HasValue && allowedSizes.Contains(pageSize.Value) ? pageSize.Value : 10;
            int pageNumber = (page ?? 1);

            var query = _db.MemberShipMasters.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string search = q.Trim().ToLower();

                int? genderFilter = null;
                if (search == "male" || search == "1") genderFilter = 1;
                else if (search == "female" || search == "2") genderFilter = 2;

                query = query.Where(m =>
                    (m.Member_Name != null && m.Member_Name.ToLower().Contains(search)) ||
                    (m.Member_Mobile_No != null && m.Member_Mobile_No.ToLower().Contains(search)) ||
                    (m.Member_WhatsApp_No != null && m.Member_WhatsApp_No.ToLower().Contains(search)) ||
                    (m.Member_EmailID != null && m.Member_EmailID.ToLower().Contains(search)) ||
                    (m.Member_Area != null && m.Member_Area.ToLower().Contains(search)) ||
                    (genderFilter.HasValue && m.Gender == genderFilter.Value)
                );
            }

            var members = query.OrderBy(m => m.Member_Name).ToPagedList(pageNumber, size);
            ViewBag.CurrentFilter = q;
            ViewBag.PageSize = size;
            return View(members);
        }

        private void PopulateViewBags()
        {
            var bloodGroups = _db.BloodGroupMasters
                                .Where(b => b.DISPSTATUS == 0)
                                .OrderBy(b => b.BLDGDESC)
                                .ToList();
            ViewBag.BloodGroups = new SelectList(bloodGroups, "BldGID", "BLDGDESC");

            var states = _db.StateMasters
                          .Where(s => s.DISPSTATUS == 0)
                          .OrderBy(s => s.STATEDESC)
                          .ToList();
            ViewBag.States = new SelectList(states, "STATEID", "STATEDESC");

            ViewBag.MemberTypes = new SelectList(
                _db.MemberShipTypeMasters.Where(m => m.DisplayStatus == 0).OrderBy(m => m.MembershipFee),
                "MemberTypeId",
                "MemberTypeDescription"
            );

            ViewBag.ChildGenders = new SelectList(new[]
            {
                new { Value = 1, Text = "Male" },
                new { Value = 2, Text = "Female" }
            }, "Value", "Text");

            ViewBag.ChildPositions = new SelectList(new[]
            {
                new { Value = 1, Text = "Student" },
                new { Value = 2, Text = "Graduated" },
                new { Value = 3, Text = "Employee" },
                new { Value = 4, Text = "Business" },
                new { Value = 5, Text = "Others" }
            }, "Value", "Text");
            ViewBag.ChildMaritalStatuses = new SelectList(new[]
            {
                new { Value = 1, Text = "Single" },
                new { Value = 2, Text = "Married" }
            }, "Value", "Text");

            // Populate Category Types from CategoryTypeMaster for CateTDesc dropdown
            try
            {
                using (var clubDb = new ClubMembership.Data.ClubMembershipDBEntities())
                {
                    var categories = clubDb.Database.SqlQuery<string>(
                        "SELECT CateTDesc FROM CategoryTypeMaster WHERE ISNULL(Dispstatus, 0) = 0 ORDER BY CateTDesc"
                    ).ToList();

                    ViewBag.CategoryTypes = new SelectList(
                        categories.Select(c => new { Value = c, Text = c }),
                        "Value",
                        "Text"
                    );
                }
            }
            catch
            {
                ViewBag.CategoryTypes = new SelectList(new[] { new { Value = "", Text = "-- No Categories --" } }, "Value", "Text");
            }
        }

        // GET: Members/Create
        public ActionResult Create()
        {
            PopulateViewBags();
            var viewModel = new MemberProfileViewModel
            {
                Member = new MemberShipMaster
                {
                    // Initialize with default values if needed
                    Member_Reg_Date = DateTime.Now,
                    Member_Sdate = DateTime.Now,
                    Member_Edate = DateTime.Now.AddYears(1),
                    DispStatus = 1 // Assuming 1 means active
                },
                Children = new List<MemberShipFamilyDetail>(),
                OrganizationDetails = new List<MemberShipODetail>()
            };
            PopulateViewBags();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MemberProfileViewModel viewModel, HttpPostedFileBase Photo, HttpPostedFileBase FamilyPhoto, string MaritalStatus, int SelectedMemberTypeId)
        {
            try
            {
                log.Info("Starting Create action");
                PopulateViewBags();

                // Get ConfirmPassword from the form
                string confirmPassword = Request.Form["ConfirmPassword"];

                // Validate password and confirm password match
                if (viewModel.Member.NPassword != confirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Password and Confirm Password do not match.");
                }

                // Hash ConfirmPassword and save to PasswordHash
                if (!string.IsNullOrEmpty(confirmPassword))
                {
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(confirmPassword);
                        var hash = sha.ComputeHash(bytes);
                        viewModel.Member.PasswordHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    }
                }

                // Check for duplicate username, email, and mobile in MemberShipMaster
                using (var db = new ApplicationDbContext())
                {
                    if (db.MemberShipMasters.Any(m => m.UserName == viewModel.Member.UserName))
                        ModelState.AddModelError("Member.UserName", "Username already exists in members!");
                    if (db.MemberShipMasters.Any(m => m.Member_EmailID == viewModel.Member.Member_EmailID))
                        ModelState.AddModelError("Member.Member_EmailID", "Email already exists in members!");
                    if (db.MemberShipMasters.Any(m => m.Member_Mobile_No == viewModel.Member.Member_Mobile_No))
                        ModelState.AddModelError("Member.Member_Mobile_No", "Mobile number already exists in members!");

                    // Validate against Identity tables
                    if (_db.Users.Any(u => u.UserName == viewModel.Member.UserName))
                        ModelState.AddModelError("Member.UserName", "Username already exists in users!");
                    if (_db.Users.Any(u => u.Email == viewModel.Member.Member_EmailID))
                        ModelState.AddModelError("Member.Member_EmailID", "Email already exists in users!");
                    if (_db.Users.Any(u => u.MobileNo == viewModel.Member.Member_Mobile_No))
                        ModelState.AddModelError("Member.Member_Mobile_No", "Mobile number already exists in users!");
                }

                // Log incoming data
                log.Debug($"ModelState.IsValid: {ModelState.IsValid}");
                log.Debug($"Member data received: {JsonConvert.SerializeObject(viewModel.Member)}");
                log.Debug($"Children count: {(viewModel.Children != null ? viewModel.Children.Count : 0)}");
                log.Debug($"OrganizationDetails count: {(viewModel.OrganizationDetails != null ? viewModel.OrganizationDetails.Count : 0)}");
                log.Debug($"MaritalStatus: {MaritalStatus}");

                // Set additional member properties
                viewModel.Member.RegstrId = "1"; // Default value
                viewModel.Member.MemberNo = GenerateMemberNumber();

                // Set MemberDNo as registeryear + dob year + "000" + MemberNo
                var registerYear = DateTime.Now.Year.ToString();
                var dobYear = viewModel.Member.Member_DOB.Year.ToString();
                var memberNoStr = viewModel.Member.MemberNo.ToString().PadLeft(3, '0');
                viewModel.Member.MemberDNo = $"{registerYear}{dobYear}{memberNoStr}";

                viewModel.Member.DispStatus = 1;
                viewModel.Member.CreatedBy = string.IsNullOrWhiteSpace(User.Identity?.Name) ? "admin" : User.Identity.Name;
                viewModel.Member.CreatedDateTime = DateTime.Now;
                // Validate DOB range before calculating age
                if (viewModel.Member.Member_DOB == default(DateTime)
                    || viewModel.Member.Member_DOB.Year < 1900
                    || viewModel.Member.Member_DOB.Date > DateTime.Today)
                {
                    ModelState.AddModelError("Member.Member_DOB", "Enter a valid date of birth.");
                }
                else
                {
                    viewModel.Member.Member_Age = CalculateAge(viewModel.Member.Member_DOB).ToString();
                }
                viewModel.Member.Member_Reg_Date = DateTime.Now;
                
                // Get subscription start date from form, default to current date if not provided
                DateTime subscriptionStartDate = DateTime.Now;
                if (Request.Form["SubscriptionStartDate"] != null && !string.IsNullOrEmpty(Request.Form["SubscriptionStartDate"]))
                {
                    if (DateTime.TryParse(Request.Form["SubscriptionStartDate"], out DateTime parsedDate))
                    {
                        subscriptionStartDate = parsedDate;
                    }
                }
                
                viewModel.Member.Member_Sdate = subscriptionStartDate;
                viewModel.Member.Member_Edate = subscriptionStartDate.AddYears(1);
                viewModel.Member.UIserID = viewModel.Member.UserName;

                // Calculate age from DOB before validation
                if (viewModel.Member.Member_DOB.Year > 1900) // Check for valid date
                {
                    viewModel.Member.Member_Age = CalculateAge(viewModel.Member.Member_DOB).ToString();
                    ModelState.Remove("Member.Member_Age"); // Remove validation error
                }

                // Handle marital status
                if (MaritalStatus != "Married")
                {
                    viewModel.Member.Spouse_Name = null;
                    viewModel.Member.Spouse_DOB = null;
                    viewModel.Member.Date_Of_Marriage = null;
                    viewModel.Member.Total_Children = 0;
                    viewModel.Children = new List<MemberShipFamilyDetail>(); // Clear children if not married
                }
                else
                {
                    // Ensure Total_Children has a valid value when married
                    viewModel.Member.Total_Children = viewModel.Member.Total_Children ?? 0; // Use 0 if null
                    if (viewModel.Member.Total_Children < 0)
                    {
                        viewModel.Member.Total_Children = 0; // Prevent negative values
                    }
                }

                // Remove ModelState errors for fields we're setting manually
                ModelState.Remove("Member.Member_Age");
                ModelState.Remove("Member.MemberNo");
                ModelState.Remove("Member.UIserID");
                ModelState.Remove("Member.MemberDNo");
                ModelState.Remove("Member.CreatedBy");
                ModelState.Remove("Member.RegstrId");
                ModelState.Remove("Member.MemberNo");
                ModelState.Remove("Member.MaritalStatus");
                ModelState.Remove("Member.Total_Children");
                ModelState.Remove("Member.Spouse_Name");
                ModelState.Remove("Member.Spouse_DOB");
                ModelState.Remove("Member.Date_Of_Marriage");

                if (ModelState.IsValid)
                {
                    // Get membership type details
                    var memberType = _db.MemberShipTypeMasters
                                      .FirstOrDefault(m => m.MemberTypeId == SelectedMemberTypeId);

                    if (memberType == null)
                    {
                        ModelState.AddModelError("", "Invalid membership type selected");
                        PopulateViewBags();
                        return View(viewModel);
                    }

                    // Set membership type info in MemberShipMaster
                    viewModel.Member.MemberTypeId = memberType.MemberTypeId;
                    viewModel.Member.MemberTypeAmount = memberType.MembershipFee;
                    
                    // Do not override CateTDesc set by the form; ensure it's not null/empty
                    if (string.IsNullOrWhiteSpace(viewModel.Member.CateTDesc))
                    {
                        viewModel.Member.CateTDesc = null;
                    }
                    
                    // Calculate expiry date based on selected start date and membership type duration
                    DateTime calculatedSubscriptionStartDate = viewModel.Member.Member_Sdate;
                    viewModel.Member.Member_Edate = calculatedSubscriptionStartDate.AddYears(memberType.NoOfYears);

                    // Ensure the model is properly initialized before returning
                    if (viewModel.Member == null)
                    {
                        viewModel.Member = new MemberShipMaster();
                    }
                    if (viewModel.Children == null)
                    {
                        viewModel.Children = new List<MemberShipFamilyDetail>();
                    }
                    if (viewModel.OrganizationDetails == null)
                    {
                        viewModel.OrganizationDetails = new List<MemberShipODetail>();
                    }

                    log.Info("ModelState is valid - processing data");

                    using (var transaction = _db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Handle photo upload
                            if (Photo != null && Photo.ContentLength > 0)
                            {
                                log.Debug("Processing member photo upload");
                                string uploadsFolder = Server.MapPath("~/Uploads/MemberPhotos");
                                Directory.CreateDirectory(uploadsFolder);
                                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                Photo.SaveAs(filePath);
                                viewModel.Member.Member_Photo_Path = $"/Uploads/MemberPhotos/{uniqueFileName}";
                                log.Debug($"Saved member photo to: {filePath}");
                            }

                            // Handle family photo upload
                            if (FamilyPhoto != null && FamilyPhoto.ContentLength > 0)
                            {
                                log.Debug("Processing family photo upload");
                                string uploadsFolder = Server.MapPath("~/Uploads/FamilyPhotos");
                                Directory.CreateDirectory(uploadsFolder);
                                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(FamilyPhoto.FileName)}";
                                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                FamilyPhoto.SaveAs(filePath);
                                viewModel.Member.Family_Photo_Path = $"/Uploads/FamilyPhotos/{uniqueFileName}";
                                log.Debug($"Saved family photo to: {filePath}");
                            }

                            // Save member
                            _db.MemberShipMasters.Add(viewModel.Member);
                            log.Debug("Added member to context");
                            _db.SaveChanges();
                            log.Info($"Successfully saved member with ID: {viewModel.Member.MemberID}");

                            // After saving MemberShipMaster (viewModel.Member)
                            var registerViewModel = new RegisterViewModel
                            {
                                UserName = viewModel.Member.UserName,
                                Password = viewModel.Member.NPassword, // Or the hashed password if needed
                                ConfirmPassword = confirmPassword,
                                FirstName = viewModel.Member.Member_Name,
                                LastName = viewModel.Member.Member_Name, // Adjust property names as per your model
                                Email = viewModel.Member.Member_EmailID,
                                NPassword = viewModel.Member.NPassword,
                                MobileNo = viewModel.Member.Member_Mobile_No,
                                DOB = viewModel.Member.Member_DOB,
                                Gender = viewModel.Member.Gender == 1 ? "Male" : "Female"
                            };

                            var user = registerViewModel.GetUser();

                            // Use UserManager to create the user and set password hash/security stamp
                            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));

                            // Derive CateTid from the selected CateTDesc (if any)
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(viewModel.Member.CateTDesc))
                                {
                                    using (var clubDb = new ClubMembership.Data.ClubMembershipDBEntities())
                                    {
                                        var cateTid = clubDb.Database.SqlQuery<int?>(
                                            "SELECT TOP 1 CateTid FROM CategoryTypeMaster WHERE CateTDesc = @p0",
                                            viewModel.Member.CateTDesc
                                        ).FirstOrDefault();
                                        user.CateTid = cateTid;
                                    }
                                }
                            }
                            catch
                            {
                                // If lookup fails, leave CateTid as null
                                user.CateTid = null;
                            }

                            // Ensure MemberID is set on the identity user (AspNetUsers.MemberID is NOT NULL in DB)
                            user.MemberID = viewModel.Member.MemberID;

                            var result = userManager.Create(user, registerViewModel.Password);

                            if (!result.Succeeded)
                            {
                                ModelState.AddModelError("", string.Join(", ", result.Errors));
                                // Optionally: rollback transaction or return view with errors
                                transaction.Rollback();
                                PopulateViewBags();
                                return View(viewModel);
                            }

                            // At this point, the user is created and you can get the user Id
                            var createdUser = userManager.FindByName(user.UserName);
                            if (createdUser != null)
                            {
                                using (var db = new ApplicationDbContext())
                                {
                                    db.Database.ExecuteSqlCommand(
                                        "EXEC pr_Auto_User_Groups_Roles_Assgn @PUserId = {0}, @PGroupId = {1}",
                                        createdUser.Id, 4
                                    );
                                }
                            }

                            // Compute compy_id, receipt serial (per company), and financial year string (YY-YY)
                            var compyObj = Session["compyid"]; // set during login
                            int? compyId = compyObj != null ? (int?)Convert.ToInt32(compyObj) : null;
                            if (!compyId.HasValue)
                            {
                                // Derive compy_id using AccountingYear and CompanyAccountingDetail if session is missing
                                var tmpDate = DateTime.Now.Date;
                                int LMNTH = tmpDate.Month;
                                int LYR = tmpDate.Year;
                                int PFYear = (LMNTH >= 4) ? LYR : LYR - 1;
                                int PTYear = PFYear + 1;
                                string PFDATE = $"01/04/{PFYear}";
                                string PTDATE = $"31/03/{PTYear}";
                                string GYrDesc = PFYear + " - " + PTYear;

                                // Ensure AccountingYear row exists and get YRID
                                var yrIds = _db.Database.SqlQuery<int>("SELECT YRID FROM AccountingYear WHERE YrDesc = '" + GYrDesc + "'").ToList();
                                int yrId;
                                if (yrIds.Count == 0)
                                {
                                    _db.Database.ExecuteSqlCommand("INSERT INTO AccountingYear (YrDesc, FDate, TDate, CUSRID, PRCSDATE) VALUES ('" + GYrDesc + "', '" + Convert.ToDateTime(PFDATE).ToString("MM/dd/yyyy") + "', '" + Convert.ToDateTime(PTDATE).ToString("MM/dd/yyyy") + "', '" + (Session["CUSRID"] ?? "system") + "', '" + DateTime.Now.ToString("MM-dd-yyyy") + "')");
                                    yrId = _db.Database.SqlQuery<int>("SELECT MAX(YRID) FROM AccountingYear").FirstOrDefault();
                                }
                                else
                                {
                                    yrId = yrIds[yrIds.Count - 1];
                                }

                                // Determine CompId from session or default
                                int compId = 0;
                                int.TryParse(Convert.ToString(Session["COMPID"] ?? "0"), out compId);
                                if (compId == 0)
                                {
                                    var compList = _db.Database.SqlQuery<int>("SELECT TOP 1 COMPID FROM companymaster ORDER BY COMPID").ToList();
                                    if (compList.Count > 0) compId = compList[0];
                                }

                                // Ensure CompanyAccountingDetail exists and get COMPYID
                                var compyList = _db.Database.SqlQuery<int>("SELECT COMPYID FROM CompanyAccountingDetail WHERE CompId = " + compId + " AND YrId = " + yrId).ToList();
                                if (compyList.Count == 0)
                                {
                                    _db.Database.ExecuteSqlCommand("INSERT INTO CompanyAccountingDetail (CompId, YrId, CUSRID, PRCSDATE) VALUES (" + compId + ", " + yrId + ", '" + (Session["CUSRID"] ?? "system") + "', '" + DateTime.Now.ToString("MM-dd-yyyy") + "')");
                                    compyId = _db.Database.SqlQuery<int>("SELECT MAX(COMPYID) FROM CompanyAccountingDetail").FirstOrDefault();
                                }
                                else
                                {
                                    compyId = compyList[0];
                                }
                                Session["compyid"] = compyId;
                            }
                            // Determine current financial year (assumes FY starts on April 1)
                            var now = DateTime.Now;
                            int startYear = now.Month >= 4 ? now.Year : now.Year - 1;
                            int endYear = startYear + 1;
                            string fy = $"{startYear % 100:00}-{endYear % 100:00}";
                            // Next receipt serial number for this company
                            int nextSerial = 1;
                            if (compyId.HasValue)
                            {
                                var maxSerial = _db.MemberShipPaymentDetails
                                    .Where(p => p.CompanyAccountingDetailId == compyId)
                                    .Max(p => (int?)p.ReceiptSerialNo);
                                nextSerial = (maxSerial ?? 0) + 1;
                            }
                            else
                            {
                                // Fallback: try to pick any existing compyid; if none, default to 1
                                var anyCompy = _db.Database.SqlQuery<int?>("SELECT TOP 1 COMPYID FROM CompanyAccountingDetail ORDER BY COMPYID").FirstOrDefault();
                                compyId = anyCompy ?? 1;
                                log.Warn($"CompanyAccountingDetailId not found in session/derivation. Using fallback compyId={compyId}.");
                                var maxSerial = _db.MemberShipPaymentDetails
                                    .Where(p => p.CompanyAccountingDetailId == compyId)
                                    .Max(p => (int?)p.ReceiptSerialNo);
                                nextSerial = (maxSerial ?? 0) + 1;
                            }

                            // Debug logging
                            log.Debug($"Resolved CompanyAccountingDetailId (compy_id): {compyId}, FY: {fy}, NextSerial: {nextSerial}");

                            var payment = new MemberShipPaymentDetail
                            {
                                MemberID = viewModel.Member.MemberID,
                                MemberTypeId = memberType.MemberTypeId,
                                MemberTypeAmount = memberType.MembershipFee,
                                Payment_Date = DateTime.Now,
                                Renewal_Date = viewModel.Member.Member_Edate, // Use calculated expiry date
                                UPI_ID = "NA",
                                RRN_NO = "NA",
                                Amount = memberType.MembershipFee,
                                Payment_Type = "Online",
                                Payment_Status = "Pending",
                                Payment_Plan = "Standard",
                                Payment_Receipt_No = string.Format("MR/{0}/{1}", fy, nextSerial.ToString("D3")),
                                ReceiptSerialNo = nextSerial,
                                ReceiptDocumentNo = fy,
                                CompanyAccountingDetailId = compyId
                            };
                            _db.MemberShipPaymentDetails.Add(payment);
                            _db.SaveChanges();

                            // Save children if married and has children
                            if (MaritalStatus == "Married" && viewModel.Member.Total_Children > 0)
                            {
                                log.Debug($"Processing {viewModel.Member.Total_Children} children");
                                for (int i = 0; i < viewModel.Member.Total_Children; i++)
                                {
                                    var childName = Request.Form[$"Children[{i}].Child_Name"];
                                    if (!string.IsNullOrEmpty(childName))
                                    {
                                        var child = new MemberShipFamilyDetail
                                        {
                                            Child_Name = childName,
                                            Child_DOB = DateTime.Parse(Request.Form[$"Children[{i}].Child_DOB"]),
                                            Child_Age = Request.Form[$"Children[{i}].Child_Age"],
                                            Child_Gender = int.Parse(Request.Form[$"Children[{i}].Child_Gender"]),
                                            Child_Current_Position = int.Parse(Request.Form[$"Children[{i}].Child_Current_Position"]),
                                            Child_MaritalStatus = int.Parse(Request.Form[$"Children[{i}].Child_MaritalStatus"]),
                                            MemberID = viewModel.Member.MemberID,
                                            ModifiedBy = User.Identity.Name,
                                            ModifiedDateTime = DateTime.Now
                                        };
                                        _db.MemberShipFamilyDetails.Add(child);
                                        log.Debug($"Added child: {JsonConvert.SerializeObject(child)}");
                                    }
                                }
                                _db.SaveChanges();
                                log.Info("Successfully saved children");
                            }

                            // Save organization details
                            var orgNames = Request.Form.GetValues("MemberIn[]");
                            var sinceYears = Request.Form.GetValues("Since[]");
                            var currDesigs = Request.Form.GetValues("CurrentDesignation[]");

                            if (orgNames != null)
                            {
                                log.Debug($"Processing {orgNames.Length} organization details");
                                for (int i = 0; i < orgNames.Length; i++)
                                {
                                    if (!string.IsNullOrWhiteSpace(orgNames[i]) &&
                                        !string.IsNullOrWhiteSpace(currDesigs[i]) &&
                                        int.TryParse(sinceYears[i], out int sinceYear))
                                    {
                                        var odetail = new MemberShipODetail
                                        {
                                            MemberID = viewModel.Member.MemberID,
                                            OrganizationName = orgNames[i],
                                            SinceYear = sinceYear,
                                            CurrentDesignation = currDesigs[i],
                                            ModifiedBy = User.Identity.Name,
                                            ModifiedDateTime = DateTime.Now
                                        };
                                        _db.MemberShipODetails.Add(odetail);
                                        log.Debug($"Added organization detail: {JsonConvert.SerializeObject(odetail)}");
                                    }
                                }
                                _db.SaveChanges();
                                log.Info("Successfully saved organization details");
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Member profile created successfully!";
                            return RedirectToAction("Index");
                        }
                        catch (DbEntityValidationException ex)
                        {
                            transaction.Rollback();
                            foreach (var validationErrors in ex.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                                    log.Error($"Validation Error: {validationError.PropertyName} - {validationError.ErrorMessage}");
                                }
                            }
                        }
                        catch (DbUpdateException ex)
                        {
                            transaction.Rollback();
                            // Extract deepest inner exception message for clarity
                            Exception inner = ex;
                            while (inner.InnerException != null) inner = inner.InnerException;
                            string detail = inner.Message;
                            log.Error("DbUpdateException while saving member or related data: " + detail, ex);
                            ModelState.AddModelError("", "Database update failed: " + detail);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", $"Error: {ex.Message}");
                            log.Error("Error saving to database", ex);
                        }
                    }
                }
                else
                {
                    log.Warn("ModelState is invalid");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        foreach (var error in state.Errors)
                        {
                            log.Warn($"Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                log.Error("Unhandled exception in Create action", ex);
            }

            PopulateViewBags();
            return View(viewModel);
        }

        private int GenerateMemberNumber()
        {
            return _db.MemberShipMasters.Max(m => (int?)m.MemberNo) + 1 ?? 1000;
        }

        private int CalculateAge(DateTime dob)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        public ActionResult ViewLogs()
        {
            string logPath = Server.MapPath("~/App_Data/Logs/Application.log");
            if (System.IO.File.Exists(logPath))
            {
                return Content(System.IO.File.ReadAllText(logPath));
            }
            return Content("No logs found");
        }

        // GET: Members/Edit/5
        //[Authorize(Roles = "Admin")]
        [Route("Members/Edit/{id:int}")]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var member = _db.MemberShipMasters.Find(id);
            if (member == null) return HttpNotFound();
            var viewModel = new MemberProfileViewModel
            {
                Member = member,
                Children = _db.MemberShipFamilyDetails.Where(f => f.MemberID == member.MemberID).ToList(),
                OrganizationDetails = _db.MemberShipODetails.Where(o => o.MemberID == member.MemberID).ToList()
            };
            PopulateViewBags();
            return View(viewModel);
        }

        [HttpPost]
        [Route("Members/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, MemberProfileViewModel viewModel, HttpPostedFileBase Photo, HttpPostedFileBase FamilyPhoto)
        {
            try
            {
                log.Info("Starting Edit action");
                var routeId = Request?.RequestContext?.RouteData?.Values?["id"];
                log.Debug($"Edit POST RouteData id: {routeId}");
                try
                {
                    var allKeys = Request?.Form?.AllKeys?.Where(k => k != null).ToArray() ?? new string[0];
                    log.Debug($"Form keys: {string.Join(", ", allKeys)}");
                }
                catch (Exception exKeys)
                {
                    log.Warn("Unable to enumerate form keys", exKeys);
                }
                log.Debug($"ModelState.IsValid: {ModelState.IsValid}");
                // Ensure model MemberID is set from route if missing
                if (viewModel?.Member != null && viewModel.Member.MemberID == 0 && id > 0)
                {
                    viewModel.Member.MemberID = id;
                }

                log.Debug($"Member data received: {JsonConvert.SerializeObject(viewModel.Member)}");
                log.Debug($"Children count: {(viewModel.Children != null ? viewModel.Children.Count : 0)}");
                log.Debug($"OrganizationDetails count: {(viewModel.OrganizationDetails != null ? viewModel.OrganizationDetails.Count : 0)}");

                // Relax validation for server-managed fields that are not edited via the form
                ModelState.Remove("Member.CreatedBy");
                ModelState.Remove("Member.CreatedDateTime");
                ModelState.Remove("Member.MemberNo");
                ModelState.Remove("Member.MemberDNo");
                ModelState.Remove("Member.Member_Reg_Date");
                ModelState.Remove("Member.Member_Sdate");
                ModelState.Remove("Member.Member_Edate");
                ModelState.Remove("Member.UIserID");
                ModelState.Remove("Member.MemberTypeId");
                ModelState.Remove("Member.MemberTypeAmount");

                if (ModelState.IsValid)
                {
                    // Get the member
                    var member = _db.MemberShipMasters.Find(viewModel.Member.MemberID);
                    if (member == null)
                    {
                        log.Warn($"Member not found with ID: {viewModel.Member.MemberID}");
                        return HttpNotFound();
                    }

                    // Log before changes
                    log.Debug($"Original member data: {JsonConvert.SerializeObject(member)}");
                    log.Debug($"Original children count: {_db.MemberShipFamilyDetails.Count(f => f.MemberID == member.MemberID)}");
                    log.Debug($"Original org details count: {_db.MemberShipODetails.Count(o => o.MemberID == member.MemberID)}");

                    // Handle photo upload
                    if (Photo != null && Photo.ContentLength > 0)
                    {
                        log.Debug("Processing member photo upload in Edit");
                        string uploadsFolder = Server.MapPath("~/Uploads/MemberPhotos");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        Photo.SaveAs(filePath);
                        viewModel.Member.Member_Photo_Path = $"/Uploads/MemberPhotos/{uniqueFileName}";
                        log.Debug($"Saved new member photo to: {filePath}");
                    }
                    else
                    {
                        // Keep existing photo path if no new photo uploaded
                        viewModel.Member.Member_Photo_Path = member.Member_Photo_Path;
                    }

                    // Handle family photo upload
                    if (FamilyPhoto != null && FamilyPhoto.ContentLength > 0)
                    {
                        log.Debug("Processing family photo upload in Edit");
                        string uploadsFolder = Server.MapPath("~/Uploads/FamilyPhotos");
                        Directory.CreateDirectory(uploadsFolder);
                        string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(FamilyPhoto.FileName)}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        FamilyPhoto.SaveAs(filePath);
                        viewModel.Member.Family_Photo_Path = $"/Uploads/FamilyPhotos/{uniqueFileName}";
                        log.Debug($"Saved new family photo to: {filePath}");
                    }
                    else
                    {
                        // Keep existing family photo path if no new photo uploaded
                        viewModel.Member.Family_Photo_Path = member.Family_Photo_Path;
                    }

                    // Update MemberShipMaster fields (basic info)
                    member.Member_Name = viewModel.Member.Member_Name;
                    member.Gender = viewModel.Member.Gender;
                    member.Member_DOB = viewModel.Member.Member_DOB;
                    member.Member_Age = CalculateAge(viewModel.Member.Member_DOB).ToString();
                    member.BldGID = viewModel.Member.BldGID;
                    member.Member_EmailID = viewModel.Member.Member_EmailID;
                    member.Member_Mobile_No = viewModel.Member.Member_Mobile_No;
                    member.Member_WhatsApp_No = viewModel.Member.Member_WhatsApp_No;
                    member.Member_Landline_No = viewModel.Member.Member_Landline_No;
                    member.Member_Addr1 = viewModel.Member.Member_Addr1;
                    member.Member_Addr2 = viewModel.Member.Member_Addr2;
                    member.Member_City = viewModel.Member.Member_City;
                    // New: Resident Area
                    member.Member_Area = viewModel.Member.Member_Area;
                    member.StateID = viewModel.Member.StateID;
                    member.Member_Pincode = viewModel.Member.Member_Pincode;
                    member.Member_Country = viewModel.Member.Member_Country;
                    member.Member_Photo_Path = viewModel.Member.Member_Photo_Path;
                    member.Family_Photo_Path = viewModel.Member.Family_Photo_Path;

                    // Permanent address
                    member.Member_Per_Addr1 = viewModel.Member.Member_Per_Addr1;
                    member.Member_Per_Addr2 = viewModel.Member.Member_Per_Addr2;
                    member.Member_Per_City = viewModel.Member.Member_Per_City;
                    // New: Permanent Area
                    member.Member_Per_Area = viewModel.Member.Member_Per_Area;
                    member.PStateID = viewModel.Member.PStateID;
                    member.Member_Per_Pincode = viewModel.Member.Member_Per_Pincode;
                    member.Member_Per_Country = viewModel.Member.Member_Per_Country;
                    member.Member_Nativity = viewModel.Member.Member_Nativity;

                    // Marital and family info
                    member.MaritalStatus = viewModel.Member.MaritalStatus;
                    member.Spouse_Name = viewModel.Member.Spouse_Name;
                    member.Spouse_DOB = viewModel.Member.Spouse_DOB;
                    member.Date_Of_Marriage = viewModel.Member.Date_Of_Marriage;
                    member.Total_Children = viewModel.Member.Total_Children;

                    // Professional info
                    // EmploymentType radios in the view are named "EmploymentType" (not bound to Member), so read from form if present
                    var postedEmploymentType = Request.Form["EmploymentType"]; // "Salaried" | "Self" or null
                    member.EmploymentType = !string.IsNullOrWhiteSpace(postedEmploymentType)
                        ? postedEmploymentType
                        : viewModel.Member.EmploymentType;
                    member.Salaried_Location = viewModel.Member.Salaried_Location;
                    member.Business_Name = viewModel.Member.Business_Name;
                    member.Business_Location = viewModel.Member.Business_Location;
                    member.Occupation = viewModel.Member.Occupation;
                    member.Interests_and_Hobbies = viewModel.Member.Interests_and_Hobbies;

                    // References
                    member.Reference_By = viewModel.Member.Reference_By;
                    member.Reference_Contact_No = viewModel.Member.Reference_Contact_No;

                    // Category description (selected from dropdown)
                    member.CateTDesc = string.IsNullOrWhiteSpace(viewModel.Member.CateTDesc)
                        ? null
                        : viewModel.Member.CateTDesc;

                    // Modified info
                    member.ModifiedBy = User.Identity.Name;
                    member.ModifiedDateTime = DateTime.Now;

                    // Tell EF this entity has changed
                    _db.Entry(member).State = EntityState.Modified;

                    log.Debug("Updated member fields");

                    using (var transaction = _db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Handle Family Details - remove old and add new
                            var oldFamily = _db.MemberShipFamilyDetails
                                             .Where(f => f.MemberID == member.MemberID).ToList();

                            log.Debug($"Removing {oldFamily.Count} old family members");
                            _db.MemberShipFamilyDetails.RemoveRange(oldFamily);

                            // Only add children if married and Total_Children > 0
                            if (string.Equals(member.MaritalStatus, "Married", StringComparison.OrdinalIgnoreCase)
                                && viewModel.Children != null && viewModel.Children.Count > 0)
                            {
                                log.Debug($"Adding {viewModel.Children.Count} new family members");
                                foreach (var child in viewModel.Children)
                                {
                                    child.MemberID = member.MemberID;
                                    child.ModifiedBy = User.Identity.Name;
                                    child.ModifiedDateTime = DateTime.Now;
                                    _db.MemberShipFamilyDetails.Add(child);
                                    log.Debug($"Added child: {JsonConvert.SerializeObject(child)}");
                                }
                            }
                            else
                            {
                                // Ensure Total_Children is synced if no children are added
                                member.Total_Children = 0;
                            }

                            // Handle Organization Details - remove old and add new
                            var oldOrgs = _db.MemberShipODetails
                                           .Where(o => o.MemberID == member.MemberID).ToList();

                            log.Debug($"Removing {oldOrgs.Count} old organization details");
                            _db.MemberShipODetails.RemoveRange(oldOrgs);

                            // Prefer posted arrays (same format as Create view) if present
                            var orgNames = Request.Form.GetValues("MemberIn[]");
                            var sinceYears = Request.Form.GetValues("Since[]");
                            var currDesigs = Request.Form.GetValues("CurrentDesignation[]");

                            if (orgNames != null)
                            {
                                log.Debug($"Processing {orgNames.Length} organization details");
                                for (int i = 0; i < orgNames.Length; i++)
                                {
                                    if (!string.IsNullOrWhiteSpace(orgNames[i]) &&
                                        !string.IsNullOrWhiteSpace(currDesigs[i]) &&
                                        int.TryParse(sinceYears[i], out int sinceYear))
                                    {
                                        var odetail = new MemberShipODetail
                                        {
                                            MemberID = member.MemberID,
                                            OrganizationName = orgNames[i],
                                            SinceYear = sinceYear,
                                            CurrentDesignation = currDesigs[i],
                                            ModifiedBy = User.Identity.Name,
                                            ModifiedDateTime = DateTime.Now
                                        };
                                        _db.MemberShipODetails.Add(odetail);
                                        log.Debug($"Added organization detail: {JsonConvert.SerializeObject(odetail)}");
                                    }
                                }
                            }
                            else if (viewModel.OrganizationDetails != null && viewModel.OrganizationDetails.Count > 0)
                            {
                                log.Debug($"Adding {viewModel.OrganizationDetails.Count} new organization details from bound model");
                                foreach (var org in viewModel.OrganizationDetails)
                                {
                                    org.MemberID = member.MemberID;
                                    org.ModifiedBy = User.Identity.Name;
                                    org.ModifiedDateTime = DateTime.Now;
                                    _db.MemberShipODetails.Add(org);
                                    log.Debug($"Added organization detail: {JsonConvert.SerializeObject(org)}");
                                }
                            }

                            _db.SaveChanges();

                            // Update AspNetUsers.CateTid to match selected CateTDesc
                            try
                            {
                                if (!string.IsNullOrWhiteSpace(member.CateTDesc))
                                {
                                    using (var clubDb = new ClubMembership.Data.ClubMembershipDBEntities())
                                    {
                                        var cateTid = clubDb.Database.SqlQuery<int?>(
                                            "SELECT TOP 1 CateTid FROM CategoryTypeMaster WHERE RTRIM(LTRIM(CateTDesc)) = @p0",
                                            member.CateTDesc?.Trim()
                                        ).FirstOrDefault();

                                        // Find the associated ApplicationUser by username
                                        var appUser = _db.Users.FirstOrDefault(u => u.UserName == member.UserName);
                                        if (appUser == null)
                                        {
                                            // Fallback by email
                                            appUser = _db.Users.FirstOrDefault(u => u.Email == member.Member_EmailID);
                                        }
                                        if (appUser != null)
                                        {
                                            appUser.CateTid = cateTid;
                                            _db.Entry(appUser).State = EntityState.Modified;
                                            _db.SaveChanges();
                                        }
                                    }
                                }
                            }
                            catch (Exception exCate)
                            {
                                log.Warn("Failed to update AspNetUsers.CateTid during Edit", exCate);
                            }
                            transaction.Commit();

                            log.Info($"Successfully updated member with ID: {member.MemberID}");
                            TempData["SuccessMessage"] = "Member profile updated successfully!";
                            return RedirectToAction("Index");
                        }
                        catch (DbEntityValidationException ex)
                        {
                            transaction.Rollback();
                            log.Error("Entity validation failed during member update", ex);
                            foreach (var validationErrors in ex.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                                    log.Error($"Validation Error: {validationError.PropertyName} - {validationError.ErrorMessage}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError("", $"Error: {ex.Message}");
                            log.Error("Error saving member updates to database", ex);
                        }
                    }
                }
                else
                {
                    log.Warn("ModelState is invalid in Edit action");
                    foreach (var key in ModelState.Keys)
                    {
                        var state = ModelState[key];
                        foreach (var error in state.Errors)
                        {
                            log.Warn($"Key: {key}, Error: {error.ErrorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                log.Error("Unhandled exception in Edit action", ex);
            }

            PopulateViewBags();
            return View(viewModel);
        }



        // GET: Members/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var member = _db.MemberShipMasters.Find(id);
            if (member == null) return HttpNotFound();
            return View(member);
        }

        // POST: Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                log.Info($"Starting delete operation for Member ID: {id}");
                
                using (var transaction = _db.Database.BeginTransaction())
                {
                    try
                    {
                        // Get member details first to find associated user
                        var member = _db.MemberShipMasters.Find(id);
                        if (member == null)
                        {
                            log.Warn($"Member not found with ID: {id}");
                            return HttpNotFound();
                        }

                        log.Debug($"Found member: {member.Member_Name} (Username: {member.UserName})");

                        // Delete from all related tables in the correct order
                        // 1. Delete from MemberShip_FDetails (Family Details)
                        var familyDeleteCount = _db.Database.ExecuteSqlCommand(
                            "DELETE FROM MemberShip_FDetails WHERE MemberID = @p0", id);
                        log.Debug($"Deleted {familyDeleteCount} family detail records");

                        // 2. Delete from MemberShip_PaymentDetails (Payment Details)
                        var paymentDeleteCount = _db.Database.ExecuteSqlCommand(
                            "DELETE FROM MemberShip_PaymentDetails WHERE MemberID = @p0", id);
                        log.Debug($"Deleted {paymentDeleteCount} payment detail records");

                        // 3. Delete from MemberShip_ODetails (Organization Details)
                        var orgDeleteCount = _db.Database.ExecuteSqlCommand(
                            "DELETE FROM MemberShip_ODetails WHERE MemberID = @p0", id);
                        log.Debug($"Deleted {orgDeleteCount} organization detail records");

                        // 4. Delete from AspNetUsers table (if associated user exists)
                        if (!string.IsNullOrEmpty(member.UserName))
                        {
                            var userDeleteCount = _db.Database.ExecuteSqlCommand(
                                "DELETE FROM AspNetUsers WHERE UserName = @p0 OR Email = @p1", 
                                member.UserName, member.Member_EmailID);
                            log.Debug($"Deleted {userDeleteCount} user records from AspNetUsers");
                        }

                        // 5. Finally, delete from MemberShipMaster (main record)
                        var memberDeleteCount = _db.Database.ExecuteSqlCommand(
                            "DELETE FROM MemberShipMaster WHERE MemberID = @p0", id);
                        log.Debug($"Deleted {memberDeleteCount} member master records");

                        transaction.Commit();
                        log.Info($"Successfully deleted member {member.Member_Name} and all related data");
                        
                        TempData["SuccessMessage"] = $"Member '{member.Member_Name}' and all related data have been successfully deleted.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        log.Error($"Error during delete operation for Member ID {id}", ex);
                        TempData["ErrorMessage"] = $"Error deleting member: {ex.Message}";
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Unexpected error during delete operation for Member ID {id}", ex);
                TempData["ErrorMessage"] = $"Unexpected error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}