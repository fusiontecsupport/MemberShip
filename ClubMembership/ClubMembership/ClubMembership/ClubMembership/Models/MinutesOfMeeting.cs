using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace ClubMembership.Models
{
    [Table("mins_of_meeting")]
    public class MinutesOfMeeting
    {
        [Key]
        [Column("mom_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int MomId { get; set; }

        [DisplayName("Heading")]
        [StringLength(200)]
        public string Heading { get; set; }

        [DisplayName("Caption")]
        [StringLength(500)]
        public string Caption { get; set; }

        [DisplayName("Description")]
        [StringLength(4000)]
        public string Description { get; set; }

        [DisplayName("Meeting Date and Time")]
        [Column("Meeting_Date_and_Time")]
        public DateTime? MeetingDateAndTime { get; set; }

        [DisplayName("Meeting Place")]
        [Column("Meeting_place")]
        [StringLength(200)]
        public string MeetingPlace { get; set; }

        [DisplayName("Conducted By")]
        [Column("Conducted_by")]
        [StringLength(200)]
        public string ConductedBy { get; set; }

        [DisplayName("Organized By")]
        [Column("Organized_by")]
        [StringLength(200)]
        public string OrganizedBy { get; set; }

        [DisplayName("Members Attended")]
        [Column("Members_attended")]
        [StringLength(1000)]
        public string MembersAttended { get; set; }

        [DisplayName("Category Type Ids")]
        [StringLength(200)]
        public string CategoryTypeIds { get; set; }

        [DisplayName("Members Invited")]
        [Column("Members_invited")]
        [StringLength(1000)]
        public string MembersInvited { get; set; }

        // Helper similar to Announcement: parsed CategoryTypeIds
        [NotMapped]
        public List<int> CategoryTypeIdList
        {
            get
            {
                if (!string.IsNullOrEmpty(CategoryTypeIds))
                {
                    return CategoryTypeIds.Split(',')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(int.Parse)
                        .ToList();
                }
                return new List<int>();
            }
            set
            {
                CategoryTypeIds = value != null ? string.Join(",", value) : "";
            }
        }

        // Attachments (paths to uploaded files)
        [StringLength(500)]
        public string Attachment1Path { get; set; }

        [StringLength(500)]
        public string Attachment2Path { get; set; }

        [StringLength(500)]
        public string Attachment3Path { get; set; }
    }
}
