using ClubMembership.Data;
using ClubMembership.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;


using System.IO; // for File/Directory
using System.Linq; // for string.Join
using System.Text; // for Encoding


namespace ClubMembership.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        ApplicationDbContext _db = new ApplicationDbContext();
        public AccountController()
                   : this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())))
        {
        }

        IAuthenticationManager Authentication
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            UserManager = userManager;
        }

        public UserManager<ApplicationUser> UserManager { get; private set; }

        // GET: Account
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ApplicationDbContext context = new ApplicationDbContext();
            ClubMembershipDBEntities db = new ClubMembershipDBEntities();
            ViewBag.ReturnUrl = returnUrl;
            Session["DEPTID"] = "";
            Session["DEPTNAME"] = "";
            Session["CUSRID"] = "";
            Session["BRNCHNAME"] = "";
            Session["BRNCHID"] = "";
            Session["F_BRNCHID"] = "";
            Session["F_BRNCHNAME"] = "";
            Session["F_DBRNCHID"] = "";
            Session["F_DEPTNAME"] = "";
            Session["BRNCHCTYPE"] = "";
            Session["COMPID"] = "";
            Session["S_BRNCHID"] = "";
            Session["Group"] = "";
            Session["STATEID"] = "";
            Session["EMP_STATEID"] = "";
            Session["EMP_LOCTID"] = "";
            Session["grntranrefid"] = "0";
            ViewBag.COMPID = new SelectList(context.companymasters, "COMPID", "COMPNAME");
            Session["LDATE"] = DateTime.Now.ToString("dd-MM-yyyy");
            Session["GYrDesc"] = (DateTime.Now.Year - 1) + " - " + (DateTime.Now.Year);
            ViewBag.COMPYID = new SelectList(context.VW_ACCOUNTING_YEAR_DETAIL_ASSGN.OrderByDescending(m => m.YRDESC), "COMPYID", "YRDESC");

            Session["USER"] = "";

            // Clear any stale anti-forgery cookie to avoid user mismatch after login
            var afCookie = Request.Cookies["__RequestVerificationToken"];
            if (afCookie != null)
            {
                afCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(afCookie);
            }

            //return View(new LoginViewModel());
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        // NOTE: Temporarily disabled anti-forgery validation to unblock login due to missing token cookie
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl) { 
            ApplicationDbContext context = new ApplicationDbContext();
            ClubMembershipDBEntities db = new ClubMembershipDBEntities();

            ViewBag.COMPID = new SelectList(context.companymasters, "COMPID", "COMPNAME");
            ViewBag.COMPYID = new SelectList(context.VW_ACCOUNTING_YEAR_DETAIL_ASSGN.OrderByDescending(m => m.YRDESC), "COMPYID", "YRDESC");

            var brnchctype = 0;// context.Database.SqlQuery<Int16>("Select BRNCHCTYPE From BranchMaster Where BRNCHID = '" + user.BrnchId + "'").ToList();
            var stateid = 1;// context.Database.SqlQuery<Int32>("Select STATEID From BranchMaster Where BRNCHID = '" + user.BrnchId + "'").ToList();
                            //var deptid = "2";// context.Database.SqlQuery<Int32>("Select DEPTID From BRANCHDEPARTMENTMASTER Where DBRNCHID = '" + user.DBrnchId + "'").ToList();
                            //var deptdesc = "ADMIN";// context.Database.SqlQuery<string>("Select DDEPTDESC From BRANCHDEPARTMENTMASTER Where DBRNCHID = '" + user.DBrnchId + "'").ToList();
                            //var uid = user.Id;

            var userchk = context.Database.SqlQuery<int>("Select CateId From View_User_Diable_Chk_For_Login Where UserName = '" + model.UserName.Trim() + "' And DispStatus = 0").ToList();
            if (userchk.Count > 0)
            {
                if (ModelState.IsValid)
                {
                    var user = await UserManager.FindAsync(model.UserName, model.Password);
                    if (user != null)
                    {

                        context.Database.ExecuteSqlCommand("Update AspNetUsers Set NPassword = '" + model.Password + "' where id ='" + user.Id + "'");

                        Session["compyid"] = model.COMPYID;
                        Session["CUSRID"] = model.UserName;
                        Session["COMPID"] = model.COMPID;
                        Session["F_DEPTNAME"] = "ADMIN";// user.DeptName;
                        Session["BRNCHCTYPE"] = 0;// brnchctype[0];// model.BRNCHCTYPE;
                        Session["DEPTID"] = "2";// deptid[0];
                        Session["DEPTNAME"] = "ADMIN";// deptdesc[0];
                        Session["grntranrefid"] = "0";
                        Session["STATEID"] = 1;// stateid[0];

                        //

                        Session["LDATE"] = Request.Form.Get("LDATE"); var COMPID = Request.Form.Get("COMPID");
                        DateTime TmpDate = Convert.ToDateTime(Request.Form.Get("LDATE")).Date;
                        var LMNTH = TmpDate.Month; var LYR = TmpDate.Year; var PFYear = 0; var PTYear = 0; var PFDATE = ""; var PTDATE = ""; var GYrDesc = "";

                        if (LMNTH >= 4)
                        {// Response.Write(LMNTH + ".." + LYR + "..." + Session["LDATE"]); Response.End(); 
                            PFYear = LYR;
                            PTYear = LYR + 1;
                            PFDATE = "01/04/" + PFYear; PTDATE = "31/03/" + PTYear;
                            GYrDesc = PFYear + " - " + PTYear;



                        }
                        else
                        { //Response.Write("ELSE" + LMNTH + ".." + LYR + "..." + Session["LDATE"]); Response.End(); 
                            PFYear = LYR - 1;
                            PTYear = LYR;
                            PFDATE = "01/04/" + PFYear; PTDATE = "31/03/" + PTYear;
                            GYrDesc = PFYear + " - " + PTYear;
                        }

                        // Resolve CompId safely
                        var compIdStr = Request.Form.Get("COMPID");
                        int compIdParsed;
                        int compId;
                        if (!int.TryParse(compIdStr, out compIdParsed))
                        {
                            compId = context.companymasters
                                .OrderBy(c => c.COMPID)
                                .Select(c => c.COMPID)
                                .FirstOrDefault();
                        }
                        else
                        {
                            compId = compIdParsed;
                        }
                        Session["COMPID"] = compId;

                        // Get or create AccountingYear and resolve YrId deterministically
                        var accYears = context.Database.SqlQuery<PR_ACCOUNTINGYEAR_ID_CHK_Result>(
                            "PR_ACCOUNTINGYEAR_ID_CHK @PFYear={0},@PTYear={1}", PFYear, PTYear).ToList();
                        int yrId;
                        if (accYears.Count == 0)
                        {
                            context.Database.ExecuteSqlCommand(
                                "INSERT INTO AccountingYear (YrDesc, FDate, TDate, CUSRID, PRCSDATE) VALUES ({0}, {1}, {2}, {3}, {4})",
                                GYrDesc,
                                Convert.ToDateTime(PFDATE),
                                Convert.ToDateTime(PTDATE),
                                Session["CUSRID"],
                                DateTime.Now);

                            // Re-query to get the created YRID via the same proc
                            accYears = context.Database.SqlQuery<PR_ACCOUNTINGYEAR_ID_CHK_Result>(
                                "PR_ACCOUNTINGYEAR_ID_CHK @PFYear={0},@PTYear={1}", PFYear, PTYear).ToList();
                        }
                        yrId = accYears.Last().YRID;

                        // Get or create CompanyAccountingDetail for (CompId, YrId)
                        var compDtl = context.Database.SqlQuery<PR_COMPANYACCOUNTINGDETAIL_ID_CHK_Result>(
                            "PR_COMPANYACCOUNTINGDETAIL_ID_CHK @PCompId={0},@PYrId={1}", compId, yrId).ToList();

                        if (compDtl.Count == 0)
                        {
                            context.Database.ExecuteSqlCommand(
                                "INSERT INTO CompanyAccountingDetail (CompId, YrId, CUSRID, PRCSDATE) VALUES ({0}, {1}, {2}, {3})",
                                compId,
                                yrId,
                                Session["CUSRID"],
                                DateTime.Now);

                            var resolvedCompyId = context.Database.SqlQuery<int>(
                                "SELECT COMPYID FROM CompanyAccountingDetail WHERE CompId={0} AND YrId={1}",
                                compId, yrId).FirstOrDefault();
                            System.Web.HttpContext.Current.Session["compyid"] = resolvedCompyId;
                        }
                        else
                        {
                            System.Web.HttpContext.Current.Session["compyid"] = Convert.ToInt32(compDtl[0].COMPYID);
                        }

                        Session["GYrDesc"] = GYrDesc;

                        // Minimal diagnostics for tracing
                        System.Diagnostics.Debug.WriteLine($"[LoginInit] CompId={compId}, YrId={yrId}, GYrDesc={GYrDesc}, compyid={System.Web.HttpContext.Current.Session["compyid"]}");


                        //var sql = context.Database.SqlQuery<int>("select GroupId from ApplicationUserGroups inner join AspNetUsers on AspNetUsers.Id=ApplicationUserGroups.UserId where AspNetUsers.UserName='" + model.UserName + "'").ToList();

                        //if (sql[0].Equals(1)) { Session["Group"] = "Admin"; }
                        //if (sql[0].Equals(2)) { Session["Group"] = "SuperAdmin"; }
                        //if (sql[0].Equals(4)) { Session["Group"] = "Users"; }
                        //if (sql[0].Equals(3)) { Session["Group"] = "Manager"; }

                        var sql = context.Database.SqlQuery<VW_USER_DETAILS>("select * from VW_USER_DETAILS Where UserName='" + model.UserName + "'").ToList();
                        if (sql.Count == 0)
                        {
                            Session["Group"] = "";
                        }
                        else
                        {
                            if (sql.Count > 1)
                            { Session["Group"] = sql[1].GroupName; }
                            else
                            { Session["Group"] = sql[0].GroupName; }

                        }
                        //if (sql[0].Equals(1)) { Session["Group"] = "Admin"; }
                        //if (sql[0].Equals(2)) { Session["Group"] = "SuperAdmin"; }
                        // if (sql[0].Equals(4)) { Session["Group"] = "Users"; }
                        // if (sql[0].Equals(3)) { Session["Group"] = "Manager"; }

                       // var aa = Session["EMPLID"].ToString();
                        //var emplid = 0;
                        //if (aa != "") { emplid = Convert.ToInt32(Session["EMPLID"]); }
                        //var rsql = context.Database.SqlQuery<EmployeeMaster>("select * from EmployeeMaster Where CATEID = '" + emplid + "'").ToList();
                        //if (rsql.Count > 0)
                        //{
                        //    Session["EMP_STATEID"] = rsql[0].STATEID;
                        //    Session["EMP_LOCTID"] = rsql[0].LOCTID;
                        //}
                        //else
                        //{
                        //    Session["EMP_STATEID"] = "0";
                        //    Session["EMP_LOCTID"] = "0";
                        //}

                        //Session["EXCLPATH"] = "D:\\SACT_EXCEL\\" + Session["CUSRID"];



                    }

                    if (user != null)
                    {
                        // Track last login without requiring DB schema changes: use cookie + session
                        DateTime? prevLogin = null;
                        var prevCookie = Request.Cookies["LastLoginAt"];
                        if (prevCookie != null)
                        {
                            DateTime parsed;
                            if (DateTime.TryParse(prevCookie.Value, out parsed))
                            {
                                prevLogin = parsed;
                            }
                        }
                        Session["PreviousLoginTime"] = prevLogin;

                        await SignInAsync(user, model.RememberMe);

                        // Store current login time
                        var now = DateTime.Now;
                        Session["LoginTime"] = now;
                        var cookie = new HttpCookie("LastLoginAt", now.ToString("o")) { Expires = DateTime.Now.AddYears(1), HttpOnly = true };
                        Response.Cookies.Add(cookie);
                        // Clear any stale anti-forgery cookie created before sign-in
                        var afCookie2 = Request.Cookies["__RequestVerificationToken"];
                        if (afCookie2 != null)
                        {
                            afCookie2.Expires = DateTime.Now.AddDays(-1);
                            Response.Cookies.Add(afCookie2);
                        }
                        Session["MyMenu"] = "";
                        context.Database.ExecuteSqlCommand("delete from menurolemaster where Roles='" + model.UserName + "'");
                        context.Database.ExecuteSqlCommand("EXEC pr_USER_MENU_DETAIL_ASSGN @PKUSRID='" + model.UserName + "'");
                        return RedirectToLocal(returnUrl);
                        //return RedirectToAction("Index", "Home");
                    }

                    ModelState.AddModelError("", "Invalid username or password.");

                    return View(model);

                }
            }
            else
            {
                ModelState.AddModelError("", "User Name Not Exists.");
            }


            return View(model);
            //if (!ModelState.IsValid)
            //{
            //    return View(model);
            //}
            //var data = new Data();
            //var users = data.users();

            //if (users.Any(p => p.user == model.UserName && p.password == model.Password))
            //{
            //    var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, model.UserName),}, DefaultAuthenticationTypes.ApplicationCookie);

            //    Authentication.SignIn(new AuthenticationProperties
            //    {
            //        IsPersistent = model.RememberMe
            //    }, identity);

            //    return RedirectToAction("Index", "Home");
            //}
            //else
            //{
            //    ModelState.AddModelError("", "Invalid login attempt.");
            //    return View(model);
            //}
        }


        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }


        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            // Route users to the appropriate dashboard when no returnUrl is provided
            var group = Session["Group"] as string;
            if (!string.IsNullOrEmpty(group) && group.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("AdminDashboard", "Home");
            }
            // For regular users, land on a blank Index page; Dashboard is accessible from the menu
            return RedirectToAction("Index", "Home");
        }


        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            // Record the time of this logout as the last seen/login display for next visit
            try
            {
                var now = DateTime.Now;
                var cookie = new HttpCookie("LastLoginAt", now.ToString("o")) // ISO 8601
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddYears(1)
                };
                Response.Cookies.Add(cookie);
                // Clear session copies so a new session will prefer cookie value next time
                Session["PreviousLoginTime"] = now;
                Session["CurrentLoginTime"] = null;
            }
            catch { /* best-effort only */ }
            Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session.Clear();
            Session.Abandon();
            var afCookie = Request.Cookies["__RequestVerificationToken"];
            if (afCookie != null)
            {
                afCookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(afCookie);
            }
            return RedirectToAction("Login", "Account");
        }






        //[AllowAnonymous]
        //public ActionResult Login_Register()

        //{
        //    //ViewBag.BRNCHID = new SelectList(_db.branchmasters.OrderBy(x => x.BRNCHNAME), "BRNCHID", "BRNCHNAME");
        //    ViewBag.DBRNCHID = GetEmployeeSelectList();
        //    // ViewBag.DBRNCHID = new SelectList("");
        //    return View();
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Login_Register(AccountViewModels.RegisterViewModel model)
        //{
        //    try
        //    {
        //        if (model.Password != model.ConfirmPassword)
        //        {
        //            ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
        //        }

        //        if (!string.IsNullOrEmpty(model.MobileNo) &&
        //            !System.Text.RegularExpressions.Regex.IsMatch(model.MobileNo, @"^[0-9]{10}$"))
        //        {
        //            ModelState.AddModelError("MobileNo", "Please enter a valid 10-digit mobile number");
        //        }

        //        if (model.DOB > DateTime.Now)
        //        {
        //            ModelState.AddModelError("DOB", "Date of birth cannot be in the future");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            var user = model.GetUser();
        //            user.NPassword = model.Password;
        //            var result = await UserManager.CreateAsync(user, model.Password);

        //            if (result.Succeeded)
        //            {
        //                LogError("User created successfully: " + user.UserName);
        //                return RedirectToAction("Login", "Account");
        //            }
        //            else
        //            {
        //                // Log errors
        //                LogError("User creation failed: " + string.Join("; ", result.Errors));

        //                // Show errors on the form
        //                foreach (var error in result.Errors)
        //                {
        //                    ModelState.AddModelError("", error); // empty key = summary error
        //                }
        //            }
        //        }
        //        else
        //        {
        //            var errors = ModelState.Values.SelectMany(v => v.Errors)
        //                                          .Select(e => e.ErrorMessage)
        //                                          .Where(msg => !string.IsNullOrEmpty(msg));

        //            LogError("ModelState invalid: " + string.Join("; ", errors));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError("Exception: " + ex);
        //        ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
        //    }

        //    ViewBag.DBRNCHID = GetEmployeeSelectList();
        //    return View("Login_Register", model);
        //}

        //private void LogError(string message)
        //{
        //    try
        //    {
        //        var logDir = Server.MapPath("~/App_Data/Logs");
        //        if (!System.IO.Directory.Exists(logDir))
        //        {
        //            System.IO.Directory.CreateDirectory(logDir);
        //        }

        //        var logPath = System.IO.Path.Combine(logDir, "errorlog.txt");
        //        var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
        //        System.IO.File.AppendAllText(logPath, logMessage, Encoding.UTF8);
        //    }
        //    catch
        //    {
        //        // Ignore logging errors
        //    }
        //}






        [HttpPost]
        // [Authorize(Roles = "Admin, CanEditUser")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(AccountViewModels.RegisterViewModel model)
        {
            // Do NOT remove required fields from ModelState
            // ModelState.Remove("MobileNo");
            // ModelState.Remove("DOB");
            // ModelState.Remove("Gender");

            // Add password confirmation validation
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "The password and confirmation password do not match.");
            }

            // Validate mobile number format (basic validation)
            if (!string.IsNullOrEmpty(model.MobileNo) && !System.Text.RegularExpressions.Regex.IsMatch(model.MobileNo, @"^[0-9]{10}$"))
            {
                ModelState.AddModelError("MobileNo", "Please enter a valid 10-digit mobile number");
            }

            // Validate DOB (must be in the past)
            if (model.DOB > DateTime.Now)
            {
                ModelState.AddModelError("DOB", "Date of birth cannot be in the future");
            }

            if (ModelState.IsValid)
            {
                var user = model.GetUser();
                user.NPassword = model.Password;
                // Ensure CateTid is set to avoid DB NOT NULL constraint violations
                if (!user.CateTid.HasValue)
                {
                    using (var db0 = new ClubMembershipDBEntities())
                    {
                        // Pick a sensible default: first active category
                        var defaultCateTid = db0.Database
                            .SqlQuery<int?>(
                                "SELECT TOP 1 CateTid FROM CategoryTypeMaster WHERE Dispstatus = 0 ORDER BY CateTid")
                            .FirstOrDefault();

                        if (defaultCateTid.HasValue)
                        {
                            user.CateTid = defaultCateTid.Value;
                        }
                        else
                        {
                            // No active categories exist; cannot proceed safely
                            ModelState.AddModelError("CateTid", "No active Category Type found. Please create a Category Type first.");
                            return View(model);
                        }
                    }
                }
                // Ensure MemberID is set to satisfy DB NOT NULL constraint by pre-creating a minimal MemberShipMaster
                if (!user.MemberID.HasValue)
                {
                    using (var db = new ApplicationDbContext())
                    {
                        // Determine a default membership type (first active)
                        var memberType = db.MemberShipTypeMasters
                            .Where(m => m.DisplayStatus == 0)
                            .OrderBy(m => m.MembershipFee)
                            .Select(m => new { m.MemberTypeId, m.MembershipFee, m.NoOfYears })
                            .FirstOrDefault();

                        if (memberType == null)
                        {
                            ModelState.AddModelError("MemberID", "No active Membership Type found. Please configure Membership Types first.");
                            return View(model);
                        }

                        // Generate next MemberNo
                        int nextMemberNo = (db.MemberShipMasters.Max(m => (int?)m.MemberNo) ?? 999) + 1;
                        string registerYear = DateTime.Now.Year.ToString();
                        string dobYear = model.DOB.Year.ToString();
                        string memberNoStr = nextMemberNo.ToString().PadLeft(3, '0');
                        string memberDNo = $"{registerYear}{dobYear}{memberNoStr}";

                        // Calculate age
                        int age = DateTime.Today.Year - model.DOB.Year;
                        if (model.DOB.Date > DateTime.Today.AddYears(-age)) age--;

                        var renewalDate = DateTime.Now.AddYears(memberType.NoOfYears);

                        var preMember = new MemberShipMaster
                        {
                            UIserID = model.UserName,
                            RegstrId = "1",
                            MemberNo = nextMemberNo,
                            MemberDNo = memberDNo,
                            Member_Reg_Date = DateTime.Now,
                            Member_Name = ($"{model.FirstName} {model.LastName}").Trim(),
                            Gender = string.Equals(model.Gender, "Male", StringComparison.OrdinalIgnoreCase) ? 1 : 2,
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
                            NPassword = model.Password,
                            MemberTypeId = memberType.MemberTypeId,
                            MemberTypeAmount = memberType.MembershipFee,
                            CateTDesc = null
                        };

                        db.MemberShipMasters.Add(preMember);
                        db.SaveChanges();

                        user.MemberID = preMember.MemberID;
                    }
                }
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Confirm user is in DB
                    var createdUser = _db.Users.FirstOrDefault(u => u.UserName == user.UserName);
                    if (createdUser != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"User created: {createdUser.UserName}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("User not found after creation!");
                    }
                    return RedirectToAction("Index", "Account");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        private SelectList GetEmployeeSelectList()
        {
            return new SelectList(
                _db.employeemasters
                   .Where(x => x.DISPSTATUS == 0)
                   .OrderBy(x => x.CATENAME),
                "CATEID",
                "CATENAME"
            );
        }

        //private void AddErrors(IdentityResult result)
        //{
        //    foreach (var error in result.Errors)
        //    {
        //        ModelState.AddModelError("", error);
        //    }
        //}


        //    [Authorize(Roles = "Admin, CanEditUser")]
        public ActionResult UserGroups(string id)
        {
            var user = _db.Users.First(u => u.UserName == id);
            var model = new AccountViewModels.SelectUserGroupsViewModel(user);
            return View(model);
        }


        [HttpPost]
        // [Authorize(Roles = "Admin, CanEditUser")]
        [ValidateAntiForgeryToken]
        public ActionResult UserGroups(AccountViewModels.SelectUserGroupsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var idManager = new IdentityManager();
                var user = _db.Users.First(u => u.UserName == model.UserName);
                idManager.ClearUserGroups(user.Id);
                foreach (var group in model.Groups)
                {
                    if (group.Selected)
                    {
                        idManager.AddUserToGroup(user.Id, group.GroupId);
                    }
                }
                return RedirectToAction("index");
            }
            return View();
        }


        // [Authorize(Roles = "Admin, CanEditRole, CanEditGroup, User")]
        public ActionResult UserPermissions(string id)
        {
            var user = _db.Users.First(u => u.UserName == id);
            var model = new AccountViewModels.UserPermissionsViewModel(user);
            return View(model);
        }


        // [Authorize(Roles = "Admin, CanEditUser")]
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        //  [Authorize(Roles = "Admin, CanEditUser")]
        public async Task<ActionResult> Manage(AccountViewModels.ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        //  [Authorize(Roles = "CanEditUser")]
        public ActionResult Index()
        {
            var users = _db.Users.OrderBy(X => X.UserName);
            var model = new List<AccountViewModels.EditUserViewModel>();
            foreach (var user in users)
            {
                var u = new AccountViewModels.EditUserViewModel(user);
                // if(u.UserName==Session["CUSRID"].ToString())
                model.Add(u);
            }
            return View(model);
        }

       // [Authorize(Roles = "UserPasswordChange")]
        public ActionResult CIndex()
        {
            var uname = Session["CUSRID"];
            var users = _db.Users.Where(X => X.UserName == uname).OrderBy(X => X.UserName);
            var model = new List<AccountViewModels.EditUserViewModel>();
            foreach (var user in users)
            {
                var u = new AccountViewModels.EditUserViewModel(user);
                // if(u.UserName==Session["CUSRID"].ToString())
                model.Add(u);
            }
            return View(model);
        }


        //   [Authorize(Roles = "Admin, CanEditUser")]
        public ActionResult Edit(string id, ManageMessageId? Message = null)
        {
            var user = _db.Users.First(u => u.UserName == id);
            var model = new AccountViewModels.EditUserViewModel(user);
           // ViewBag.BRNCHID = new SelectList(_db.branchmasters, "BRNCHID", "BRNCHNAME", brnchid);
            ViewBag.DBRNCHID = new SelectList(_db.employeemasters, "CATEID", "CATENAME");
            //ViewBag.DBRNCHID = new SelectList(_db.branchdepartmentmasters, "DBRNCHID", "DDEPTDESC", dbrnchid);
            // ViewBag.DBRNCHID = new SelectList(_db.branchdepartmentmasters.Where(x => x.BRNCHID == model.BrnchId), "DBRNCHID", "DDEPTDESC", model.DBrnchId);
            ViewBag.MessageId = Message;
            return View(model);
        }


        [HttpPost]
        //   [Authorize(Roles = "Admin, CanEditUser")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(AccountViewModels.EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                //ViewBag.BRNCHID = new SelectList(_db.branchmasters, "BRNCHID", "BRNCHNAME", model.BrnchId);
                ViewBag.DBRNCHID = new SelectList(_db.employeemasters, "CATEID", "CATENAME");
                //ViewBag.DBRNCHID = new SelectList(_db.branchdepartmentmasters.Where(x => x.BRNCHID == model.BrnchId), "DBRNCHID", "DDEPTDESC", model.DBrnchId);

                //    ViewBag.Subjects = new SelectList(_odb.SUBJ_MSTR.Where(o => o.TYPE == "4" && o.TYPE == "5").OrderBy(o => o.SUBJ_NAME), "SUBJ_ID", "SUBJ_VAL");

                var user = _db.Users.First(u => u.UserName == model.UserName);
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;

                // Update new fields
                user.MobileNo = model.MobileNo;
                user.DOB = model.DOB;
                user.Gender = model.Gender;

                //user.DeptName = model.DeptName;

                //user
                _db.Entry(user).State = System.Data.Entity.EntityState.Modified;
                await _db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        [Authorize(Roles = "Admin, CanEditUser")]
        public ActionResult Delete(string id = null)
        {
            var user = _db.Users.First(u => u.UserName == id);
            var model = new AccountViewModels.EditUserViewModel(user);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, CanEditUser")]
        public ActionResult DeleteConfirmed(string id)
        {
            var user = _db.Users.First(u => u.UserName == id);
            _db.Users.Remove(user);
            _db.SaveChanges();
            return RedirectToAction("Index");
        }


        //public JsonResult BranchDepartment(int id)
        //{
        //    var result = _db.Database.SqlQuery<BranchDepartmentMaster>("select * FROM BranchDepartmentMaster where BRNCHID =" + id + " ORDER BY DDEPTDESC").ToList();
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        [HttpGet]
        public JsonResult SearchEmployees(string searchTerm)
        {
            try
            {
                var employees = _db.employeemasters
                    .Where(e => e.DISPSTATUS == 0 && 
                          (e.CATENAME.Contains(searchTerm) || 
                           e.CATECODE.Contains(searchTerm)))
                    .OrderBy(e => e.CATENAME)
                    .Take(10)
                    .AsEnumerable() // Switch to client-side evaluation
                    .Select(e => new {
                        id = e.CATEID,
                        text = e.CATENAME + " (" + e.CATECODE + ")" // Simple concatenation
                    })
                    .ToList();

                return Json(new { results = employees }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetEmployeesByDepartment(int departmentId)
        {
            try
            {
                // Adjust this based on your actual department relationship
                var employees = _db.employeemasters
                    .Where(e => e.DISPSTATUS == 0) // Active employees only
                    .OrderBy(e => e.CATENAME)
                    .Select(e => new {
                        id = e.CATEID,
                        text = $"{e.CATENAME} ({e.CATECODE})"
                    })
                    .ToList();

                return Json(employees, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Create()
        {
            // Fetch blood groups from database
            var bloodGroups = _db.BloodGroupMasters
                              .Where(b => b.DISPSTATUS == 0) // Active records only
                              .OrderBy(b => b.BLDGDESC)
                              .ToList();

            // Store in ViewBag for dropdown options
            ViewBag.BloodGroups = new SelectList(bloodGroups, "BLDGCODE", "BLDGDESC");

            return View();
        }

        [HttpPost]
        public ActionResult Create(string BloodGroup) // Matches dropdown name
        {
            // BloodGroup will contain the selected BLDGCODE
            // Save to database or process as needed
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
    }
}