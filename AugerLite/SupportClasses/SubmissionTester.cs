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
    public class SubmissionTester : IDisposable
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
            var augerScriptUrl = $"{_augerBaseUrl}Scripts/auger-test-core.js";

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
        
        private TestResults _presubmissionResults = new TestResults();
        private TestResults _fullResults = new TestResults();
        private StudentSubmission _submission = null;
        private Assignment _assignment = null;
        private SubmissionRepository _repo = null;
        private W3CValidator _validator = null;
        private List<Device> _allDevices = new List<Device>();
        private IBrowser _browser = null;

        public SubmissionTester(StudentSubmission submission)
        {
            _submission = submission;
            _assignment = submission.StudentAssignment.Assignment;

            _repo = SubmissionRepository.Get(submission.StudentAssignment);
            _repo.Checkout(submission.CommitId);

            _validator = new W3CValidator(_repo.FileUri);

            var allDeviceIds = _assignment.AllScripts.Select(s => s.DeviceId).Distinct();
            foreach (var deviceId in allDeviceIds)
            {
                _allDevices.Add(Device.Parse(deviceId));
            }
            if (_allDevices.Count == 0) _allDevices.Add(Device.Large);

            _browser = BrowserFactory.GetDriver();
            if (_browser != null)
            {
                _browser.SetWindowSize(Device.Large.ViewportWidth, Device.Large.ViewportHeight);
            }
        }

        public void TestAll()
        {
            if (_browser != null)
            {
                // If an assignment has pages defined, assume that we should ONLY test
                // the pages defined for that assignment. Helps to find missing pages.
                if (_assignment.Pages.Any())
                {
                    foreach (var page in _assignment.Pages)
                    {
                        var pageUri = new Uri(_repo.FileUri, page.PageName);

                        string pageText = null;

                        if (RequestHelper.GetPageText(pageUri, out pageText))
                        {
                            _validator.ValidatePage(page.PageName, pageText);

                            foreach (var device in _allDevices)
                            {
                                _browser.SetWindowSize(device.ViewportWidth, device.ViewportHeight);
                                _presubmissionResults.AppendResults(TestPage(pageUri, page, true));
                                _fullResults.AppendResults(TestPage(pageUri, page, false));
                            }
                        }
                        else
                        {
                            _presubmissionResults.Exceptions.Add($"{page.PageName} not found");
                            _fullResults.Exceptions.Add($"{page.PageName} not found");
                        }
                    }
                }
                // If an assignment has no pages defined, assume that we should test
                // ALL pages in the repository.
                else
                {
                    var folder = _repo.GetFolder();
                    _TestFolder(folder);
                }

                _presubmissionResults.AppendResults(_validator.Results);
                _fullResults.AppendResults(_validator.Results);
                _presubmissionResults.DomTestComplete = true;
                _fullResults.DomTestComplete = true;
            }

            _submission.PreSubmissionResults = _presubmissionResults;
            _submission.FullResults = _fullResults;
        }

        private void _TestFolder(RepoFolder folder)
        {
            foreach (var file in folder.Files.Where(f => f.Type == FileType.html))
            {
                var pageUri = new Uri(folder.Uri, file.Name);

                string pageText = null;

                if (RequestHelper.GetPageText(pageUri, out pageText))
                {
                    _validator.ValidatePage(file.Name, pageText);

                    foreach (var device in _allDevices)
                    {
                        _browser.SetWindowSize(device.ViewportWidth, device.ViewportHeight);
                        _presubmissionResults.AppendResults(TestPage(pageUri, null, true));
                        _fullResults.AppendResults(TestPage(pageUri, null, false));
                    }
                }
            }
            foreach (var subfolder in folder.Folders)
            {
                _TestFolder(subfolder);
            }
        }

        public TestResults TestPage(Uri pageUri, Page page, bool preTest = true)
        {
            TestResults results = new TestResults();

            var script = preTest ?
                GetPreTestScript(_assignment, page, _browser.WindowSize.Width) :
                GetTestScript(_assignment, page, _browser.WindowSize.Width);

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

            _browser.LoadPage(pageUri.ToString());
            try
            {
                _browser.ExecuteAsyncScript(_augerScriptLoader);

                var junkResult = _browser.ExecuteAsyncScript(script); // seriously, I really JUST want a string folks
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
            finally
            {
                foreach (var entry in _browser.BrowserLog)
                {
                    System.Diagnostics.Debug.WriteLine(entry.Message);
                    results.DebugMessages.Add(entry.Message);
                }
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _browser.Dispose();
                }

                _presubmissionResults = null;
                _fullResults = null;
                _submission = null;
                _assignment = null;
                _repo = null;
                _validator = null;
                _allDevices = null;
                _browser = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SubmissionTester() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
