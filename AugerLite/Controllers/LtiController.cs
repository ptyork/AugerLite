using Auger;
using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using Auger.Models.View;
using LtiLibrary.Core.Common;
using LtiLibrary.Core.OAuth;
using LtiLibrary.AspNet.Extensions;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LtiLibrary.Core.Lti1;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Security.Claims;
using LtiLibrary.Core.Outcomes.v1;
using Microsoft.Owin.Security;

namespace Auger.Controllers
{
    public class LtiController : Controller
    {
        private AugerContext db = new AugerContext();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public LtiController()
        {
        }

        public LtiController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

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

        public const string AuthenticationType = "LTI";
        public const string ClaimType = "LtiRequest";

        [HttpPost]
        public async Task<ActionResult> Index()
        {
            Request.CheckForRequiredLtiParameters();

            var ltiRequest = new LtiRequest(null);
            ltiRequest.ParseRequest(Request);

            // Make sure the request is not being replayed
            var timeout = TimeSpan.FromMinutes(5);
            var oauthTimestampAbsolute = OAuthConstants.Epoch.AddSeconds(ltiRequest.Timestamp);
            if (DateTime.UtcNow - oauthTimestampAbsolute > timeout)
            {
                throw new LtiException("Expired " + OAuthConstants.TimestampParameter);
            }

            var consumerKey = ltiRequest.ConsumerKey;
            var consumer = await db.LtiConsumers.SingleOrDefaultAsync(c => c.Key == consumerKey);
            if (consumer == null)
            {
                throw new LtiException("Invalid " + OAuthConstants.ConsumerKeyParameter);
            }

            var signature = ltiRequest.GenerateSignature(consumer.Secret);
            if (!signature.EqualsIgnoreCase(ltiRequest.Signature))
            {
                throw new LtiException("Invalid " + OAuthConstants.SignatureParameter);
            }

            //
            // If we made it this far the request is valid
            //

            // Record the request for logging purposes and as reference for outcomes
            var providerRequest = new LtiProviderRequest
            {
                Received = DateTime.UtcNow,
                LtiRequest = JsonConvert.SerializeObject(ltiRequest, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
            };
            db.LtiProviderRequests.Add(providerRequest);
            db.SaveChanges();

            // Add claims
            var claims = new List<Claim>
            {
                new Claim("LtiProviderRequestId", providerRequest.LtiProviderRequestId.ToString()),
                new Claim("ContextId", ltiRequest.ContextId),
                new Claim("ContextTitle", ltiRequest.ContextTitle),
                new Claim("ContextLabel", ltiRequest.ContextLabel),
                new Claim("ResourceLinkId", ((IBasicLaunchRequest)ltiRequest).ResourceLinkId),
                new Claim("ResourceLinkTitle", ((IBasicLaunchRequest)ltiRequest).ResourceLinkTitle),
                new Claim("LtiRoles", ltiRequest.Roles)
            };

            // Outcomes can live a long time to give the teacher enough
            // time to grade the assignment. So they are stored in a separate table.
            var lisOutcomeServiceUrl = ((IOutcomesManagementRequest)ltiRequest).LisOutcomeServiceUrl;
            var lisResultSourcedid = ((IOutcomesManagementRequest)ltiRequest).LisResultSourcedId;
            if (!string.IsNullOrWhiteSpace(lisOutcomeServiceUrl) &&
                !string.IsNullOrWhiteSpace(lisResultSourcedid))
            {
                var outcome = await db.LtiOutcomes.SingleOrDefaultAsync(o =>
                    o.LtiConsumerId == consumer.LtiConsumerId
                    && o.LisResultSourcedId == lisResultSourcedid);

                if (outcome == null)
                {
                    outcome = new LtiOutcome
                    {
                        LtiConsumerId = consumer.LtiConsumerId,
                        LisResultSourcedId = lisResultSourcedid
                    };
                    db.LtiOutcomes.Add(outcome);
                    await db.SaveChangesAsync(); // Assign OutcomeId;
                }
                outcome.ContextTitle = ltiRequest.ContextTitle;
                outcome.ServiceUrl = lisOutcomeServiceUrl;
                await db.SaveChangesAsync();

                // Add the outcome ID as a claim
                claims.Add(new Claim("LtiOutcomeId", outcome.LtiOutcomeId.ToString()));
            }

            //
            // Generate User Name
            //
            string userName = ltiRequest.LisPersonEmailPrimary;
            if (string.IsNullOrEmpty(userName))
            {
                var anonName = string.Concat("anon-", ltiRequest.UserId);
                Uri url;
                if (string.IsNullOrEmpty(ltiRequest.ToolConsumerInstanceUrl) ||
                    !Uri.TryCreate(ltiRequest.ToolConsumerInstanceUrl, UriKind.Absolute, out url))
                {
                    userName = string.Concat(anonName, "@anon-", ltiRequest.ConsumerKey, ".lti");
                }
                else
                {
                    userName = string.Concat(anonName, "@", url.Host);
                }
            }

            //
            // Check to see if a user is currently logged in
            //
            var newUser = false;
            var user = ApplicationUser.Current;
            var loginProvider = string.Join(":", new[] { AuthenticationType, ltiRequest.ConsumerKey });
            var login = new UserLoginInfo(loginProvider, ltiRequest.UserId);
            if (user == null)
            {
                //
                // Not logged in. Find user by name
                //
                user = await UserManager.FindByNameAsync(userName);
                if (user == null)
                {
                    //
                    // Not found! Create new user!
                    //
                    newUser = true;
                    user = new ApplicationUser { UserName = userName, Email = userName };
                    var result = await UserManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        user.FirstName = ltiRequest.LisPersonNameGiven;
                        user.LastName = ltiRequest.LisPersonNameFamily;
                        await UserManager.UpdateAsync(user);
                    }
                    else
                    {
                        throw new LtiException("Unable to create User");
                    }
                }

                // Save the pairing between LTI user and application user
                await UserManager.AddLoginAsync(user.Id, login);
            }
            else
            {
                // A user is aready logged in. Make sure this is kosher.
                var loginUser = await UserManager.FindAsync(login);
                if (loginUser == null)
                {
                    // Add login to user
                    // TODO: This is probably not cool. Need to prompt the user
                    //       to determine if they want to login as a new user
                    //       or not.
                    await UserManager.AddLoginAsync(user.Id, login);
                }
                else if (user.UserName.EqualsIgnoreCase(loginUser.UserName))
                {
                    // Already logged in as this user...'tscool...just relogin with claims
                }
                else
                {
                    // TODO: Consider a "merge user" action here
                    throw new LtiException($"Your account ({loginUser.UserName}) is already associated with another user ({user.UserName}) on Auger.");
                }
            }

            //
            // By here, user exists in system and LTI request matches user
            // Create identity, add claims and log in.
            //
            var authType = DefaultAuthenticationTypes.ApplicationCookie;
            var identity = await user.GenerateUserIdentityAsync(UserManager);
            identity.AddClaim(
                new Claim(
                    ClaimType,
                    JsonConvert.SerializeObject(ltiRequest, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                    ClaimValueTypes.String,
                    authType)
            );
            if (identity != null && claims != null)
            {
                foreach (var claim in claims)
                {
                    identity.AddClaim(claim);
                }
            }
            SignInManager.AuthenticationManager.SignIn( new AuthenticationProperties { IsPersistent = false }, identity);

            //
            // Logged in. Let's do real work
            // Persist an ltiContext in the session because there are a
            // number of places that depend on it (mostly in CourseAdmin)
            //
            var ltiContext = new LtiContextModel();
            Session["ltiContext"] = ltiContext;

            if (string.IsNullOrWhiteSpace(ltiRequest.Roles))
            {
                // BAD LTI REQUEST (NO ROLES)
                // TODO: Make Good Error Page
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("No Roles in LTI Request"));
                return RedirectToAction("Index", "Home");
            }

            ltiContext.Roles = ltiRequest.Roles.Split(',');

            if (!ltiContext.IsLearner && !ltiContext.IsInstructor)
            {
                // BAD LTI REQUEST (INCORRECT ROLE)
                // TODO: Make Good Error Page
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("LTI Request Roles do not include Learner or Instructor"));
                return RedirectToAction("Index", "Home");
            }

            ltiContext.LtiContextId = ltiRequest.ContextId;
            if (string.IsNullOrWhiteSpace(ltiContext.LtiContextId))
            {
                // BAD LTI REQUEST (NO CONTEXT)
                // TODO: Make Good Error Page
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("No LTI ContextId"));
                return RedirectToAction("Index", "Home");
            }

            ltiContext.CourseTitle = ltiRequest.ContextTitle;
            ltiContext.CourseLabel = ltiRequest.ContextLabel;
            ltiContext.LtiResourceLinkId = ltiRequest.ResourceLinkId;
            ltiContext.AssignmentName = ltiRequest.ResourceLinkTitle;

            //
            // FIND, LINK OR CREATE A COURSE
            //
            var course = db.Courses.FirstOrDefault(c => c.LtiContextId == ltiContext.LtiContextId);

            if (course == null && ltiContext.IsInstructor)
            {
                // NEW COURSE
                var unlinkedCourses = db.Enrollments
                    .Where(e => e.UserId == user.Id && e.Course.LtiContextId == null)
                    .Select(e => e.Course);

                if (unlinkedCourses.Any())
                {
                    return RedirectToAction("CourseLink", "CourseAdmin");
                }
                else
                {
                    return RedirectToAction("CourseCreate", "CourseAdmin");
                }
            }

            if (course == null)
            {
                // NEW COURSE BUT NOT AN INSTRUCTOR
                // TODO: Make Good Error Page
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception($"Student accessing {ltiContext.CourseTitle} - {ltiContext.CourseTitle} but it isn't available yet in Auger"));
                return RedirectToAction("Index", "Home");
            }

            CookieManager.SetCourseId(course.CourseId);
            ltiContext.IsCourseLinked = true;

            //
            // CREATE OR UPDATE ENROLLMENTS
            //
            var enrollment = db.Enrollments.FirstOrDefault(e => e.CourseId == course.CourseId && e.UserId == user.Id);

            if (enrollment == null)
            {
                enrollment = new Enrollment
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    CourseId = course.CourseId,
                    AllRoles = string.Join(",", ltiContext.Roles),
                    IsActive = true
                };
                db.Enrollments.Add(enrollment);
                db.SaveChanges();
            }
            else
            {
                var isSuperUser = enrollment.IsInRole(UserRoles.SuperUserRole);
                enrollment.AllRoles = string.Join(",", ltiContext.Roles);
                if (isSuperUser) enrollment.Roles.Add(UserRoles.SuperUserRole);
                db.SaveChanges();
            }

            //
            // ADD ALL ASSIGNMENTS TO USER IF (S)HE IS NEW
            //
            if (newUser && !ltiContext.IsInstructor)
            {
                foreach (var assn in db.Assignments.Where(e => e.CourseId == enrollment.CourseId))
                {
                    db.StudentAssignments.Add(
                        new StudentAssignment
                        {
                            AssignmentId = assn.AssignmentId,
                            EnrollmentId = enrollment.EnrollmentId
                        }
                    );
                }
                db.SaveChanges();
            }

            //
            // REDIRECT HERE IF NO ASSIGNMENT SPECIFIED
            // HACK: At least for Canvas, the LitResourceLinkId is always
            //       provided. However, if it isn't an assignment, there's
            //       no LisOutcomeServiceUrl, so we'll use that instead.
            //
            //if (string.IsNullOrWhiteSpace(ltiContext.LtiResourceLinkId))
            if (string.IsNullOrWhiteSpace(lisOutcomeServiceUrl))
            {
                if (ltiContext.IsInstructor)
                {
                    return RedirectToAction("CourseDetails", "CourseAdmin", new { courseId = course.CourseId });
                }
                else
                {
                    return RedirectToAction("Index", "Assignment");
                }
            }

            //
            // FIND, LINK OR CREATE AN ASSIGNMENT
            //
            var assignment = db.Assignments.FirstOrDefault(a => a.CourseId == course.CourseId && a.LtiResourceLinkId == ltiContext.LtiResourceLinkId);

            if (assignment == null && ltiContext.IsInstructor)
            {
                var unlinkedAssignments = db.Assignments.Where(a => a.CourseId == course.CourseId && a.LtiResourceLinkId == null);

                if (unlinkedAssignments.Any())
                {
                    return RedirectToAction("AssignmentLink", "CourseAdmin", new { courseId = course.CourseId });
                }
                else
                {
                    return RedirectToAction("AssignmentCreate", "CourseAdmin", new { courseId = course.CourseId });
                }
            }

            if (assignment == null)
            {
                // NEW ASSIGNMENT BUT NOT AN INSTRUCTOR
                // TODO: Make Good Error Page
                Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception($"{user.UserName} attempted to access '{ltiContext.CourseTitle}/{ltiContext.AssignmentName}' before it was created."));
                return RedirectToAction("Index", "Assignment");
            }

            ltiContext.IsAssignmentLinked = true;

            if (ltiContext.IsInstructor)
            {
                return RedirectToAction("AssignmentDetails", "CourseAdmin", new { courseId = course.CourseId, id = assignment.AssignmentId });
            }
            else
            {
                // ADD A STUDENTASSIGNMENT IF NEEDED (SHOULD NEVER HAPPEN, BUT JUST IN CASE)
                var studentAssignment = db.StudentAssignments.FirstOrDefault(sa => sa.AssignmentId == assignment.AssignmentId && sa.EnrollmentId == enrollment.EnrollmentId);

                if (studentAssignment == null)
                {
                    studentAssignment = new StudentAssignment
                    {
                        EnrollmentId = enrollment.EnrollmentId,
                        AssignmentId = assignment.AssignmentId
                    };
                    db.StudentAssignments.Add(studentAssignment);
                    db.SaveChanges();
                }

                if (studentAssignment == null)
                {
                    // SHOULD NEVER HAPPEN
                    // TODO: Make Good Error Page
                    Elmah.ErrorSignal.FromCurrentContext().Raise(new Exception("Enable to find or create StudentAssignment..bad news"));
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return RedirectToAction("Details", "Assignment", new { id = assignment.AssignmentId });
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}