using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubMembership.Models
{
    [Table("MemberShip_FDetails")]
    public class MemberShipFamilyDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MemberFID { get; set; }

        [Required]
        [ForeignKey("Member")]
        public int MemberID { get; set; }

        [Required]
        [StringLength(100)]
        public string Child_Name { get; set; }

        [Required]
        public DateTime Child_DOB { get; set; }

        [Required]
        [StringLength(15)]
        public string Child_Age { get; set; }

        [Required]
        public int Child_Gender { get; set; }

        [Required]
        public int Child_Current_Position { get; set; }

        [Required]
        public int Child_MaritalStatus { get; set; }

        [StringLength(100)]
        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDateTime { get; set; }

        public virtual MemberShipMaster Member { get; set; }
    }
}