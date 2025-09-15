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
    [Table("MemberShip_ODetails")]
    public class MemberShipODetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("MemberOID")]
        public int MemberOID { get; set; }

        [Required]
        [Column("MemberID")]
        public int MemberID { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Organization_Name")]
        public string OrganizationName { get; set; }

        [Required]
        [Column("Since_Year")]
        public int SinceYear { get; set; }

        [Required]
        [StringLength(100)]
        [Column("Current_Designation")]
        public string CurrentDesignation { get; set; }

        [StringLength(100)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDateTime { get; set; }

        // Navigation property
        [ForeignKey("MemberID")]
        public virtual MemberShipMaster Member { get; set; }
    }
}