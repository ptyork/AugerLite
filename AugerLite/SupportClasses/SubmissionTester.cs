using Auger.Models;
using Auger.Models.Data;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Auger
{
    public static class SubmissionTester
    {
        private static string _augerBaseUrl = null;
        private static string _augerScriptLoader = null;

        public static bool IsInitialized { get; private set; } = false;

        public static void Init(Uri requestUri, string virtualPath)
        {
            if (IsInitialized)
                return;

            if (virtualPath?.Last() != '/')
                virtualPath += "/";

            _augerBaseUrl = $"{requestUri.Scheme}://{requestUri.Authority}{virtualPath}";
            var augerScriptUrl = $"{_augerBaseUrl}Scripts/auger.js?nogui";

            var loadScript = @"
var loadScript = function (url, callback) {
    var script = document.createElement('script');
    script.type = 'text/javascript';
    script.src = url;
    if (script.readyState)
    { /* IE */
        script.onreadystatechange = function() {
            if (script.readyState == 'loaded' || script.readyState == 'complete')
            {
                script.onreadystatechange = null;
                callback();
            }
        };
    }
    else
    { /* OTHERS */
        script.onload = callback;
    }
    document.getElementsByTagName('head')[0].appendChild(script);
}";
            _augerScriptLoader = $@"
var callback = arguments[arguments.length - 1];
if (typeof Auger === 'undefined') {{
    {loadScript};
    loadScript('{augerScriptUrl}', callback);
}} else {{
    callback();
}}";

            IsInitialized = true;
        }

        public static void TestSubmission(StudentSubmission submission)
        {
            TestResults preres = new TestResults();
            TestResults fullres = new TestResults();
            List<String> checkedCssFiles = new List<string>();

            var repo = SubmissionRepository.Get(submission.StudentAssignment);
            repo.Checkout(submission.CommitId);

            var assignment = submission.StudentAssignment.Assignment;

            var allDeviceIds = assignment.AllScripts.Select(s => s.DeviceId).Distinct();
            List<Device> allDevices = new List<Device>();
            foreach (var deviceId in allDeviceIds)
            {
                allDevices.Add(Device.Parse(deviceId));
            }
            if (allDevices.Count == 0) allDevices.Add(Device.Large);

            using (var browser = BrowserFactory.GetDriver())
            {
                if (browser != null)
                {
                    if (assignment.Pages.Any())
                    {
                        foreach (var page in assignment.Pages)
                        {
                            var pageUri = new Uri(repo.FileUri, page.PageName);

                            string pageText = null;

                            if (RequestHelper.GetPageText(pageUri, out pageText))
                            {
                                var res = W3CValidator.ValidateHTML(page.PageName, pageText);
                                res.AppendResults(W3CValidator.ValidateCSS(repo.FileUri, checkedCssFiles, pageText));
                                preres.AppendResults(res);
                                fullres.AppendResults(res);

                                foreach (var device in allDevices)
                                {
                                    browser.SetWindowSize(device.ViewportWidth, device.ViewportHeight);
                                    preres.AppendResults(TestPage(browser, pageUri, assignment, page, device.ViewportWidth, true));
                                    fullres.AppendResults(TestPage(browser, pageUri, assignment, page, device.ViewportWidth, false));
                                }
                            }
                            else
                            {
                                preres.Exceptions.Add($"{page.PageName} not found");
                                fullres.Exceptions.Add($"{page.PageName} not found");
                            }
                        }
                    }
                    else
                    {
                        var folder = repo.GetFolder();
                        _TestFolder(folder, preres, fullres, checkedCssFiles, assignment, allDevices, browser);
                    }
                }
            }

            preres.DomTestComplete = true;
            fullres.DomTestComplete = true;
            submission.PreSubmissionResults = preres;
            submission.FullResults = fullres;
        }

        private static void _TestFolder(RepoFolder folder, TestResults preres, TestResults fullres, List<string> checkedCssFiles, Assignment assignment, List<Device> allDevices, IBrowser browser)
        {
            foreach (var file in folder.Files.Where(f => f.Type == FileType.html))
            {
                var pageUri = new Uri(folder.Uri, file.Name);

                string pageText = null;

                if (RequestHelper.GetPageText(pageUri, out pageText))
                {
                    var res = W3CValidator.ValidateHTML(file.Name, pageText);
                    res.AppendResults(W3CValidator.ValidateCSS(folder.Uri, checkedCssFiles, pageText));
                    preres.AppendResults(res);
                    fullres.AppendResults(res);

                    foreach (var device in allDevices)
                    {
                        browser.SetWindowSize(device.ViewportWidth, device.ViewportHeight);
                        preres.AppendResults(TestPage(browser, pageUri, assignment, null, device.ViewportWidth, true));
                        fullres.AppendResults(TestPage(browser, pageUri, assignment, null, device.ViewportWidth, false));
                    }
                }
            }
            foreach (var subfolder in folder.Folders)
            {
                _TestFolder(subfolder, preres, fullres, checkedCssFiles, assignment, allDevices, browser);
            }
        }

        public static TestResults TestPage(IBrowser browser, Uri pageUri, Assignment assignment, Page page, int viewportWidth, bool preTest)
        {
            TestResults results = new TestResults();

            var script = preTest ?
                GetPreTestScript(assignment, page, viewportWidth) :
                GetTestScript(assignment, page, viewportWidth);

            if (string.IsNullOrWhiteSpace(script))
            {
                return results;
            }

            script = $@"
var callback = arguments[arguments.length - 1];
Auger.Init(function () {{
    {script}
    Auger.RunTestEmbedded(callback);
}});
";

            browser.LoadPage(pageUri.ToString());
            try
            {
                browser.ExecuteAsyncScript(_augerScriptLoader);

                var junkResult = browser.ExecuteAsyncScript(script); // seriously, I really JUST want a string folks
                var json = JsonConvert.SerializeObject(junkResult);
                results.AppendResults(JsonConvert.DeserializeObject<TestResults>(json));
            }
            catch (WebDriverTimeoutException ex)
            {
                results.Exceptions.Add($"Timeout encounterd while running test script for {pageUri.Segments.LastOrDefault()}");
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }
            catch (Exception ex)
            {
                results.Exceptions.Add($"Unexpected error while running test script for {pageUri.Segments.LastOrDefault()}: {ex.Message}");
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
            }

            return results;
        }

        public static string GetPreTestScript(Assignment assignment, Page page, int viewportWidth)
        {
            var allCommonScripts = assignment?.CommonScriptsPreGrade;
            var allPageScripts = page?.ScriptsPreGrade;

            return _GetTestScript(viewportWidth, allCommonScripts, allPageScripts);
        }

        public static string GetTestScript(Assignment assignment, Page page, int viewportWidth)
        {
            var allCommonScripts = assignment?.CommonScripts;
            var allPageScripts = page?.Scripts;

            return _GetTestScript(viewportWidth, allCommonScripts, allPageScripts);
        }

        private static String _GetTestScript(int viewportWidth, IEnumerable<Script> allCommonScripts, IEnumerable<Script> allPageScripts)
        {
            IEnumerable<Script> commonScripts = new List<Script>(),
                                pageScripts = new List<Script>();

            if (allCommonScripts != null)
            {
                var commonDevices = allCommonScripts.Select(s => s.Device).OrderByDescending(d => d.ViewportWidth);
                var commonDevice = commonDevices.FirstOrDefault(d => d.ViewportWidth <= viewportWidth);
                commonScripts = allCommonScripts.Where(s => string.IsNullOrWhiteSpace(s.DeviceId) || s.DeviceId == commonDevice.DeviceId);
            }
            if (allPageScripts != null)
            {
                var pageDevices = allPageScripts.Select(s => s.Device).OrderByDescending(d => d.ViewportWidth);
                var pageDevice = pageDevices.FirstOrDefault(d => d.ViewportWidth <= viewportWidth);
                pageScripts = allPageScripts.Where(s => string.IsNullOrWhiteSpace(s.DeviceId) || s.DeviceId == pageDevice.DeviceId);
            }

            var allScripts = new List<Script>();
            allScripts.AddRange(commonScripts);
            allScripts.AddRange(pageScripts);

            StringBuilder scriptSB = new StringBuilder();
            foreach (var script in allScripts)
            {
                scriptSB.Append(script.ScriptText);
            }

            return scriptSB.ToString();
        }

    }
}
