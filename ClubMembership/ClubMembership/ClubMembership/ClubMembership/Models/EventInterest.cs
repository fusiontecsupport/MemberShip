using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClubMembership.Models
{
    [Table("EVENTINTEREST")]
    public class EventInterest
    {
        [Key]
        public int EventInterestId { get; set; }

        [Index("IX_Event_User", 1, IsUnique = true)]
        public int EventId { get; set; }

        [Required]
        [StringLength(128)]
        [Index("IX_Event_User", 2, IsUnique = true)]
        public string UserId { get; set; }

        // 1 = Interested, -1 = Not Interested, 0 = None
        public short State { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}


