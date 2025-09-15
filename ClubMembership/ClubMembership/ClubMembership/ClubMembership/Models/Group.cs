using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClubMembership.Models
{
    public class Group
    {
        public Group()
        {
            Roles = new List<ApplicationRoleGroup>();
        }


        public Group(string name) : this()
        {
            Name = name;
        }


        [Key]
        [Required]
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }
        public virtual ICollection<ApplicationRoleGroup> Roles { get; set; }
    }
}