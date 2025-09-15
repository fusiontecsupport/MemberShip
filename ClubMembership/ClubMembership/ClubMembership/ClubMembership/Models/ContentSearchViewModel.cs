using System.Collections.Generic;

namespace ClubMembership.Models
{
    public class ContentSearchViewModel
    {
        public string Query { get; set; }
        public List<AnnouncementMaster> Announcements { get; set; }
        public List<EventMaster> Events { get; set; }
    }
}


