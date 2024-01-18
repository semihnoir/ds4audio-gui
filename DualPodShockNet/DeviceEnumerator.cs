using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DS4Windows;

namespace DualPodShockNet
{
    static class DeviceEnumerator
    {
        internal const int SONY_VID = 0x054C;

        private static DS4Windows.VidPidInfo[] knownDevices =
        {
            new DS4Windows.VidPidInfo(SONY_VID, 0x5C4, "DS4 v.1"),
            new DS4Windows.VidPidInfo(SONY_VID, 0x09CC, "DS4 v.2"),
        };

        public static List<HidDevice> FindDevices()
        {
            IEnumerable<HidDevice> hDevices = HidDevices.EnumerateDS4(knownDevices);
            List<HidDevice> tempList = hDevices.ToList();
            return tempList;
        }
        
        public static void SwitchModes(HidDevice hidDevice)
        {

        }
    }
}
