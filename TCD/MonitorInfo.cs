using System.Collections.Generic;
using System.Windows.Forms;

namespace TCD.System.ApplicationExtensions
{
    public class MonitorInfo
    {
        public string FriendlyName { get; set; }
        public Screen Screen { get; set; }

        public static MonitorInfo[] GetAllMonitors()
        {
            List<MonitorInfo> monitors = new List<MonitorInfo>();
            foreach (Screen s in Screen.AllScreens)
            {
                MonitorInfo mi = new MonitorInfo();
                mi.Screen = s;
                mi.FriendlyName = s.DeviceName.Replace("\\\\.\\DISPLAY", "Monitor ") + string.Format(" ({0}x{1})", s.Bounds.Width, s.Bounds.Height);
                monitors.Add(mi);
            }
            return monitors.ToArray();
        }
    }
}