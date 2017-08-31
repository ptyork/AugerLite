namespace Auger.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Models;
    using Models.Data;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Web;
    internal sealed class Configuration : DbMigrationsConfiguration<Auger.DAL.AugerContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            ContextKey = "Auger.DAL.AugerContext";
        }

        protected override void Seed(Auger.DAL.AugerContext context)
        {

            if (!(context.Users.Any()))
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);
                var role = new IdentityRole { Name = UserRoles.SuperUserRole };
                roleManager.Create(role);

                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                ApplicationUser user;

                user = new ApplicationUser
                {
                    UserName = "super@test.com",
                    FirstName = "Super",
                    LastName = "User",
                    Email = "super@test.com"
                };
                userManager.Create(user, "password");
                userManager.AddToRole(user.Id, UserRoles.SuperUserRole);

                user = new ApplicationUser
                {
                    UserName = "instructor@test.com",
                    FirstName = "Test",
                    LastName = "Instructor",
                    Email = "instructor@test.com"
                };
                userManager.Create(user, "password");

                user = new ApplicationUser
                {
                    UserName = "learner@test.com",
                    FirstName = "Test",
                    LastName = "Learner",
                    Email = "learner@test.com"
                };
                userManager.Create(user, "password");
            }

            if (!context.Courses.Any())
            {
                Course c = new Course
                {
                    CourseTitle = "This is a Test Course",
                    CourseLabel = "TestCourse",
                    IsActive = true
                };
                context.Courses.Add(c);
                context.SaveChanges();

                Assignment a;

                a = new Assignment
                {
                    Course = c,
                    DueDate = DateTime.Now.AddDays(1),
                    AssignmentName = "Test Assignment"
                };
                
                ApplicationUser u;
                Enrollment e;
                StudentAssignment sa;

                u = context.Users.Where(us => us.UserName == "instructor@test.com").FirstOrDefault();
                if (u != null)
                {
                    e = new Enrollment
                    {
                        User = u,
                        Course = c,
                        IsActive = true,
                        AllRoles = UserRoles.InstructorRole
                    };
                    context.Enrollments.Add(e);
                }

                u = context.Users.Where(us => us.UserName == "learner@test.com").FirstOrDefault();
                if (u != null)
                {
                    e = new Enrollment
                    {
                        User = u,
                        Course = c,
                        IsActive = true,
                        AllRoles = UserRoles.LearnerRole
                    };
                    context.Enrollments.Add(e);

                    sa = new StudentAssignment
                    {
                        Enrollment = e,
                        Assignment = a
                    };
                    context.StudentAssignments.Add(sa);
                }
                context.SaveChanges();

            }
        }

    }
}
