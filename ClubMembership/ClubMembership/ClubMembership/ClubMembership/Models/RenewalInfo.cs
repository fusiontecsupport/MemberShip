using System;

namespace ClubMembership.Models
{
    public class RenewalInfo
    {
        public int MemberID { get; set; }
        public string MemberName { get; set; }
        public DateTime RenewalDate { get; set; }
        public int DaysRemaining { get; set; }
    }
}


