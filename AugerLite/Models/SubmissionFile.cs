using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public enum FileType
    {
        HTML,
        CSS,
        Script,
        Image,
        Other
    }

    public class SubmissionFile
    {
        public string Name { get; set; }
        public FileType Type { get; set; }
        public SubmissionFile(string name)
        {
            name = name.Trim();
            this.Name = name;

            var ext = name.Split('.').Last().Trim();
            switch (ext)
            {
                case "htm":
                case "html":
                    this.Type = FileType.HTML;
                    break;
                case "css":
                    this.Type = FileType.CSS;
                    break;
                case "js":
                    this.Type = FileType.Script;
                    break;
                case "bmp":
                case "gif":
                case "jpg":
                case "jpeg":
                case "png":
                case "svg":
                case "tiff":
                    this.Type = FileType.Image;
                    break;
                default:
                    this.Type = FileType.Other;
                    break;
            }
        }
    }

    public class SubmissionFolder
    {
        public string Name { get; set; }
        public Uri Uri { get; set; }
        public List<SubmissionFolder> Folders { get; set; } = new List<SubmissionFolder>();
        public List<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
        public SubmissionFolder(string name, Uri uri)
        {
            name = name.Trim();
            this.Name = name;
            this.Uri = uri;
        }
    }

}
