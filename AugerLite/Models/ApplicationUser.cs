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
                try
                {
                    var principal = HttpContext.Current.User;
                    var user = UserManager.FindById(principal?.Identity?.GetUserId());
                    return user;
                }
                catch (Exception ex)
                {
                    Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                    return null;
                }
            }
        }

        public static ApplicationUser FromUserName(string userName)
        {
            try
            {
                var user = UserManager.FindByEmail(userName);
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

            return userIdentity;
        }
    }
}