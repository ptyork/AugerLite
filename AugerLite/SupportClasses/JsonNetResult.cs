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
    public class JsonNetResult : JsonResult
    {
        object data = null;

        public JsonNetResult()
        {
        }

        public JsonNetResult(object data)
        {
            this.data = data;
        }

        public override void ExecuteResult(ControllerContext controllerContext)
        {
            if (controllerContext != null)
            {
                HttpResponseBase Response = controllerContext.HttpContext.Response;
                Response.ContentType = "application/x-javascript";
                string json = data == null ? "{}" : JsonConvert.SerializeObject(data);
                Response.Write(json);
            }
        }
    }
}
