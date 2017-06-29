using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Auger
{
    public class JsonpResult : JsonResult
    {
        object data = null;

        public JsonpResult()
        {
        }

        public JsonpResult(object data)
        {
            this.data = data;
        }

        public override void ExecuteResult(ControllerContext controllerContext)
        {
            if (controllerContext != null)
            {
                HttpResponseBase Response = controllerContext.HttpContext.Response;
                HttpRequestBase Request = controllerContext.HttpContext.Request;

                string callbackfunction = Request["callback"];
                if (string.IsNullOrEmpty(callbackfunction))
                {
                    throw new Exception("Callback function name must be provided in the request!");
                }
                Response.ContentType = "application/x-javascript";
                string json = data == null ? "{}" : JsonConvert.SerializeObject(data);
                Response.Write($"{callbackfunction}({json})");
            }
        }
    }
}
