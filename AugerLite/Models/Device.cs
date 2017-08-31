using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class Device
    {
        public static Device Large = new Device
        {
            DeviceId = "LG",
            DeviceName = "Large Desktop (1280px)",
            ViewportWidth = 1280,
            ViewportHeight = 1024
        };
        public static Device Medium = new Device
        {
            DeviceId = "MD",
            DeviceName = "Medium Device (1024px)",
            ViewportWidth = 1024,
            ViewportHeight = 768
        };
        public static Device Small = new Device
        {
            DeviceId = "SM",
            DeviceName = "Small Device (768px)",
            ViewportWidth = 768,
            ViewportHeight = 1024
        };
        public static Device ExtraSmall = new Device
        {
            DeviceId = "XS",
            DeviceName = "Extra Small Device (360px)",
            ViewportWidth = 360,
            ViewportHeight = 640
        };

        public static IEnumerable<Device> AllDevices
        {
            get
            {
                yield return Large;
                yield return Medium;
                yield return Small;
                yield return ExtraSmall;
            }
        }

        public static Device Parse(string deviceString)
        {
            switch (deviceString)
            {
                case "XS":
                    return ExtraSmall;
                case "SM":
                    return Small;
                case "MD":
                    return Medium;
            }
            return Large;
        }

        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int ViewportWidth { get; set; }
        public int ViewportHeight { get; set; }
    }
}
