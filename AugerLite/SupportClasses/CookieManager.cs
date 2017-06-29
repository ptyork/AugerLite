using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Auger
{
    public static class CookieManager
    {
        public static int GetCourseId()
        {
            int courseId = 0;
            if (HttpContext.Current.Request.Cookies["courseId"] != null)
            {
                int.TryParse(HttpContext.Current.Request.Cookies["courseId"].Value, out courseId);
            }
            return courseId;
        }

        public static void SetCourseId(int id)
        {
            HttpContext.Current.Response.SetCookie(new HttpCookie("courseId", id.ToString()));
        }

        public static void ClearCourseId()
        {
            HttpCookie c = new HttpCookie("courseId");
            c.Expires = DateTime.Now.AddDays(-1);
            HttpContext.Current.Response.Cookies.Add(c);
        }
    }
}
