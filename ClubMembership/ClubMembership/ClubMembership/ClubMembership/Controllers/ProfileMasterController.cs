using ClubMembership.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace ClubMembership.Controllers
{
    public class ProfileMasterController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ProfileMasterController()
        {
            _db = new ApplicationDbContext();
        }

        // GET: ProfileMaster
        public ActionResult Index()
        {
            return RedirectToAction("Create");
        }

        [HttpGet]
        public ActionResult Create()
        {
            var profile = GetFullProfileForCurrentUser();

            // Only create a new profile if none exists in MemberShipMaster
            if (profile == null)
            {
                var userId = User.Identity.GetUserId();
                var user = _db.Users.FirstOrDefault(u => u.Id == userId);
                var member = new MemberShipMaster
                {
                    UIserID = user.UserName,
                    Member_EmailID = user.Email,
                    Member_DOB = user.DOB,
                    Member_Mobile_No = user.MobileNo,
                    Gender = user.Gender == "Male" ? 1 : 2,
                    Member_Reg_Date = DateTime.Now,
                    Member_Sdate = DateTime.Now,
                    Member_Edate = DateTime.Now.AddYears(1),
                    DispStatus = 1
                };
                profile = new MemberProfileViewModel
                {
                    Member = member,
                    Children = new List<MemberShipFamilyDetail>(),
                    OrganizationDetails = new List<MemberShipODetail>()
                };
            }

            PopulateViewBags();
            try
            {
                if (profile != null && profile.Member != null)
                {
                    System.IO.File.AppendAllText(Server.MapPath("~/App_Data/debug.log"),
                        $"[GET Create] User={User.Identity.Name} MemberID={profile.Member.MemberID} Created=\"{profile.Member.CreatedDateTime}\" Modified=\"{profile.Member.ModifiedDateTime}\"\n");
                }
            }
            catch { }
            return View(profile);
        }

        private MemberProfileViewModel GetFullProfileForCurrentUser()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return null;

            // Use UserName to match UIserID in MemberShipMaster
            var member = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == user.UserName);
            if (member == null)
                return null;

            // --- Add this block ---
            if (string.IsNullOrEmpty(member.MaritalStatus))
            {
                member.MaritalStatus = (member.Total_Children > 0 || !string.IsNullOrEmpty(member.Spouse_Name)) ? "Married" : "Single";
            }
            if (string.IsNullOrEmpty(member.EmploymentType))
            {
                member.EmploymentType = "Salaried";
            }
            // --- End block ---

            var children = _db.MemberShipFamilyDetails.Where(f => f.MemberID == member.MemberID).ToList();
            var orgDetails = _db.MemberShipODetails.Where(o => o.MemberID == member.MemberID).ToList();

            return new MemberProfileViewModel
            {
                Member = member,
                Children = children,
                OrganizationDetails = orgDetails
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MemberProfileViewModel model, HttpPostedFileBase Photo, HttpPostedFileBase FamilyPhoto, string MaritalStatus)
        {
            // Get current user
            var userId = User.Identity.GetUserId();
            var user = _db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                // Always update these identifiers from AspNetUsers
                model.Member.UIserID = user.UserName;
                model.Member.Member_EmailID = user.Email;
                model.Member.Member_Mobile_No = user.MobileNo;
                // Do not overwrite Gender; allow user edits via form
            }

            // Remove ModelState errors for these fields so they are re-validated (use correct prefixed keys)
            ModelState.Remove("Member.UIserID");
            ModelState.Remove("Member.Member_EmailID");
            // Keep DOB in ModelState for validation and to allow user edits
            ModelState.Remove("Member.Member_Mobile_No");
            // Keep Gender in ModelState for validation and to allow user edits

            // At the start of your action method
            string logPath = Server.MapPath("~/App_Data/debug.log");
            string logContent = $"----- {DateTime.Now} -----\n";
            foreach (var key in Request.Form.AllKeys)
            {
                logContent += $"{key} = {Request.Form[key]}\n";
            }
            foreach (var key in ModelState.Keys)
            {
                foreach (var error in ModelState[key].Errors)
                {
                    logContent += $"ERROR: {key} - {error.ErrorMessage}\n";
                }
            }
            System.IO.File.AppendAllText(logPath, logContent);

            // >>> ADD THE REQUIRED FIELD SETTING CODE HERE <<<
            if (string.IsNullOrEmpty(model.Member.MemberDNo))
            {
                var registerYear = DateTime.Now.Year.ToString();
                var dobYear = model.Member.Member_DOB.Year.ToString();
                var memberNoStr = model.Member.MemberNo.ToString().PadLeft(3, '0');
                model.Member.MemberDNo = $"{registerYear}{dobYear}{memberNoStr}";
            }
            if (string.IsNullOrEmpty(model.Member.Member_Name))
            {
                model.Member.Member_Name = "Default Name"; // Or get from user or form
            }
            if (string.IsNullOrEmpty(model.Member.Member_Age) && model.Member.Member_DOB != null)
            {
                model.Member.Member_Age = CalculateAge(model.Member.Member_DOB).ToString();
            }
            if (string.IsNullOrEmpty(model.Member.Member_Addr1)) model.Member.Member_Addr1 = "Address1";
            if (string.IsNullOrEmpty(model.Member.Member_Pincode)) model.Member.Member_Pincode = "000000";
            if (string.IsNullOrEmpty(model.Member.Member_Country)) model.Member.Member_Country = "India";
            if (string.IsNullOrEmpty(model.Member.Member_Per_Addr1)) model.Member.Member_Per_Addr1 = "Address1";
            if (string.IsNullOrEmpty(model.Member.Member_Per_Pincode)) model.Member.Member_Per_Pincode = "000000";
            if (string.IsNullOrEmpty(model.Member.Member_Per_Country)) model.Member.Member_Per_Country = "India";
            if (string.IsNullOrEmpty(model.Member.Member_Nativity)) model.Member.Member_Nativity = "Kerala";
            if (string.IsNullOrEmpty(model.Member.CreatedBy)) model.Member.CreatedBy = User.Identity.Name;
            if (string.IsNullOrEmpty(model.Member.Reference_By)) model.Member.Reference_By = "Self";
            if (string.IsNullOrEmpty(model.Member.Reference_Contact_No)) model.Member.Reference_Contact_No = model.Member.Member_Mobile_No;
            // >>> END OF REQUIRED FIELD SETTING CODE <<<

            // Ensure EmploymentType posts and normalize dependent fields
            var postedEmpType = Request.Form["Member.EmploymentType"]; // Reliable capture
            if (!string.IsNullOrWhiteSpace(postedEmpType))
            {
                model.Member.EmploymentType = postedEmpType;
            }
            // Fallback for legacy/unprefixed key
            if (string.IsNullOrWhiteSpace(model.Member.EmploymentType))
            {
                var altEmp = Request.Form["EmploymentType"];
                if (!string.IsNullOrWhiteSpace(altEmp))
                    model.Member.EmploymentType = altEmp;
            }

            // Defensive capture for possibly unprefixed inputs
            if (string.IsNullOrWhiteSpace(model.Member.Salaried_Location))
            {
                var sal = Request.Form["Member.Salaried_Location"] ?? Request.Form["SalariedLocation"];
                if (!string.IsNullOrWhiteSpace(sal)) model.Member.Salaried_Location = sal;
            }
            if (string.IsNullOrWhiteSpace(model.Member.Business_Name))
            {
                var bname = Request.Form["Member.Business_Name"] ?? Request.Form["Business_Name"];
                if (!string.IsNullOrWhiteSpace(bname)) model.Member.Business_Name = bname;
            }
            if (string.IsNullOrWhiteSpace(model.Member.Business_Location))
            {
                var bloc = Request.Form["Member.Business_Location"] ?? Request.Form["Business_Location"];
                if (!string.IsNullOrWhiteSpace(bloc)) model.Member.Business_Location = bloc;
            }
            if (string.IsNullOrWhiteSpace(model.Member.Occupation))
            {
                var occ = Request.Form["Member.Occupation"] ?? Request.Form["Occupation"];
                if (!string.IsNullOrWhiteSpace(occ)) model.Member.Occupation = occ;
            }
            // Normalize based on EmploymentType
            if (string.Equals(model.Member.EmploymentType, "Student", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(model.Member.Occupation))
                    model.Member.Occupation = "Student";
                model.Member.Salaried_Location = "-";
                model.Member.Business_Name = "-";
                model.Member.Business_Location = "-";
            }
            else if (string.Equals(model.Member.EmploymentType, "Self", StringComparison.OrdinalIgnoreCase))
            {
                // Self-employed: hide salaried details
                model.Member.Salaried_Location = "-";
            }
            else if (string.Equals(model.Member.EmploymentType, "Salaried", StringComparison.OrdinalIgnoreCase))
            {
                // Salaried: hide business details
                model.Member.Business_Name = string.IsNullOrWhiteSpace(model.Member.Business_Name) ? "-" : model.Member.Business_Name;
                model.Member.Business_Location = string.IsNullOrWhiteSpace(model.Member.Business_Location) ? "-" : model.Member.Business_Location;
            }

            try
            {
                // Set RegstrId default value
                model.Member.RegstrId = "1";
                // Set MemberNo as next available integer
                //model.MemberNo = GenerateMemberNumber();
                // Set MemberDNo as registeryear + dob year + "000" + MemberNo
                //var registerYear = DateTime.Now.Year.ToString();
                //var dobYear = model.Member_DOB.Year.ToString();
                //var memberNoStr = model.MemberNo.ToString().PadLeft(3, '0');
                //model.MemberDNo = $"{registerYear}{dobYear}{memberNoStr}";
                model.Member.DispStatus = 1;
                model.Member.CreatedBy = User.Identity.Name;
                model.Member.CreatedDateTime = DateTime.Now;
                model.Member.Member_Age = CalculateAge(model.Member.Member_DOB).ToString();
                model.Member.Member_Reg_Date = DateTime.Now;
                model.Member.Member_Sdate = DateTime.Now;
                model.Member.Member_Edate = DateTime.Now.AddYears(1);
                if (MaritalStatus != "Married")
                {
                    model.Member.Spouse_Name = null;
                    model.Member.Spouse_DOB = null;
                    model.Member.Date_Of_Marriage = null;
                    model.Member.Total_Children = 0;
                }
                else
                {
                    if (model.Member.Total_Children < 0)
                    {
                        model.Member.Total_Children = 0;
                    }
                }
                // Remove non-editable/system-populated fields (use correct prefixed keys)
                ModelState.Remove("Member.UIserID");
                ModelState.Remove("Member.MemberDNo");
                ModelState.Remove("Member.CreatedBy");
                ModelState.Remove("Member.RegstrId");
                ModelState.Remove("Member.MemberNo");
                ModelState.Remove("MaritalStatus");

                // Remove ModelState errors for required fields we set server-side (use prefixed keys)
                ModelState.Remove("Member.MemberDNo");
                ModelState.Remove("Member.Member_Name");
                ModelState.Remove("Member.Member_Age");
                ModelState.Remove("Member.Member_Addr1");
                ModelState.Remove("Member.Member_Pincode");
                ModelState.Remove("Member.Member_Country");
                ModelState.Remove("Member.Member_Per_Addr1");
                ModelState.Remove("Member.Member_Per_Pincode");
                ModelState.Remove("Member.Member_Per_Country");
                ModelState.Remove("Member.CreatedBy");
                ModelState.Remove("Member.Reference_By");
                ModelState.Remove("Member.Reference_Contact_No");

                if (ModelState.IsValid)
                {
                    using (var transaction = _db.Database.BeginTransaction())
                    {
                        try
                        {
                            // Check if record exists for this user
                            var existing = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == model.Member.UIserID);
                            if (existing != null)
                            {
                                existing.BldGID = model.Member.BldGID;
                                //existing.UIserID = model.UIserID;
                                //existing.RegstrId = model.RegstrId;
                                //existing.MemberNo = model.MemberNo;
                                //existing.MemberDNo = model.MemberDNo;
                                //existing.Member_Reg_Date = model.Member_Reg_Date;
                                existing.Member_Name = model.Member.Member_Name;
                                // Allow updating Gender from the form
                                existing.Gender = model.Member.Gender;
                                // Allow updating DOB from the form
                                existing.Member_DOB = model.Member.Member_DOB;
                                existing.Member_Age = model.Member.Member_Age;
                                //existing.Member_Mobile_No = model.Member.Member_Mobile_No;
                                existing.Member_WhatsApp_No = model.Member.Member_WhatsApp_No;
                                existing.Member_Landline_No = model.Member.Member_Landline_No;
                                //existing.Member_EmailID = model.Member.Member_EmailID;
                                existing.Member_Addr1 = model.Member.Member_Addr1;
                                existing.Member_Addr2 = model.Member.Member_Addr2;
                                existing.Member_City = model.Member.Member_City;
                                existing.Member_Area = model.Member.Member_Area;
                                existing.StateID = model.Member.StateID;
                                existing.Member_Pincode = model.Member.Member_Pincode;
                                existing.Member_Country = model.Member.Member_Country;
                                existing.Member_Per_Addr1 = model.Member.Member_Per_Addr1;
                                existing.Member_Per_Addr2 = model.Member.Member_Per_Addr2;
                                existing.Member_Per_City = model.Member.Member_Per_City;
                                existing.Member_Per_Area = model.Member.Member_Per_Area;
                                existing.PStateID = model.Member.PStateID;
                                existing.Member_Per_Pincode = model.Member.Member_Per_Pincode;
                                existing.Member_Per_Country = model.Member.Member_Per_Country;
                                existing.Member_Photo_Path = model.Member.Member_Photo_Path;
                                //existing.Member_Sdate = model.Member_Sdate;
                                //existing.Member_Edate = model.Member_Edate;
                                existing.Family_Photo_Path = model.Member.Family_Photo_Path;
                                existing.Member_Nativity = model.Member.Member_Nativity;

                                // Employment
                                existing.EmploymentType = model.Member.EmploymentType;
                                existing.Salaried_Location = model.Member.Salaried_Location;

                                // Spouse Details
                                existing.Spouse_Name = model.Member.Spouse_Name;
                                existing.Spouse_DOB = model.Member.Spouse_DOB;
                                existing.Date_Of_Marriage = model.Member.Date_Of_Marriage;
                                existing.Total_Children = model.Member.Total_Children;

                                // Professional details
                                existing.Business_Name = model.Member.Business_Name;
                                existing.Business_Location = model.Member.Business_Location;
                                existing.Occupation = model.Member.Occupation;

                                // Other Activities
                                existing.Interests_and_Hobbies = model.Member.Interests_and_Hobbies;
                                existing.Reference_By = model.Member.Reference_By;
                                existing.Reference_Contact_No = model.Member.Reference_Contact_No;

                                // Stamp modification metadata
                                existing.ModifiedBy = User.Identity.Name;
                                existing.ModifiedDateTime = DateTime.Now;
                            }
                            else
                            {
                                // For new records, initialize modification metadata same as created
                                model.Member.ModifiedBy = User.Identity.Name;
                                model.Member.ModifiedDateTime = DateTime.Now;
                                _db.MemberShipMasters.Add(model.Member);
                            }
                            _db.SaveChanges();

                            // Log after save
                            try
                            {
                                var saved = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == model.Member.UIserID);
                                System.IO.File.AppendAllText(Server.MapPath("~/App_Data/debug.log"),
                                    $"[POST Save] User={User.Identity.Name} MemberID={saved?.MemberID} Created=\"{saved?.CreatedDateTime}\" Modified=\"{saved?.ModifiedDateTime}\"\n");
                            }
                            catch { }

                            // 2. Use the correct MemberID for child records
                            int memberId = existing != null ? existing.MemberID : model.Member.MemberID;

                            // Handle photo uploads now that we have a confirmed MemberID and folder
                            if (Photo != null && Photo.ContentLength > 0)
                            {
                                string memberPhotoFolder = Server.MapPath($"~/Uploads/MemberPhotos/{memberId}/image");
                                Directory.CreateDirectory(memberPhotoFolder);
                                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(Photo.FileName)}";
                                string filePath = Path.Combine(memberPhotoFolder, uniqueFileName);
                                Photo.SaveAs(filePath);
                                var memberEntity = _db.MemberShipMasters.Find(memberId);
                                if (memberEntity != null)
                                {
                                    memberEntity.Member_Photo_Path = $"/Uploads/MemberPhotos/{memberId}/image/{uniqueFileName}";
                                    _db.SaveChanges();
                                }
                            }
                            if (FamilyPhoto != null && FamilyPhoto.ContentLength > 0)
                            {
                                string familyPhotoFolder = Server.MapPath($"~/Uploads/FamilyPhotos/{memberId}");
                                Directory.CreateDirectory(familyPhotoFolder);
                                string uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(FamilyPhoto.FileName)}";
                                string filePath = Path.Combine(familyPhotoFolder, uniqueFileName);
                                FamilyPhoto.SaveAs(filePath);
                                var memberEntity = _db.MemberShipMasters.Find(memberId);
                                if (memberEntity != null)
                                {
                                    memberEntity.Family_Photo_Path = $"/Uploads/FamilyPhotos/{memberId}/{uniqueFileName}";
                                    _db.SaveChanges();
                                }
                            }

                            // 3. Remove old family details
                            var oldFamilyDetails = _db.MemberShipFamilyDetails.Where(f => f.MemberID == memberId).ToList();
                            foreach (var old in oldFamilyDetails)
                                _db.MemberShipFamilyDetails.Remove(old);
                            _db.SaveChanges();

                            // 4. Add new family details
                            if (MaritalStatus == "Married" && model.Member.Total_Children > 0)
                            {
                                for (int i = 0; i < model.Member.Total_Children; i++)
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
                                            MemberID = memberId, // <-- Use the correct MemberID here!
                                            ModifiedBy = User.Identity.Name,
                                            ModifiedDateTime = DateTime.Now
                                        };
                                        _db.MemberShipFamilyDetails.Add(child);
                                    }
                                }
                                _db.SaveChanges();
                            }

                            // Remove old ODetails for this member
                            var oldODetails = _db.MemberShipODetails.Where(o => o.MemberID == memberId).ToList();
                            foreach (var old in oldODetails)
                                _db.MemberShipODetails.Remove(old);
                            _db.SaveChanges();

                            // Add new ODetails using the correct MemberID
                            var orgNames = Request.Form.GetValues("MemberIn[]");
                            var sinceYears = Request.Form.GetValues("Since[]");
                            var currDesigs = Request.Form.GetValues("CurrentDesignation[]");

                            if (orgNames != null)
                            {
                                for (int i = 0; i < orgNames.Length; i++)
                                {
                                    if (!string.IsNullOrWhiteSpace(orgNames[i]) && !string.IsNullOrWhiteSpace(currDesigs[i]) && int.TryParse(sinceYears[i], out int sinceYear))
                                    {
                                        var odetail = new MemberShipODetail
                                        {
                                            MemberID = memberId, // <-- Use the correct MemberID here!
                                            OrganizationName = orgNames[i],
                                            SinceYear = sinceYear,
                                            CurrentDesignation = currDesigs[i],
                                            ModifiedBy = User.Identity.Name,
                                            ModifiedDateTime = DateTime.Now
                                        };
                                        _db.MemberShipODetails.Add(odetail);
                                    }
                                }
                                _db.SaveChanges();
                            }

                            transaction.Commit();
                            TempData["SuccessMessage"] = "Member profile saved successfully!";
                            return RedirectToAction("Create");
                        }
                        catch (DbEntityValidationException ex)
                        {
                            transaction.Rollback();
                            foreach (var validationErrors in ex.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                                    System.Diagnostics.Debug.WriteLine($"Validation Error: {validationError.PropertyName} - {validationError.ErrorMessage}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            var errorMsg = ex.Message;
                            if (ex.InnerException != null)
                            {
                                errorMsg += " | Inner: " + ex.InnerException.Message;
                                if (ex.InnerException.InnerException != null)
                                    errorMsg += " | Inner2: " + ex.InnerException.InnerException.Message;
                            }
                            ModelState.AddModelError("", $"Error: {errorMsg}");
                            System.Diagnostics.Debug.WriteLine($"Exception: {errorMsg}");
                            // Optionally, write to your debug.log file:
                            System.IO.File.AppendAllText(Server.MapPath("~/App_Data/debug.log"), $"ERROR: {errorMsg}\n");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ModelState is invalid:");
                    foreach (var error in ModelState.Where(ms => ms.Value.Errors.Any()))
                    {
                        System.Diagnostics.Debug.WriteLine($"{error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Unexpected error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Unexpected Exception: {ex.ToString()}");
            }

            PopulateViewBags();
            // Re-fetch member from DB to ensure audit fields reflect database truth
            var freshMember = _db.MemberShipMasters.FirstOrDefault(m => m.UIserID == model.Member.UIserID) ?? model.Member;
            var viewModel = new MemberProfileViewModel
            {
                Member = freshMember,
                Children = _db.MemberShipFamilyDetails.Where(f => f.MemberID == freshMember.MemberID).ToList(),
                OrganizationDetails = _db.MemberShipODetails.Where(o => o.MemberID == freshMember.MemberID).ToList()
            };
            return View(viewModel);
        }

        //private int GenerateMemberNumber()
        //{
        //    return _db.MemberShipMasters.Max(m => (int?)m.MemberNo) + 1 ?? 1000;
        //}

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
        }

        private int CalculateAge(DateTime dob)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;
            return age;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult ViewLog()
        {
            string logPath = Server.MapPath("~/App_Data/debug.log");
            return Content(System.IO.File.ReadAllText(logPath));
        }

       
    }
}
