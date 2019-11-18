using Auger.DAL;
using Auger.Models;
using Auger.Models.Data;
using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using System.Collections;
using System.Web.SessionState;

namespace Auger.Controllers
{
    [Authorize(Roles = UserRoles.SuperUserRole)]
    public class AdminController : Controller
    {
        private AugerContext db = new AugerContext();

        private const string UniqueKeyErrorMessage = "The Key field must be unique";

        // Pick Course
        public ActionResult Index()
        {
            try
            {
                List<String> activeSessions = new List<String>();
                object obj = typeof(HttpRuntime).GetProperty("CacheInternal", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null);
                object[] obj2 = (object[])obj.GetType().GetField("_caches", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
                for (int i = 0; i < obj2.Length; i++)
                {
                    Hashtable c2 = (Hashtable)obj2[i].GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj2[i]);
                    foreach (DictionaryEntry entry in c2)
                    {
                        object o1 = entry.Value.GetType().GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(entry.Value, null);
                        if (o1.GetType().ToString() == "System.Web.SessionState.InProcSessionState")
                        {
                            SessionStateItemCollection sess = (SessionStateItemCollection)o1.GetType().GetField("_sessionItems", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o1);
                            if (sess != null)
                            {
                                if (sess["loggedInUserId"] != null)
                                {
                                    activeSessions.Add(sess["loggedInUserId"].ToString());
                                }
                            }
                        }
                    }
                }
                ViewBag.ActiveSessions = activeSessions;
            }
            catch { }

            return View();
        }

        //
        // GET: /Admin/ConsumerList
        public ActionResult ConsumerList()
        {
            return View(db.LtiConsumers.OrderBy(c => c.Name).ToList());
        }

        //
        // GET: /Admin/ConsumerDetails/5

        public ActionResult ConsumerDetails(int id = 0)
        {
            var consumer = db.LtiConsumers.Find(id);
            if (consumer == null)
            {
                return HttpNotFound();
            }
            return View(consumer);
        }

        //
        // GET: /Admin/ConsumerCreate

        public ActionResult ConsumerCreate()
        {
            var consumer = new LtiConsumer();
            consumer.Key = Guid.NewGuid().ToString("N").Substring(0, 16);
            consumer.Secret = Guid.NewGuid().ToString("N").Substring(0, 16);
            return View(consumer);
        }

        //
        // POST: /Admin/ConsumerCreate

        [HttpPost]
        public ActionResult ConsumerCreate(LtiConsumer consumer)
        {
            if (ModelState.IsValid)
            {
                // Make sure the user did not create a non-unique key
                var match = db.LtiConsumers.SingleOrDefault(
                    c => c.Key == consumer.Key);
                if (match != null)
                {
                    ModelState.AddModelError("Key", UniqueKeyErrorMessage);
                }
                else
                {
                    db.LtiConsumers.Add(consumer);
                    db.SaveChanges();
                    if (string.IsNullOrEmpty(Request["ReturnURL"]))
                    {
                        return RedirectToAction("ConsumerList");
                    }
                    var uri = new UriBuilder(Request["ReturnURL"]);
                    uri.Query += "ConsumerId=" + consumer.LtiConsumerId;
                    return Redirect(uri.ToString());
                }
            }
            return View(consumer);
        }

        //
        // GET: /Admin/ConsumerEdit/5

        public ActionResult ConsumerEdit(int id = 0)
        {
            var consumer = db.LtiConsumers.Find(id);
            if (consumer == null)
            {
                return HttpNotFound();
            }
            return View(consumer);
        }

        //
        // POST: /Admin/ConsumerEdit/5

        [HttpPost]
        public ActionResult ConsumerEdit(LtiConsumer consumer)
        {
            if (ModelState.IsValid)
            {
                // Make sure the user did not change the Key to
                // a non-unique value
                var match = db.LtiConsumers.SingleOrDefault(
                    c => c.Key == consumer.Key && c.LtiConsumerId != consumer.LtiConsumerId);
                if (match != null)
                {
                    ModelState.AddModelError("Key", UniqueKeyErrorMessage);
                }
                else
                {
                    var dbConsumer = db.LtiConsumers.SingleOrDefault(c => c.LtiConsumerId == consumer.LtiConsumerId);

                    if (dbConsumer == null)
                    {
                        ModelState.AddModelError(null, "Consumer does not exist");
                    }

                    dbConsumer.Name = consumer.Name;
                    dbConsumer.Key = consumer.Key;
                    dbConsumer.Secret = consumer.Secret;
                    db.SaveChanges();
                    return RedirectToAction("ConsumerList");
                }
            }
            return View(consumer);
        }

        //
        // GET: /Admin/ConsumerDelete/5

        public ActionResult ConsumerDelete(int id = 0)
        {
            var consumer = db.LtiConsumers.Find(id);
            if (consumer == null)
            {
                return HttpNotFound();
            }
            return View(consumer);
        }

        //
        // POST: /Admin/ConsumerDelete/5

        [HttpPost, ActionName("ConsumerDelete")]
        public ActionResult ConsumerDeleteConfirmed(int id)
        {
            var consumer = db.LtiConsumers.Find(id);
            db.LtiConsumers.Remove(consumer);
            db.SaveChanges();
            return RedirectToAction("ConsumerList");
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
