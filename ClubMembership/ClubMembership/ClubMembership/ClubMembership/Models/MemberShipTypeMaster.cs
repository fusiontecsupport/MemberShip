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
    [Table("MemberShipTypeMaster")]

    public class MemberShipTypeMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("MemberTId")]
        public int MemberTypeId { get; set; }  // Using more readable property name

        [Required]
        [StringLength(50)]
        [Column("MemberTCode")]
        public string MemberTypeCode { get; set; }

        [Required]
        [StringLength(100)]
        [Column("MemberTDesc")]
        public string MemberTypeDescription { get; set; }

        [Required]
        //[Column("MemberTAmt", TypeName = "numeric(18,2)")]
        [Column("MemberTAmt", TypeName = "decimal")]
        public decimal MembershipFee { get; set; }  // More descriptive name

        [Required]
        [StringLength(100)]
        [Column("CUsrId")]
        public string CreatedBy { get; set; }  // Better property naming

        [Required]
        [StringLength(100)]
        [Column("LMUsrId")]
        public string LastModifiedBy { get; set; }

        [Required]
        [Column("DispStatus")]
        public short DisplayStatus { get; set; }

        [Required]
        [Column("PrcsDate")]
        public DateTime ProcessDate { get; set; } = DateTime.Now;  // Default value

        [Required]
        [Column("No_Of_Years")]
        public int NoOfYears { get; set; }

    }
}