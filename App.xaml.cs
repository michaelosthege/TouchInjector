using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace TouchInjector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Dictionary<AvailableParameter, bool> StartParameters { get; set; }
        public enum AvailableParameter { minimized }//TODO: Startup parameter max_touch ddd

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            StartParameters = new Dictionary<AvailableParameter, bool>();

            foreach (AvailableParameter param in Enum.GetValues(typeof(AvailableParameter)))
                StartParameters.Add(param, e.Args.Contains("-" + param.ToString("g")));

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow();
            if (!StartParameters[AvailableParameter.minimized])
                mainWindow.Show();//show only if we don't start minimized
            mainWindow.Initialize();
        }
    }
}
