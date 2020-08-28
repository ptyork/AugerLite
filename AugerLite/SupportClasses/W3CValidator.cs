using Auger.Models;
using CsQuery;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Auger
{
    public class W3CValidator
    {
        //private const string VALIDATOR_URL = "https://validator.w3.org/nu/";
        private const string VALIDATOR_URL = "https://validator.nu/";

        private Uri _baseUri;
        private List<string> _checkedFiles = new List<string>();

        public TestResults Results { get; } = new TestResults();

        public W3CValidator(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        public void ValidatePage(string pageName, string pageText)
        {
            Results.AppendResults(ValidateSingleFile(pageName, pageText));
            Results.HtmlValidationCompleted = true;

            var dom = CQ.CreateDocument(pageText);
            var links = dom["link[rel='stylesheet']"];
            foreach (var link in links)
            {
                var linkHref = link.Attributes["href"] as string;
                if (string.IsNullOrWhiteSpace(linkHref))
                {
                    continue;
                }
                linkHref = linkHref.Trim();

                if (_checkedFiles.Contains(linkHref))
                {
                    continue;
                }
                _checkedFiles.Add(linkHref);

                var isRelative = !linkHref.Contains("//");
                if (isRelative)
                {
                    var linkUrl = _baseUri + linkHref;

                    // Get CSS File
                    string linkText = null;
                    try
                    {
                        var req = HttpWebRequest.Create(linkUrl);
                        using (var rsp = req.GetResponse())
                        {
                            using (var rdr = new StreamReader(rsp.GetResponseStream()))
                            {
                                linkText = rdr.ReadToEnd();
                            }
                        }
                        Results.AppendResults(ValidateSingleFile(linkHref, linkText, true));
                    }
                    catch (Exception e)
                    {
                        Results.W3CCssValidationMessages.Add(new W3CCssValidationMessage()
                        {
                            File = linkHref,
                            Level = W3CCssValidationMessage.MessageLevels.Warning,
                            Message = "Unable to perform CSS Validation"
                        });
                        Elmah.ErrorSignal.FromCurrentContext().Raise(e);
                    }
                }
            }
            Results.CssValidationCompleted = true;
        }


        public static TestResults ValidateSingleFile(string fileName, string fileText, bool isCSS = false)
        {
            TestResults results = new TestResults();

            try
            {
                var req = HttpWebRequest.CreateHttp(
                    VALIDATOR_URL + "?out=json&level=error"
                );
                req.Method = "POST";
                var type = isCSS ? "css" : "html";
                req.ContentType = $"text/{type}; charset=utf-8";
                req.UserAgent = "Auger/1.0";
                using (var writer = new StreamWriter(req.GetRequestStream()))
                {
                    writer.Write(fileText);
                    writer.Flush();
                }

                string validationJson = null;
                using (var rsp = req.GetResponse())
                {
                    using (var rdr = new StreamReader(rsp.GetResponseStream()))
                    {
                        validationJson = rdr.ReadToEnd();
                    }
                }

                var validationResultObject = JObject.Parse(validationJson);
                var jsonMessages = validationResultObject["messages"]?.Children();

                if (jsonMessages != null)
                {
                    foreach (var jsonMessage in jsonMessages)
                    {
                        var msg = JsonConvert.DeserializeObject<W3CHtmlValidationMessage>(jsonMessage.ToString());
                        msg.Page = fileName;
                        if (isCSS)
                        {
                            results.W3CCssValidationMessagesNew.Add(msg);
                        }
                        else
                        {
                            results.W3CHtmlValidationMessages.Add(msg);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                results.W3CHtmlValidationMessages.Add(new W3CHtmlValidationMessage()
                {
                    Type = W3CHtmlValidationMessage.MessageTypes.Warning,
                    Message = "Unable to perform HTML Validation"
                });
                Elmah.ErrorSignal.FromCurrentContext().Raise(e);
            }
            return results;
        }

    }
}
