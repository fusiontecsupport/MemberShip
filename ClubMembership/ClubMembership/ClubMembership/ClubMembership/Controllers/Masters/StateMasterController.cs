using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity.Validation;
using System.Diagnostics;
using ClubMembership.Models;
using ClubMembership.Data;
using System.Configuration;

namespace ClubMembership.Controllers.Masters
{
    [SessionExpire]
    public class StateMasterController : Controller
    {
        // GET: StateMaster
        ApplicationDbContext context = new ApplicationDbContext();

        [Authorize(Roles = "StateMasterIndex")]
        public ActionResult Index()
        {
            return View(context.StateMasters.ToList());//Loading Grid
        }
        public JsonResult GetAjaxData(JQueryDataTableParamModel param)
        {
            using (var e = new ClubMembershipDBEntities())
            {
                var totalRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("TotalRowsCount", typeof(int));
                var filteredRowsCount = new System.Data.Entity.Core.Objects.ObjectParameter("FilteredRowsCount", typeof(int));
                var data = e.pr_SearchStateMaster(param.sSearch,
                                                Convert.ToInt32(Request["iSortCol_0"]),
                                                Request["sSortDir_0"],
                                                param.iDisplayStart,
                                                param.iDisplayStart + param.iDisplayLength,
                                                totalRowsCount,
                                                filteredRowsCount);

                //var aaData = data.Select(d => new string[] { d.STATECODE, d.STATEDESC, d.STATETYPE, d.DISPSTATUS, d.STATEID.ToString() }).ToArray();
                var aaData = data.Select(d => new { STATECODE = d.STATECODE, STATEDESC = d.STATEDESC, STATETYPE = d.STATETYPE.ToString(), DISPSTATUS = d.DISPSTATUS.ToString(), STATEID = d.STATEID.ToString() }).ToArray();
                return Json(new
                {
                    //sEcho = param.sEcho,
                    data = aaData
                    //iTotalRecords = Convert.ToInt32(totalRowsCount.Value),
                    //iTotalDisplayRecords = Convert.ToInt32(filteredRowsCount.Value)
                }, JsonRequestBehavior.AllowGet);
            }
        }
        //----------------------Initializing Form--------------------------//
        [Authorize(Roles = "StateMasterCreate")]
        public ActionResult Form(int? id = 0)
        {
            StateMaster tab = new StateMaster();
            tab.STATEID = 0;

            List<SelectListItem> selectedDISPSTATUS = new List<SelectListItem>();
            SelectListItem selectedItem = new SelectListItem { Text = "Disabled", Value = "1", Selected = false };
            selectedDISPSTATUS.Add(selectedItem);
            selectedItem = new SelectListItem { Text = "Enabled", Value = "0", Selected = true };
            selectedDISPSTATUS.Add(selectedItem);
            ViewBag.DISPSTATUS = selectedDISPSTATUS;

            List<SelectListItem> selectedSTATETYPE = new List<SelectListItem>();
            SelectListItem selectedSType = new SelectListItem { Text = "InterState", Value = "1", Selected = false };
            selectedSTATETYPE.Add(selectedSType);
            selectedSType = new SelectListItem { Text = "Local", Value = "0", Selected = true };
            selectedSTATETYPE.Add(selectedSType);
            ViewBag.STATETYPE = selectedSTATETYPE;

            ViewBag.REGNID = new SelectList(context.RegionMasters, "REGNID", "REGNDESC");

            // IMP
            if (id == -1)
                ViewBag.msg = "<div class='msg'>Record Successfully Saved</div>";
            if (id != 0 && id != -1)  // IMP
            {
                tab = context.StateMasters.Find(id);

                List<SelectListItem> selectedDISPSTATUS1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.DISPSTATUS) == 1)
                {
                    SelectListItem selectedItem31 = new SelectListItem { Text = "Disabled", Value = "1", Selected = true };
                    selectedDISPSTATUS1.Add(selectedItem31);
                    selectedItem31 = new SelectListItem { Text = "Enabled", Value = "0", Selected = false };
                    selectedDISPSTATUS1.Add(selectedItem31);
                    ViewBag.DISPSTATUS = selectedDISPSTATUS1;
                }

                List<SelectListItem> selectedSTATETYPE1 = new List<SelectListItem>();
                if (Convert.ToInt32(tab.STATETYPE) == 1)
                {
                    SelectListItem selectedSType31 = new SelectListItem { Text = "InterState", Value = "1", Selected = true };
                    selectedSTATETYPE1.Add(selectedSType31);
                    selectedSType31 = new SelectListItem { Text = "Local", Value = "0", Selected = false };
                    selectedSTATETYPE1.Add(selectedSType31);
                    ViewBag.STATETYPE = selectedSTATETYPE1;
                }

            }
            return View(tab);
        }//End of Form

        //--------------------------Insert or Modify data------------------------//
        [HttpPost]
        public void savedata(StateMaster tab)
        {
            tab.CUSRID = Session["CUSRID"].ToString();
            tab.LMUSRID = 1;
            tab.PRCSDATE = DateTime.Now;
            var s = tab.STATEDESC;//...ProperCase
            s = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());//end
            tab.STATEDESC = s;
            if ((tab.STATEID).ToString() != "0")
            {
                context.Entry(tab).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
            }
            else
            {
                context.StateMasters.Add(tab);
                context.SaveChanges();
            }

            // IMP
            if (Request.Form.Get("continue") == null)
            {
                Response.Redirect("index");
            }
            else
            {
                Response.Redirect("Form/-1");
            }
        }

        //------------------------Delete Record----------//
        [Authorize(Roles = "StateMasterDelete")]
        public void Del()
        {
            String id = Request.Form.Get("id");
            //    String fld = Request.Form.Get("fld");
            //    String temp = Delete_fun.delete_check1(fld, id);
            //    if (temp.Equals("PROCEED"))
            //    {
            StateMaster statemasters = context.StateMasters.Find(Convert.ToInt32(id));
            context.StateMasters.Remove(statemasters);
            context.SaveChanges();
            Response.Write("Deleted Successfully ...");
        }


    }
}