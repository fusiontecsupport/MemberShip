using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubMembership.Models
{
    [Table("govrmnet_proof")]
    public class GovernmentProof
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("gp_id")]
        public int Id { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Government Proof Path")]
        [Column("gov_path")]
        public string GovPath { get; set; }

        // UploadDate column doesn't exist in database yet
        // [Display(Name = "Upload Date")]
        // public DateTime? UploadDate { get; set; }

        // Navigation property to MemberShipMaster
        [ForeignKey("MemberID")]
        public virtual MemberShipMaster MemberShipMaster { get; set; }
    }
}
