using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FileType
    {
        html,
        css,
        script,
        image,
        other
    }

    public class RepoFile
    {
        public string Name { get; set; }
        public FileType Type { get; set; }
        public RepoFile(string name)
        {
            name = name.Trim();
            this.Name = name;

            var ext = name.Split('.').Last().ToLowerInvariant();
            switch (ext)
            {
                case "htm":
                case "html":
                    this.Type = FileType.html;
                    break;
                case "css":
                    this.Type = FileType.css;
                    break;
                case "js":
                    this.Type = FileType.script;
                    break;
                case "bmp":
                case "gif":
                case "jpg":
                case "jpeg":
                case "png":
                case "svg":
                case "tiff":
                    this.Type = FileType.image;
                    break;
                default:
                    this.Type = FileType.other;
                    break;
            }
        }
    }

}
