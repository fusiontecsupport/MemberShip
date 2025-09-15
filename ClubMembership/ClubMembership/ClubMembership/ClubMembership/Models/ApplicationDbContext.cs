using ClubMembership.Data;
using ClubMembership.Models;
// Add this using directive if AnnouncementMaster is in another namespace
// using ClubMembership.Entities; // <-- Uncomment and adjust if needed
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;

namespace ClubMembership.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<MenuMaster> MenuMasters { get; set; }
        public DbSet<RImageMaster> RImageMasters { get; set; }
        public DbSet<StateMaster> StateMasters { get; set; }
        public DbSet<BloodGroupMaster> BloodGroupMasters { get; set; }
        public DbSet<RegionMaster> RegionMasters { get; set; }
        public DbSet<MemberShipMaster> MemberShipMasters { get; set; }
        public DbSet<MemberShipFamilyDetail> MemberShipFamilyDetails { get; set; }
        public DbSet<MemberShipODetail> MemberShipODetails { get; set; }
        public DbSet<MemberShipPaymentDetail> MemberShipPaymentDetails { get; set; }
        public DbSet<MemberShipTypeMaster> MemberShipTypeMasters { get; set; }


        new public virtual IDbSet<ApplicationRole> Roles { get; set; }
        public virtual IDbSet<Group> Groups { get; set; }
        public DbSet<CompanyMaster> companymasters { get; set; }
        public DbSet<CompanyDetail> companydetails { get; set; }
        public DbSet<TMPRPT_IDS> TMPRPT_IDS { get; set; }
        public virtual IDbSet<VW_ACCOUNTING_YEAR_DETAIL_ASSGN> VW_ACCOUNTING_YEAR_DETAIL_ASSGN { get; set; }
        public virtual IDbSet<MenuRoleMaster> MenuRoleMasters { get; set; }
        public DbSet<SoftDepartmentMaster> softdepartmentmasters { get; set; }
        public virtual IDbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual IDbSet<AccountGroupMaster> accountgroupmasters { get; set; }
        public virtual IDbSet<AccountHeadMaster> accountheadmasters { get; set; }
        public virtual IDbSet<DisplayOrderMaster> displayordermasters { get; set; }
        public DbSet<EmployeeMaster> employeemasters { get; set; }
        public DbSet<EmployeeLinkMaster> employeelinkmasters { get; set; }
        public DbSet<AnnouncementMaster> announcementmasters { get; set; }
        public DbSet<EventMaster> eventmasters { get; set; }
        public DbSet<GalleryMaster> gallerymasters { get; set; }
        public DbSet<EventInterest> eventinterests { get; set; }
        public DbSet<MinutesOfMeeting> minutesofmeetings { get; set; }
        public DbSet<GovernmentProof> GovernmentProofs { get; set; }
        public ApplicationDbContext()
         : base("Club_DefaultConnection")
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException("modelBuilder");
            }

            //modelBuilder.Entity<TransactionMaster>().Property(d => d.TRANCRATE).HasPrecision(18, 4);

            // Keep this:
            modelBuilder.Entity<IdentityUser>().ToTable("AspNetUsers");

            // Change TUser to ApplicationUser everywhere else - IdentityUser and ApplicationUser essentially 'share' the AspNetUsers Table in the database:
            EntityTypeConfiguration<ApplicationUser> table =
                modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");

            table.Property((ApplicationUser u) => u.UserName).IsRequired();
            
            // Map the CateTid property to the database column
            table.Property((ApplicationUser u) => u.CateTid).HasColumnName("CateTid");

            // EF won't let us swap out IdentityUserRole for ApplicationUserRole here:
            modelBuilder.Entity<ApplicationUser>().HasMany<IdentityUserRole>((ApplicationUser u) => u.Roles);
            modelBuilder.Entity<IdentityUserRole>().HasKey((IdentityUserRole r) =>
                new { UserId = r.UserId, RoleId = r.RoleId }).ToTable("AspNetUserRoles");


            // Add the group stuff here:
            modelBuilder.Entity<ApplicationUser>().HasMany<ApplicationUserGroup>((ApplicationUser u) => u.Groups);
            modelBuilder.Entity<ApplicationUserGroup>().HasKey((ApplicationUserGroup r) => new { UserId = r.UserId, GroupId = r.GroupId }).ToTable("ApplicationUserGroups");

            // And here:
            modelBuilder.Entity<Group>().HasMany<ApplicationRoleGroup>((Group g) => g.Roles);
            modelBuilder.Entity<ApplicationRoleGroup>().HasKey((ApplicationRoleGroup gr) => new { RoleId = gr.RoleId, GroupId = gr.GroupId }).ToTable("ApplicationRoleGroups");

            // And Here:
            EntityTypeConfiguration<Group> groupsConfig = modelBuilder.Entity<Group>().ToTable("Groups");
            groupsConfig.Property((Group r) => r.Name).IsRequired();

            // Leave this alone:
            EntityTypeConfiguration<IdentityUserLogin> entityTypeConfiguration =
                modelBuilder.Entity<IdentityUserLogin>().HasKey((IdentityUserLogin l) =>
                    new { UserId = l.UserId, LoginProvider = l.LoginProvider, ProviderKey = l.ProviderKey }).ToTable("AspNetUserLogins");

            entityTypeConfiguration.HasRequired<IdentityUser>((IdentityUserLogin u) => u.User);
            EntityTypeConfiguration<IdentityUserClaim> table1 = modelBuilder.Entity<IdentityUserClaim>().ToTable("AspNetUserClaims");
            table1.HasRequired<IdentityUser>((IdentityUserClaim u) => u.User);

            // Add this, so that IdentityRole can share a table with ApplicationRole:
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");

            // Change these from IdentityRole to ApplicationRole:
            EntityTypeConfiguration<ApplicationRole> entityTypeConfiguration1 = modelBuilder.Entity<ApplicationRole>().ToTable("AspNetRoles");
            entityTypeConfiguration1.Property((ApplicationRole r) => r.Name).IsRequired();

        }

    }
}