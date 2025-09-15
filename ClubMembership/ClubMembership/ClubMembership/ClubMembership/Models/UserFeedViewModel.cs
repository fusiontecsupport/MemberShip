using System;
using System.Collections.Generic;

namespace ClubMembership.Models
{
    public class UserFeedViewModel
    {
        public List<UserFeedItem> Items { get; set; } = new List<UserFeedItem>();
    }

    public class UserFeedItem
    {
        public string Type { get; set; } // Announcement | Event | Gallery
        public int Id { get; set; }
        public string Heading { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string MainImage { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public string AdditionalImagesCsv { get; set; }
        public int LikeCount { get; set; }
    }
}


