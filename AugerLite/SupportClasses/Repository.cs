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
    public abstract class Repository
    {
        private int _courseId;
        private string _userName;
        private int _repositoryId;

        private string _repositoryFolderName;

        private string _currentCommitId;

        public virtual string BasePath { get; }

        public Repository(int courseId, string userName, int repositoryId, bool create = false)
        {
            if (string.IsNullOrWhiteSpace(BasePath))
            {
                throw new InvalidOperationException("This repository type has not been initialized.");
            }

            _courseId = courseId;
            _userName = userName.Trim().ToLowerInvariant();
            _repositoryId = repositoryId;

            if (!create && !System.IO.Directory.Exists($"{BasePath}\\{_courseId}\\{_userName}\\{_repositoryId}"))
            {
                throw new FileNotFoundException($"No repository exists for {_courseId}\\{_userName}\\{_repositoryId}.");
            }

            // init assignment folder + repo
            _repositoryFolderName = $"{BasePath}\\{_courseId}\\{_userName}\\{_repositoryId}";
            System.IO.Directory.CreateDirectory(_repositoryFolderName);
            if (!LibGit2Sharp.Repository.IsValid(_repositoryFolderName))
            {
                LibGit2Sharp.Repository.Init(_repositoryFolderName);
            }
        }

        public DirectoryInfo Folder
        {
            get { return System.IO.Directory.CreateDirectory(_repositoryFolderName); }
        }

        public string FilePath
        {
            get { return _repositoryFolderName + "\\"; }
        }

        public Uri FileUri
        {
            get { return new Uri(FilePath); }
        }

        public int CourseId
        {
            get { return _courseId; }
        }

        public String UserName
        {
            get { return _userName; }
        }

        public int RepositoryId
        {
            get { return _repositoryId; }
        }

        public string CurrentCommitId
        {
            get { return _currentCommitId; }
        }

        public string LatestCommitId
        {
            get
            {
                using (var repo = new LibGit2Sharp.Repository(_repositoryFolderName))
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

        public string GetFullPath(string path = "", string name = "")
        {
            path = path ?? "";
            path = path.Replace('/', '\\');
            var fullPath = $"{path}\\{name}".Trim('\\');
            return $"{this.FilePath}\\{fullPath}";
        }

        public void CopyFromRepository(Repository otherRepository)
        {
            try
            {
                _ClearRepository();

                _Retry(() => {
                    var thisDir = System.IO.Directory.CreateDirectory(_repositoryFolderName);
                    var otherDir = System.IO.Directory.CreateDirectory(otherRepository._repositoryFolderName);
                    otherDir.CopyTo(thisDir);
                });
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
            }
        }

        public void Checkout(string commitId = null)
        {
            try
            {
                _ClearRepository(); // Delete the contents to insure no file locks
                using (var repo = new LibGit2Sharp.Repository(_repositoryFolderName))
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

        public string Commit(string message = "")
        {
            string commitId = null;
            var now = DateTime.Now;
            message += $"[{now.ToShortDateString()} {now.ToShortTimeString()}] {message}";

            using (var repo = new LibGit2Sharp.Repository(_repositoryFolderName))
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
                    Signature sig = new Signature("auger", "admin@auger.in", DateTimeOffset.Now);

                    try
                    {
                        commitId = _Retry(() =>
                        {
                            var commit = repo.Commit(message, sig, sig);
                            return commit.Sha;
                        });

                        if (repo.Tags.Any(t => t.FriendlyName == "LATEST"))
                        {
                            repo.Tags.Remove("LATEST");
                        }
                        repo.ApplyTag("LATEST");
                        //repo.ApplyTag("DATETIME", sig, now.Ticks.ToString());
                    }
                    catch (EmptyCommitException) { } // ignore this but throw others
                }
                else if (!repo.Commits.Any())
                {
                    // This is the first commit for this repository

                    // TODO: Get Signature info from configuration file
                    Signature sig = new Signature("auger", "admin@auger.in", DateTimeOffset.Now);

                    commitId = _Retry(() => {
                        var commit = repo.Commit(message, sig, sig);
                        return commit.Sha;
                    });

                    repo.ApplyTag("LATEST");
                    //repo.ApplyTag("DATETIME", sig, now.Ticks.ToString());
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

        public string CommitFromRepository(Repository otherRepository)
        {
            try
            {
                Checkout();   // Move HEAD to LATEST checkout
                CopyFromRepository(otherRepository);
                return Commit();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
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

        public string CommitFromZip(Stream zipStream)
        {
            try
            {
                Checkout();   // Move HEAD to LATEST checkout
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
                        System.IO.Directory.CreateDirectory($"{_repositoryFolderName}\\{folder}");

                        var fileName = Path.GetFileName(zentry.FileName);
                        if (fileName.Length > 0)
                        {
                            using (var fstr = File.Create($"{_repositoryFolderName}\\{folder}\\{fileName}"))
                            {
                                zstr.CopyTo(fstr);
                                fstr.Flush(true);
                                fstr.Close();
                            }
                        }
                    }
                }
                return Commit();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
            }
        }

        // TODO: DELETE THIS METHOD
        public string CommitFromUrl(string url)
        {
            try
            {
                Checkout();   // Move HEAD to LATEST checkout
                _ClearRepository();     // Clear the current submission for complete replacement
                var path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(SubmissionRepository)).CodeBase);
                var baseUri = new Uri(new Uri(url), ".");
                var dirCount = baseUri.Segments.Length - 1;
                var startInfo = new ProcessStartInfo();
                startInfo.Arguments = $"-p -k -r -nH --cut-dirs={dirCount} {url}";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.WorkingDirectory = _repositoryFolderName;
                startInfo.FileName = $"{path}\\wget.exe";
                var proc = Process.Start(startInfo);
                proc.WaitForExit(5000);
                return Commit();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Unable to commit the assignment", e);
            }
        }

        private void _ClearRepository()
        {
            _Retry(() => {
                var repoDir = System.IO.Directory.CreateDirectory(_repositoryFolderName);
                foreach (var file in repoDir.GetFiles())
                {
                    if (!file.Attributes.HasFlag(FileAttributes.Hidden) && !file.Name.StartsWith("."))
                    {
                        file.Delete();
                    }
                }
                foreach (DirectoryInfo dir in repoDir.GetDirectories())
                {
                    if (!dir.Attributes.HasFlag(FileAttributes.Hidden) && !dir.Name.StartsWith("."))
                    {
                        dir.Delete(true);
                    }
                }
            });
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