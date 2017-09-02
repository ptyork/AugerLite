using System.Web;
using System.Web.Optimization;

namespace Auger
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/scripts/core").Include(
                "~/lib/jquery/jquery.js",
                "~/lib/bootstrap/js/bootstrap.js",
                "~/lib/bootstrap-switch/js/bootstrap-switch.js",
                "~/lib/mustache.js/mustache.js",
                "~/Scripts/auger-ui.js"
            ));

            bundles.Add(new StyleBundle("~/styles/core-light").Include(
                "~/lib/font-awesome/css/font-awesome.css",
                "~/Content/bootstrap-light.css",
                "~/lib/bootstrap-switch/css/bootstrap3/bootstrap-switch.css",
                "~/Content/auger.css",
                "~/Content/auger-light.css"
            ));
            bundles.Add(new StyleBundle("~/styles/core-dark").Include(
                "~/lib/font-awesome/css/font-awesome.css",
                "~/Content/bootstrap-dark.css",
                "~/lib/bootstrap-switch/css/bootstrap3/bootstrap-switch.css",
                "~/Content/auger.css",
                "~/Content/auger-dark.css"
            ));

            bundles.Add(new ScriptBundle("~/scripts/jqueryval").Include(
                "~/lib/jquery-validation/dist/jquery.validate.js",
                "~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js"
            ));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/scripts/compat").Include(
                "~/lib/modernizr/modernizr.js",
                "~/lib/respond/src/respond.js",
                "~/lib/es6-promise/es6-promise.auto.js"
            ));

            bundles.Add(new ScriptBundle("~/scripts/datepicker").Include(
                "~/lib/moment/moment.js",
                "~/lib/datetimepicker/js/bootstrap-datetimepicker.js"
            ));
            bundles.Add(new StyleBundle("~/styles/datepicker").Include(
                "~/lib/datetimepicker/css/bootstrap-datetimepicker.css"
            ));

            bundles.Add(new ScriptBundle("~/scripts/editor").Include(
                "~/lib/ace/ace.js"
            ));
            bundles.Add(new ScriptBundle("~/scripts/ide").Include(
                "~/lib/ace/ext-language_tools.js",
                "~/lib/ace/ext-settings_menu.js",
                "~/Scripts/auger-filemanager.js"
            ));
        }
    }
}