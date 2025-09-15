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
    [Table("MemberShip_PaymentDetails")]

    public class MemberShipPaymentDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentID { get; set; } 

        [Required]
        public int MemberID { get; set; } 

        [Required]
        public DateTime Payment_Date { get; set; } 

        [Required]
        [Column(TypeName = "datetime2")]
        public DateTime Renewal_Date { get; set; } 

        [Required]
        [StringLength(100)]
        public string UPI_ID { get; set; } 

        [Required]
        [StringLength(100)]
        public string RRN_NO { get; set; }

        [Required]
        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string Payment_Type { get; set; }

        [Required]
        [StringLength(20)]
        public string Payment_Status { get; set; } 

        [Required]
        [StringLength(50)]
        public string Payment_Plan { get; set; } 

        [Required]
        [StringLength(50)]
        public string Payment_Receipt_No { get; set; }

        // New mapped columns
        [Column("reciept_sno")]
        public int? ReceiptSerialNo { get; set; }

        [StringLength(50)]
        [Column("Reciept_Dno")]
        public string ReceiptDocumentNo { get; set; }

        [Column("compy_id")]
        public int? CompanyAccountingDetailId { get; set; }

        [Required]
        [Column("MemberTId")] 
        public int MemberTypeId { get; set; }

        [Required]
        //[Column("MemberTAmt", TypeName = "numeric(18,2)")]
        [Column("MemberTAmt", TypeName = "decimal")]
        public decimal MemberTypeAmount { get; set; }
    }
}