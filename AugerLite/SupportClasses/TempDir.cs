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
    public class TempDir
    {
        private const string SESSION_KEY = "__TEMPDIRS";

        private static string _basePath = null;
        public static void Init(string basePath)
        {
            _basePath = basePath;
            // delete and recreate on application start
            var _dir = System.IO.Directory.CreateDirectory(_basePath);
            _dir.Delete(true);
            System.IO.Directory.CreateDirectory(_basePath);
        }

        public static void InitSession()
        {
            System.Web.HttpContext.Current.Session.Add(SESSION_KEY, new Dictionary<string, TempDir>());
        }

        public static void EndSession()
        {
            var tempDirs = System.Web.HttpContext.Current.Session[SESSION_KEY] as Dictionary<string, TempDir>;
            if (tempDirs != null)
            {
                foreach (var tempDir in tempDirs.Values)
                {
                    tempDir.Delete();
                }
                tempDirs.Clear();
            }
            System.Web.HttpContext.Current.Session.Remove(SESSION_KEY);
        }

        public static TempDir Get(Repository repo)
        {
            string dirKey = $"{repo.CourseId}\\{repo.UserId}\\{repo.AssignmentId}";

            TempDir tempDir = null;
            var tempDirs = System.Web.HttpContext.Current.Session[SESSION_KEY] as Dictionary<string, TempDir>;
            if (tempDirs != null)
            {
                if (!tempDirs.TryGetValue(dirKey, out tempDir) || tempDir.CommitId != repo.CurrentCommitId)
                {
                    tempDir = new TempDir(repo);
                    tempDirs[dirKey] = tempDir;
                }
            }
            return tempDir;
        }

        public static string GetPath(string courseId, string userId, string assignmentId)
        {
            var sessionId = System.Web.HttpContext.Current.Session.SessionID;
            return $"{_basePath}\\{sessionId}\\{courseId}\\{userId}\\{assignmentId}";
        }

        public static string FolderName
        {
            get { return _basePath; }
        }

        private string _commitId;
        private string _folderName;
        private DirectoryInfo _dir;

        public TempDir(Repository repo)
        {
            // init folder
            _folderName = GetPath(repo.CourseId, repo.UserId, repo.AssignmentId);
            _dir = System.IO.Directory.CreateDirectory(_folderName);
            DeleteContent();
            _CopyFolder(repo.Directory, _dir);
            _commitId = repo.CurrentCommitId;
        }

        public String CommitId
        {
            get { return _commitId; }
        }

        public DirectoryInfo Directory
        {
            get { return _dir; }
        }

        public Uri FileUri
        {
            get { return new Uri(_folderName + "\\"); }
        }

        public void Delete()
        {
            _Retry(() =>
            {
                _dir.Delete(true);
                // tempDir is at the assignment level, so try to delete user, course, and sessionId folders
                if (!_dir.Parent.GetDirectories().Any())
                {
                    _dir.Parent.Delete(); // user folder
                    if (!_dir.Parent.Parent.GetDirectories().Any())
                    {
                        _dir.Parent.Parent.Delete(); // course folder
                        if (!_dir.Parent.Parent.Parent.GetDirectories().Any())
                        {
                            _dir.Parent.Parent.Parent.Delete(); // sessionId folder
                        }
                    }
                }
            });
        }

        public void DeleteContent()
        {
            _Retry(() =>
            {
                foreach (var dir in _dir.GetDirectories())
                {
                    dir.Delete(true);
                }
                foreach (var file in _dir.GetFiles())
                {
                    file.Delete();
                }
            });
        }

        private static void _CopyFolder(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
            {
                if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    _CopyFolder(dir, target.CreateSubdirectory(dir.Name));
                }
            }
            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name));
            }
        }

        private static void _Retry(Action func)
        {
            _Retry(() => {
                func();
                return null;
            });
        }

        private static string _Retry(Func<string> func)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception e)
                {
                    if (++retryCount < 5)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }

    }
}
