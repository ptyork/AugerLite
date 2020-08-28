using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class W3CHtmlValidationMessage
    {
        public enum MessageTypes
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            [EnumMember(Value = "non-document-error")]
            NonDocumentError = 3
        }

        public string Page { get; set; }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public MessageTypes Type { get; set; }

        public string Message { get; set; }

        public int FirstColumn { get; set; } = -1;
        public int LastColumn { get; set; } = -1;
        public int LastLine { get; set; } = -1;

        public string Extract { get; set; }
        public int HiliteStart { get; set; } = -1;
        public int HiliteLength { get; set; } = -1;
    }
}
