using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

namespace Auger
{
    public interface IBrowser : IDisposable
    {
        RemoteWebDriver Driver { get; }
        ReadOnlyCollection<LogEntry> BrowserLog { get; }
        Size WindowSize { get; }
        object ExecuteAsyncScript(string script, params object[] args);
        void LoadPage(string url);
        void SetWindowSize(int width, int height);
    }

    public static class BrowserFactory
    {
        private const int DRIVER_COUNT = 3;
        private const int SCRIPT_TIMEOUT = 3;

        private class BrowserWrapper : IBrowser
        {
            RemoteWebDriver _driver;
            public RemoteWebDriver Driver
            {
                get { return _driver; }
            }

            public ReadOnlyCollection<LogEntry> BrowserLog
            {
                get { return _driver.Manage().Logs.GetLog(LogType.Browser); }
            }

            public Size WindowSize
            {
                get { return _driver.Manage().Window.Size; }
            }

            public BrowserWrapper(RemoteWebDriver driver)
            {
                _driver = driver;
            }

            public void LoadPage(string url)
            {
                _driver.Url = url;
            }

            public void SetWindowSize(int width, int height)
            {
                _driver.Manage().Window.Size = new System.Drawing.Size(width, height);
            }

            public object ExecuteAsyncScript(string script, params object[] args)
            {
                return _driver.ExecuteAsyncScript(script, args);
            }

            public void Dispose()
            {
                ReturnDriver(_driver);
                //_driver.Dispose();
                //_driver.Quit();
            }
        }

        private static PhantomJSDriverService _driverService;
        //private static ChromeDriverService _driverService;
        private static BlockingCollection<RemoteWebDriver> _drivers;

        static BrowserFactory()
        {
            _driverService = PhantomJSDriverService.CreateDefaultService();
            //_driverService = ChromeDriverService.CreateDefaultService();
            _driverService.HideCommandPromptWindow = true;

            _drivers = new BlockingCollection<RemoteWebDriver>();
            for (var i = 0; i < DRIVER_COUNT; i++)
            {
                var driver = new PhantomJSDriver(_driverService);

                //var options = new ChromeOptions();
                //options.AddArgument("headless");
                //options.SetLoggingPreference(LogType.Browser, LogLevel.All);
                //var driver = new ChromeDriver(_driverService, options);

                driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(SCRIPT_TIMEOUT);
                _drivers.Add(driver);
            }
        }

        public static IBrowser GetDriver()
        {
            RemoteWebDriver driver;
            //var options = new ChromeOptions();
            //options.AddArgument("headless");
            //options.SetLoggingPreference(LogType.Browser, LogLevel.All);

            //driver = new ChromeDriver(_driverService, options);
            //driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(SCRIPT_TIMEOUT);
            //return new BrowserWrapper(driver);

            if (_drivers.TryTake(out driver, 3000))
            {
                return new BrowserWrapper(driver);
            }
            return null;
        }

        private static void ReturnDriver(RemoteWebDriver driver)
        {
            _drivers.Add(driver);
        }

        #region Static "Destructor"

        private static readonly Destructor Finalize = new Destructor();
        private sealed class Destructor
        {
            public Destructor()
            {
                AppDomain.CurrentDomain.ProcessExit += Destroy;
            }

            private static void Destroy(object sender, EventArgs e)
            {
                var count = 0;
                while (_drivers != null && count++ < DRIVER_COUNT)
                {
                    RemoteWebDriver driver;
                    if (_drivers.TryTake(out driver, 3000))
                    {
                        driver?.Quit();
                        driver?.Dispose();
                    }
                }

                _driverService?.Dispose();
            }

            ~Destructor()
            {
                Destroy(null, null);
            }
        }
        #endregion
    }
}

