using ClubMembership.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClubMembership.Models
{
    public class Company
    {
        public List<CompanyMaster> masterdata { get; set; }
        public List<CompanyDetail> detaildata { get; set; }
        //public List<pr_CompanyDetail_Flx_Assgn_Result> queryresultdata { get; set; }
    }
}