using Auger.Models;
using Auger.Models.Data;
using Ionic.Zip;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.SessionState;

namespace Auger
{
    public class TemplateManager
    {
        private static string _basePath = null;
        public static void Init(string basePath)
        {
            _basePath = basePath;
            System.IO.Directory.CreateDirectory(_basePath);
            System.IO.Directory.CreateDirectory($"{_basePath}\\default");
        }

        public static FileInfo GetTemplate(int courseId, string template)
        {
            FileInfo file = null;
            if (!string.IsNullOrWhiteSpace(template))
            {
                var fileName = $"template.{template}";
                var dir = System.IO.Directory.CreateDirectory($"{_basePath}\\{courseId}");
                file = dir.EnumerateFiles(fileName).FirstOrDefault();
                if (file == null)
                {
                    dir = System.IO.Directory.CreateDirectory($"{_basePath}\\default");
                    file = dir.EnumerateFiles(fileName).FirstOrDefault();
                }
            }
            return file;
        }
    }
}
