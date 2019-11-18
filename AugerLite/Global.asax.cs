using Auger.DAL;
using Auger.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Auger
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            TemplateManager.Init(GetPath("AugerTemplateDir"));
            SubmissionRepository.Init(GetPath("AugerSubmissionDir"));
            WorkRepository.Init(GetPath("AugerWorkDir"));
            PlaygroundRepository.Init(GetPath("AugerPlaygroundDir"));
            TempDir.Init(GetPath("AugerTempDir"));
            //TemplateManager.Init(Server.MapPath("~/app_data/templates/"));
            //SubmissionRepository.Init(Server.MapPath("~/app_data/repo/"));
            //WorkRepository.Init(Server.MapPath("~/app_data/work/"));
            //PlaygroundRepository.Init(Server.MapPath("~/app_data/play/"));
            //TempDir.Init(Server.MapPath("~/app_data/temp/"));

            //HostingEnvironment.RegisterVirtualPathProvider(new RepositoryVPP());

            HttpContext.Current.Server.ScriptTimeout = 2400;
        }

        private string GetPath(string settingsKey)
        {
            var path = System.Web.Configuration.WebConfigurationManager.AppSettings[settingsKey];

            if (path.Contains("~"))
            {
                path = Server.MapPath(path);
            }

            return path;
        }

        protected void Application_BeginRequest()
        {
            if (!SubmissionTester.IsInitialized)
            {
                SubmissionTester.Init(Request.Url, HttpRuntime.AppDomainAppVirtualPath);
            }
        }

        protected void Application_End()
        {
        }

        protected void Session_Start()
        {
            TempDir.InitSession(this.Session);
        }

        protected void Session_End()
        {
            TempDir.EndSession(this.Session);
        }
    }
}
