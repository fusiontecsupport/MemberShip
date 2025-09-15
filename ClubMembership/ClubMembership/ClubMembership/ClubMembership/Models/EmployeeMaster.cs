using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClubMembership.Models
{
    [Table("EMPLOYEEMASTER")]
    public class EmployeeMaster
    {
        [Key]
        public int CATEID { get; set; }
        [Required(ErrorMessage = "Please Enter numeric or Alphanumeric string")]
        [Remote("ValidateEMPCATENAME", "Common", AdditionalFields = "i_CATENAME", ErrorMessage = "This is already used.")]
        public string CATENAME { get; set; }
        //[Required(ErrorMessage = "Please Enter numeric or Alphanumeric string")]
        //[Remote("ValidateEMPCATECODE", "Common", AdditionalFields = "i_CATECODE", ErrorMessage = "This is already used.")]
        //public string CATEDNAME { get; set; }
        public string CATECODE { get; set; }
        public string CATEADDR1 { get; set; }
        public string CATEADDR2 { get; set; }
        public string CATEADDR3 { get; set; }
        public string CATEADDR4 { get; set; }
       // public string CATEADDR5 { get; set; }
        public string CATEPHN1 { get; set; }
        public string CATEPHN2 { get; set; }
        public string CATEPHN3 { get; set; }
        public string CATEPHN4 { get; set; }
        public string CATEPNAME { get; set; }
        public string CATEMAIL { get; set; }
        public Nullable<DateTime> CATEDOJ { get; set; }
        public int CATENO { get; set; }
        public Int16 CATEGTYPE { get; set; }
        public int LOCTID { get; set; }
        public string CATEAUTRNO { get; set; }
        public string CATEDRVLSNO { get; set; }


        public int DEPTID { get; set; }
        public int DSGNID { get; set; }
        public int STATEID { get; set; }
        public string CATE_DSGNDESC { get; set; }
        public string CUSRID { get; set; }
        public int LMUSRID { get; set; }
        public short DISPSTATUS { get; set; }
        public DateTime PRCSDATE { get; set; }
        public IEnumerable<string> emplfile { get; set; }
        public int BRNCHID { get; set; }
    }
}