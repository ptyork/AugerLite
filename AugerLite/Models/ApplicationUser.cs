using Auger.DAL;
using Auger.Models.Data;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;

namespace Auger.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public const string SystemRoleClaim = "auger.systemrole";

        public static ApplicationUserManager UserManager
        {
            get
            {
                return HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        public static ApplicationUser Current
        {
            get
            {
                var context = HttpContext.Current;
                if (context == null)
                {
                    return null;
                }

                ApplicationUser user = context.Session["User"] as ApplicationUser;
                if (user == null)
                {
                    try
                    {
                        var principal = context.User;
                        user = UserManager.FindById(principal?.Identity?.GetUserId());
                        context.Session["User"] = user;
                    }
                    catch (Exception ex)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                        return null;
                    }
                }
                return user;
            }
        }

        public static void InvalidateCurrent()
        {
            HttpContext.Current?.Session?.Remove("User");
        }

        public static ApplicationUser FromUserName(string userName)
        {
            try
            {
                var user = UserManager.FindByName(userName);
                return user;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return null;
            }
        }

        public static ApplicationUser FromUserId(string userId)
        {
            try
            {
                var user = UserManager.FindById(userId);
                return user;
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return null;
            }
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        private string _theme;
        public string Theme
        {
            get
            {
                return _theme ?? "dark";
            }
            set
            {
                _theme = value;
            }
        }

        public string FolderName
        {
            get
            {
                var username = this.UserName.Trim().ToLowerInvariant();
                return username;
            }
        }

        public string FullName
        {
            get
            {
                var fullname = string.Format("{0} {1}", FirstName, LastName).Trim();
                return string.IsNullOrEmpty(fullname) ? "n/a" : fullname;
            }
        }

        public virtual List<Enrollment> Enrollments { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            ApplicationUser.InvalidateCurrent();
            
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            if (await manager.IsInRoleAsync(userIdentity.GetUserId(), UserRoles.SuperUserRole))
            {
                userIdentity.AddClaim(new Claim(SystemRoleClaim, UserRoles.SuperUserRole));
            }
            else
            {
                var isInstructor = false;
                using (var db = new AugerContext())
                {
                    var enrollments = db.Enrollments.Where(e => e.UserId == this.Id);
                    foreach (var enrollment in enrollments)
                    {
                        if (enrollment.IsInRole(UserRoles.InstructorRole))
                        {
                            isInstructor = true;
                            break;
                        }
                    }
                }
                userIdentity.AddClaim(new Claim(SystemRoleClaim, isInstructor ? UserRoles.InstructorRole : UserRoles.LearnerRole));
            }

            _identity = userIdentity;
            return userIdentity;
        }

        public ClaimsIdentity GenerateUserIdentity(UserManager<ApplicationUser> manager)
        {
            ApplicationUser.InvalidateCurrent();

            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = manager.CreateIdentity(this, DefaultAuthenticationTypes.ApplicationCookie);

            if (manager.IsInRole(userIdentity.GetUserId(), UserRoles.SuperUserRole))
            {
                userIdentity.AddClaim(new Claim(SystemRoleClaim, UserRoles.SuperUserRole));
            }
            else
            {
                var isInstructor = false;
                using (var db = new AugerContext())
                {
                    var enrollments = db.Enrollments.Where(e => e.UserId == this.Id);
                    foreach (var enrollment in enrollments)
                    {
                        if (enrollment.IsInRole(UserRoles.InstructorRole))
                        {
                            isInstructor = true;
                            break;
                        }
                    }
                }
                userIdentity.AddClaim(new Claim(SystemRoleClaim, isInstructor ? UserRoles.InstructorRole : UserRoles.LearnerRole));
            }

            _identity = userIdentity;
            return userIdentity;
        }

        private ClaimsIdentity _identity = null;

        public ClaimsIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    return GenerateUserIdentity(UserManager);
                }
                else
                {
                    return _identity;
                }
            }
        }

        /// <summary>
        /// Use claims to determine if the user is in one of the specified roles.
        /// </summary>
        public bool IsInRole(params string[] roles)
        {
            var claims = Identity.Claims;
            var isInRole = claims.Any(c => c.Type == SystemRoleClaim && roles.Contains(c.Value));
            return isInRole;
        }

        public bool IsInstructorForCourse(Course course)
        {
            if (course.Enrollments == null || course.Enrollments.Count == 0)
            {
                return false;
            }
            var enrollment = course.Enrollments.FirstOrDefault(e => e.UserId == this.Id);
            if (enrollment == null)
            {
                return false;
            }
            return enrollment.IsActive && enrollment.Roles.Contains(UserRoles.InstructorRole, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}