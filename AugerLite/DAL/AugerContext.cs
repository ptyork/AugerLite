using Auger.Models;
using Auger.Models.Data;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace Auger.DAL
{
    public class AugerContext : IdentityDbContext<ApplicationUser>
    {
        public AugerContext()
            : base("name=DefaultConnection", throwIfV1Schema: false)
        {
            Database.SetInitializer<AugerContext>(new MigrateDatabaseToLatestVersion<AugerContext, Auger.Migrations.Configuration>());
        }
        
        public static AugerContext Create()
        {
            return new AugerContext();
        }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        // LTI Library Tables
        public virtual DbSet<LtiConsumer> LtiConsumers { get; set; }
        public virtual DbSet<LtiProviderRequest> LtiProviderRequests { get; set; }
        public virtual DbSet<LtiOutcome> LtiOutcomes { get; set; }

        // Auger-specific Tables
        public virtual DbSet<Course> Courses { get; set; }
        public virtual DbSet<Enrollment> Enrollments { get; set; }
        public virtual DbSet<Assignment> Assignments { get; set; }
        public virtual DbSet<StudentAssignment> StudentAssignments { get; set; }
        public virtual DbSet<StudentSubmission> StudentSubmissions { get; set; }
        public virtual DbSet<Page> Pages { get; set; }
        public virtual DbSet<Script> Scripts { get; set; }

        private void SetDates()
        {
            DateTime saveTime = DateTime.Now;
            foreach (var entry in this.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    var entity = entry.Entity as EntityBase;
                    if (entity != null)
                    {
                        if (entry.State == EntityState.Added)
                        {
                            entity.DateCreated = saveTime;
                        }
                        entity.DateModified = saveTime;
                    }
                }
            }
        }

        public override int SaveChanges()
        {
            SetDates();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync()
        {
            SetDates();
            return base.SaveChangesAsync();
        }

    }
}