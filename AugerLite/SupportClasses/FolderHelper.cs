using Auger.Models;
using Auger.Models.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger
{
    public static class FolderHelper
    {
        public static RepoFolder GetFolder(this Repository repo)
        {
            RepoFolder folder = new RepoFolder("_root", repo.FileUri);
            _FillFolder(repo.Folder, folder);
            return folder;
        }

        public static RepoFolder GetFolder(this TempDir dir)
        {
            RepoFolder folder = new RepoFolder("_root", dir.FileUri);
            _FillFolder(dir.Directory, folder);
            return folder;
        }

        private static void _FillFolder(DirectoryInfo directory, RepoFolder folder)
        {
            foreach (var dirinfo in directory.GetDirectories())
            {
                if (dirinfo.Attributes.HasFlag(FileAttributes.Hidden) || dirinfo.Name.StartsWith("."))
                {
                    continue;
                }
                var subFolder = new RepoFolder(dirinfo.Name, new Uri(folder.Uri, dirinfo.Name));
                _FillFolder(dirinfo, subFolder);
                folder.Folders.Add(subFolder);
            }

            foreach (var fileinfo in directory.GetFiles())
            {
                if (fileinfo.Attributes.HasFlag(FileAttributes.Hidden) || fileinfo.Name.StartsWith("."))
                {
                    continue;
                }
                var file = new RepoFile(fileinfo.Name);
                folder.Files.Add(file);
            }

            var defaultfile = (from f in folder.Files
                               where f.Name.ToLowerInvariant().StartsWith("index.") || f.Name.ToLowerInvariant().StartsWith("default.")
                               where f.Type == FileType.html
                               select f).FirstOrDefault();

            if (defaultfile != null)
            {
                folder.Files.Remove(defaultfile);
                folder.Files.Insert(0, defaultfile);
            }

        }
    }
}
