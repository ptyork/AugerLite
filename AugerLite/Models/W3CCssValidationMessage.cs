using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class W3CCssValidationMessage
    {
        public enum MessageLevels
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }

        public string File { get; set; }

        public int Line { get; set; }
        public string Message { get; set; }
        public string Context { get; set; }
        public string Type { get; set; }
        public MessageLevels Level { get; set; }
    }
}
