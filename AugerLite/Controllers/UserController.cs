using Auger.Models;
using Auger.Models.View;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Auger.Controllers
{
    public class UserController : Controller
    {
        public UserController() {}

        public UserController(ApplicationUserManager userManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationRoleManager _roleManager;
        public ApplicationRoleManager RoleManager
        {
            get
            {
                return this._roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }

        //
        // GET: /User/
        [Authorize(Roles = UserRoles.SuperUserRole)]
        public ActionResult Index()
        {
            return View(UserManager.Users.OrderBy(u => u.UserName).ToList());
        }

        //
        // GET: /User/Details/5
        [Authorize(Roles = UserRoles.SuperUserRole)]
        public ActionResult Details(string id)
        {
            var user = UserManager.FindById(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        //
        // GET: /User/EditSelf
        [Authorize]
        public ActionResult EditSelf(string returnUrl)
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            var model = new UserProfileModel(user);
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /User/EditSelf
        [Authorize]
        [HttpPost]
        public async Task<ActionResult> EditSelf(UserProfileModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(model.UserId);
                if (User.Identity.GetUserId() != user.Id)
                {
                    return new HttpUnauthorizedResult();
                }
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Theme = model.Theme;

                UserManager.Update(user);

                var claimsIdentity = await user.GenerateUserIdentityAsync(UserManager);
                Request.GetOwinContext().Authentication.SignIn(claimsIdentity);

                return RedirectToLocal(returnUrl);
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }


        //
        // GET: /User/Edit/5
        [Authorize(Roles = UserRoles.SuperUserRole)]
        public ActionResult Edit(string id)
        {
            var user = UserManager.FindById(id);
            var model = new UserProfileModel(user);
            //model.IsStudent = UserManager.IsInRole(user.Id, UserRoles.StudentRole);
            //model.IsTeacher = UserManager.IsInRole(user.Id, UserRoles.TeacherRole);
            return View(model);
        }

        //
        // POST: /User/Edit/5
        [Authorize(Roles = UserRoles.SuperUserRole)]
        [HttpPost]
        public ActionResult Edit(UserProfileModel model)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindById(model.UserId);
                if (!User.IsInRole(UserRoles.SuperUserRole) && User.Identity.GetUserId() != user.Id)
                {
                    return new HttpUnauthorizedResult();
                }
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                UserManager.Update(user);

                //UpdateUserRole(user.Id, UserRoles.StudentRole, model.IsStudent);
                //UpdateUserRole(user.Id, UserRoles.TeacherRole, model.IsTeacher);

                if (User.Identity.GetUserId() == user.Id)
                {
                    var claimsIdentity = UserManager.CreateIdentity(user, DefaultAuthenticationTypes.ApplicationCookie);
                    Request.GetOwinContext().Authentication.SignIn(claimsIdentity);
                }

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        private void UpdateUserRole(string userId, string roleName, bool userIsInRole)
        {
            Debug.WriteLine("{0} is in role {1}: {2}", userId, roleName, UserManager.IsInRole(userId, roleName));
            if (userIsInRole)
            {
                if (!UserManager.IsInRole(userId, roleName))
                    UserManager.AddToRole(userId, roleName);
            }
            else
            {
                if (UserManager.IsInRole(userId, roleName))
                    UserManager.RemoveFromRole(userId, roleName);
            }
        }

        //
        // GET: /User/Delete/5
        [Authorize(Roles = UserRoles.SuperUserRole)]
        public ActionResult Delete(string id)
        {
            var user = UserManager.FindById(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        //
        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = UserRoles.SuperUserRole)]
        public ActionResult DeleteConfirmed(string id)
        {
            var user = UserManager.FindById(id);

            // Remove this user from any roles they may have
            foreach (var role in user.Roles)
            {
                UserManager.RemoveFromRole(id, role.RoleId);
            }

            // Remove the user's account
            UserManager.Delete(user);

            return RedirectToAction("Index");
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }


    }
}