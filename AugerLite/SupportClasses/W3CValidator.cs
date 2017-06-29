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
    public static class W3CValidator
    {

        public static TestResults ValidateHTML(string pageName, string pageText)
        {
            TestResults results = new TestResults();

            try
            {
                var form = new NameValueCollection();
                form.Add("out", "json");
                form.Add("content", pageText);

                string htmlValidationJson = null;
                var req = RequestHelper.GetMultipartFormDataRequest("http://validator.w3.org/nu/", form);

                //var req = HttpWebRequest.CreateHttp("https://validator.nu/?out=json");
                //req.Method = "POST";
                //req.ContentType = "text/html; charset=utf-8";
                //req.UserAgent = "Auger/1.0";
                //using (var writer = new StreamWriter(req.GetRequestStream()))
                //{
                //    writer.Write(pageText);
                //    writer.Flush();
                //}

                using (var rsp = req.GetResponse())
                {
                    using (var rdr = new StreamReader(rsp.GetResponseStream()))
                    {
                        htmlValidationJson = rdr.ReadToEnd();
                    }
                }

                var validationResultObject = JObject.Parse(htmlValidationJson);
                var jsonMessages = validationResultObject["messages"]?.Children();

                if (jsonMessages != null)
                {
                    foreach (var jsonMessage in jsonMessages)
                    {
                        var msg = JsonConvert.DeserializeObject<W3CHtmlValidationMessage>(jsonMessage.ToString());
                        msg.Page = pageName;
                        results.W3CHtmlValidationMessages.Add(msg);
                    }
                }
            }
            catch (Exception e)
            {
                results.W3CHtmlValidationMessages.Add(new W3CHtmlValidationMessage() {
                    Type = W3CHtmlValidationMessage.MessageTypes.Warning,
                    Message = "Unable to perform HTML Validation"
                });
                Elmah.ErrorSignal.FromCurrentContext().Raise(e);
            }

            results.HtmlValidationCompleted = true;
            return results;
        }

        public static TestResults ValidateCSS(string fileName, string cssText)
        {
            TestResults results = new TestResults();

            try
            {
                var form = new NameValueCollection();
                form.Add("profile", "css3");
                form.Add("usermedium", "all");
                form.Add("type", "css");
                form.Add("warning", "0");
                form.Add("vextwarning", "true");
                form.Add("output", "json");
                form.Add("lang", "en");
                form.Add("text", cssText);

                string cssValidationJson = null;
                var req = RequestHelper.GetMultipartFormDataRequest("http://jigsaw.w3.org/css-validator/validator", form);
                using (var rsp = req.GetResponse())
                {
                    using (var rdr = new StreamReader(rsp.GetResponseStream()))
                    {
                        cssValidationJson = rdr.ReadToEnd();
                    }
                }

                var validationResultObject = JObject.Parse(cssValidationJson);
                var jsonErrors = validationResultObject["cssvalidation"]["errors"]?.Children();
                var jsonWarnings = validationResultObject["cssvalidation"]["warnings"]?.Children();

                if (jsonErrors != null)
                {
                    foreach (var jsonError in jsonErrors)
                    {
                        var msg = JsonConvert.DeserializeObject<W3CCssValidationMessage>(jsonError.ToString());
                        msg.File = fileName;
                        msg.Level = W3CCssValidationMessage.MessageLevels.Error;
                        results.W3CCssValidationMessages.Add(msg);
                    }
                }

                if (jsonWarnings != null)
                {
                    foreach (var jsonWarning in jsonWarnings)
                    {
                        var msg = JsonConvert.DeserializeObject<W3CCssValidationMessage>(jsonWarning.ToString());
                        msg.File = fileName;
                        msg.Level = W3CCssValidationMessage.MessageLevels.Warning;
                        results.W3CCssValidationMessages.Add(msg);
                    }
                }
            }
            catch (Exception e)
            {
                results.W3CCssValidationMessages.Add(new W3CCssValidationMessage()
                {
                    File = fileName,
                    Level = W3CCssValidationMessage.MessageLevels.Warning,
                    Message = "Unable to perform CSS Validation"
                });
                Elmah.ErrorSignal.FromCurrentContext().Raise(e);
            }

            results.CssValidationCompleted = true;
            return results;
        }

        public static TestResults ValidateCSS(Uri baseUri, List<string> checkedCssFiles, string pageText)
        {
            TestResults results = new TestResults();

            var dom = CQ.CreateDocument(pageText);
            var links = dom["link[rel='stylesheet']"];
            foreach (var link in links)
            {
                var linkUrl = link.Attributes["href"] as string;
                if (string.IsNullOrWhiteSpace(linkUrl))
                {
                    continue;
                }
                linkUrl = linkUrl.Trim();

                if (checkedCssFiles.Contains(linkUrl))
                {
                    continue;
                }
                checkedCssFiles.Add(linkUrl);

                var isRelative = !linkUrl.Contains("//");
                if (isRelative)
                {
                    linkUrl = baseUri + linkUrl;
                    var fileName = linkUrl.Split('/').Last();

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
                        results.AppendResults(ValidateCSS(fileName, linkText));
                    }
                    catch (Exception e)
                    {
                        results.W3CCssValidationMessages.Add(new W3CCssValidationMessage()
                        {
                            File = fileName,
                            Level = W3CCssValidationMessage.MessageLevels.Warning,
                            Message = "Unable to perform CSS Validation"
                        });
                        Elmah.ErrorSignal.FromCurrentContext().Raise(e);
                    }
                }
            }
            results.CssValidationCompleted = true;
            return results;
        }

    }
}
