using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class RepoFolder
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
        public List<RepoFolder> Folders { get; set; } = new List<RepoFolder>();
        public List<RepoFile> Files { get; set; } = new List<RepoFile>();
        public RepoFolder(string name, Uri uri)
        {
            name = name.Trim();
            this.Name = name;
            this.Uri = uri;
        }
    }
}
