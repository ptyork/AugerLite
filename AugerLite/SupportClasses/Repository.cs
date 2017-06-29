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

namespace Auger
{
    public class Repository
    {
        private string _basePath;
        private string _courseId;
        private string _userId;
        private string _assignmentId;

        private string _assignmentFolderName;

        private string _currentCommitId;

        public Repository(string basePath, string courseId, string userId, string assignmentId, bool create = false)
        {
            _basePath = basePath;
            _courseId = courseId.Trim();
            _userId = userId.Trim().ToLowerInvariant();
            _assignmentId = assignmentId.Trim();

            if (!create && !System.IO.Directory.Exists($"{_basePath}\\{_courseId}\\{_userId}\\{_assignmentId}"))
            {
                throw new FileNotFoundException($"No repository exists for {_courseId}\\{_userId}\\{_assignmentId}.");
            }

            // init assignment folder + repo
            _assignmentFolderName = $"{_basePath}\\{_courseId}\\{_userId}\\{_assignmentId}";
            System.IO.Directory.CreateDirectory(_assignmentFolderName);
            if (!LibGit2Sharp.Repository.IsValid(_assignmentFolderName))
            {
                LibGit2Sharp.Repository.Init(_assignmentFolderName);
            }
        }

        public DirectoryInfo Directory
        {
            get { return System.IO.Directory.CreateDirectory(_assignmentFolderName); }
        }

        public Uri FileUri
        {
            get { return new Uri(_assignmentFolderName + "\\"); }
        }

        public string CourseId
        {
            get { return _courseId; }
        }

        public String UserId
        {
            get { return _userId; }
        }

        public string AssignmentId
        {
            get { return _assignmentId; }
        }

        public string CurrentCommitId
        {
            get { return _currentCommitId; }
        }

        public string LatestCommitId
        {
            get
            {
                using (var repo = new LibGit2Sharp.Repository(_assignmentFolderName))
                {
                    if (!repo.Commits.Any())
                    {
                        return null;
                    }
                    if (repo.Tags.Any(t => t.FriendlyName == "LATEST"))
                    {
                        return repo.Tags["LATEST"].Target.Sha;
                    }
                    return repo.Commits.First().Sha;
                }
            }
        }

        public void CheckoutSubmission(string commitId = null)
        {
            try
            {
                _ClearRepository(); // Delete the contents to insure no file locks
                using (var repo = new LibGit2Sharp.Repository(_assignmentFolderName))
                {
                    if (!repo.Commits.Any())
                    {
                        return; // hopefully just happens during the first commit
                    }

                    CheckoutOptions opts = new CheckoutOptions() { CheckoutModifiers = CheckoutModifiers.Force };
                    _Retry(() => {
                        Commands.Checkout(repo, LatestCommitId, opts);
                    });

                    _currentCommitId = LatestCommitId;

                    if (commitId != null)
                    {
                        //var commit = repo.Commits.FirstOrDefault(c => c.Message.StartsWith(GetCommitName(commitTime.Value)));
                        var commit = repo.Commits.FirstOrDefault(c => c.Sha == commitId);

                        if (commit == null)
                        {
                            throw new ApplicationException("Unable to find specified submission");
                        }

                        _Retry(() => {
                            Commands.Checkout(repo, commit);
                        });

                        _currentCommitId = commitId;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to checkout the specified Submission", e);
            }
        }

        List<string> _zipIgnoreList = new List<string>() {
            "__MACOSX",
            ".DS_Store"
        };

        private bool _ShouldSkip(string filename)
        {
            foreach (var entry in _zipIgnoreList)
            {
                if (filename.ToLowerInvariant().Contains(entry.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        public string CommitAssignmentFromZip(Stream zipStream)
        {
            try
            {
                CheckoutSubmission();   // Move HEAD to LATEST checkout
                _ClearRepository();     // Clear the current submission for complete replacement
                using (var zstr = new ZipInputStream(zipStream))
                {
                    ZipEntry zentry;
                    List<string> rootFolders = new List<string>();
                    while ((zentry = zstr.GetNextEntry()) != null)
                    {
                        if (_ShouldSkip(zentry.FileName))
                        {
                            continue;
                        }

                        // only looking for folders with files in them
                        var fileName = Path.GetFileName(zentry.FileName);
                        if (fileName.Length == 0)
                        {
                            continue;
                        }

                        var folder = Path.GetDirectoryName(zentry.FileName);

                        // if there's no folder in any "real" file, there's not root folder
                        if (folder.Length == 0)
                        {
                            rootFolders.Clear();
                            break;
                        }

                        // if this is the first folder, assume initially that it IS the root folder
                        if (rootFolders.Count == 0)
                        {
                            rootFolders = folder.Split(Path.DirectorySeparatorChar).ToList();
                        }
                        // otherwise, compare to the current set of rootFolders
                        else
                        {
                            var folders = folder.Split(Path.DirectorySeparatorChar);

                            // compare each part of the folder path to the current set of rootFolders
                            for (var i = 0; i < folders.Length; i++)
                            {
                                if (i >= rootFolders.Count)
                                {
                                    break;
                                }
                                if (folders[i].ToLowerInvariant() != rootFolders[i].ToLowerInvariant())
                                {
                                    rootFolders.RemoveRange(i, rootFolders.Count - i);
                                    break;
                                }
                            }
                            if (rootFolders.Count == 0)
                            {
                                break;
                            }
                        }
                    }

                    int rootLen = 0;
                    if (rootFolders.Count > 0)
                    {
                        rootLen = string.Join("/", rootFolders).Length;
                    }

                    zstr.Position = 0;
                    while ((zentry = zstr.GetNextEntry()) != null)
                    {
                        if (_ShouldSkip(zentry.FileName))
                        {
                            continue;
                        }

                        var folder = Path.GetDirectoryName(zentry.FileName);
                        if (folder.Length < rootLen)
                        {
                            continue;
                        }
                        folder = folder.Substring(rootLen);
                        System.IO.Directory.CreateDirectory($"{_assignmentFolderName}\\{folder}");

                        var fileName = Path.GetFileName(zentry.FileName);
                        if (fileName.Length > 0)
                        {
                            using (var fstr = File.Create($"{_assignmentFolderName}\\{folder}\\{fileName}"))
                            {
                                zstr.CopyTo(fstr);
                                fstr.Flush(true);
                                fstr.Close();
                            }
                        }
                    }
                }
                return _Commit();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
            }
        }

        public string CommitAssignmentFromUrl(string url)
        {
            try
            {
                CheckoutSubmission();   // Move HEAD to LATEST checkout
                _ClearRepository();     // Clear the current submission for complete replacement
                var path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SubmissionRepository)).CodeBase);
                var baseUri = new Uri(new Uri(url), ".");
                var dirCount = baseUri.Segments.Length - 1;
                var startInfo = new ProcessStartInfo();
                startInfo.Arguments = $"-p -k -r -nH --cut-dirs={dirCount} {url}";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.WorkingDirectory = _assignmentFolderName;
                startInfo.FileName = $"{path}\\wget.exe";
                var proc = Process.Start(startInfo);
                proc.WaitForExit(5000);
                return _Commit();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
            }
        }

        public static string GetCommitName(DateTime commitTime)
        {
            return commitTime.Ticks.ToString();
        }

        private void _ClearRepository()
        {
            _Retry(() => {
                var repoDir = System.IO.Directory.CreateDirectory(_assignmentFolderName);
                foreach (var file in repoDir.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in repoDir.GetDirectories())
                {
                    if ((dir.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                    {
                        dir.Delete(true);
                    }
                }
            });
        }

        private string _Commit()
        {
            string commitId = null;
            using (var repo = new LibGit2Sharp.Repository(_assignmentFolderName))
            {
                var diffs = repo.Diff.Compare<TreeChanges>(null, true);

                if (diffs.Count() > 0)
                {
                    // This is a repository with existing submissions AND this
                    // commit has changes

                    foreach (var diff in diffs)
                    {
                        if (diff.Status == ChangeKind.Added)
                        {
                            repo.Index.Add(diff.Path);
                        }
                        else if (diff.Status == ChangeKind.Deleted)
                        {
                            repo.Index.Remove(diff.Path);
                        }
                        Commands.Stage(repo, diff.Path);
                    }

                    // TODO: Get Signature info from configuration file
                    Signature sig = new Signature("auger", "admin@auger.org", DateTimeOffset.Now);

                    try
                    {
                        commitId = _Retry(() =>
                        {
                            var commit = repo.Commit(GetCommitName(DateTime.Now), sig, sig);
                            return commit.Sha;
                        });
                        if (repo.Tags.Any(t => t.FriendlyName == "LATEST"))
                        {
                            repo.Tags.Remove("LATEST");
                        }
                        repo.ApplyTag("LATEST");
                    }
                    catch (EmptyCommitException) { } // ignore this but throw others
                }
                else if (!repo.Commits.Any())
                {
                    // This is the first commit for this repository

                    // TODO: Get Signature info from configuration file
                    Signature sig = new Signature("auger", "admin@auger.org", DateTimeOffset.Now);
                    commitId = _Retry(() => {
                        var commit = repo.Commit(GetCommitName(DateTime.Now), sig, sig);
                        return commit.Sha;
                    });
                    repo.ApplyTag("LATEST");
                }
                else
                {
                    // No changes to an existing repository. Return null for
                    // the commitId to indicate no change.
                }
            }
            _currentCommitId = commitId;
            return commitId;
        }

        private void _Retry(Action func)
        {
            _Retry(() => {
                func();
                return null;
            });
        }

        private string _Retry(Func<string> func)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (EmptyCommitException e)
                {
                    throw e;
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