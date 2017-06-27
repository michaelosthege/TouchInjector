using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using TCD.System.ApplicationExtensions;
using TCD.System.TouchInjection;
using TCD.Sys.TUIO;
using System.Drawing;
using TUIO;

namespace TouchInjector
{
    public partial class MainWindow : Window
    {
        //TODO: in the app.manifest (in the Properties folder), the uiaccess-variable is set to true. In theory this should be the basis to the ability to send touch events 
        //to applications running under the secure desktop (such as the Universal Access Control aka. UAC). According to an MSDN article, UIAccess-applications need to be certified
        //however, I was unable to get it working using a self-signed (and self-trusted) certificate.
        //If anyone succeeds in enabling UIAccess, please contact Michael Osthege via thecakedev@hotmail.com

        #region Backend Variables
        TuioChannel channel;
        private uint maxTouchPoints = 256;
        private uint frameCount = 0;
        private const int checkScreenEvery = 100;
        Rectangle targetArea;
        #endregion

        #region UI Variables
        private NotifyIcon trayIcon;
        private bool closeIt;
        #endregion

        #region Lifecycle
        public MainWindow()
        {
            InitializeComponent();
            channel = new TuioChannel();
            TuioChannel.OnTuioRefresh += TuioChannel_OnTuioRefresh;
            InitTrayIcon();
            SetStatus(TouchInjectorStatus.Initializing);
        }
        public async void Initialize()
        {
            ScanScreens();
            feedbackCheckBox.IsChecked = Properties.Settings.Default.TouchFeedback;
            portBox.Text = Properties.Settings.Default.ListeningPort.ToString();
            bool touch = await InitTouch();
            bool tuio = await Connect();
            touchInjectionStatus.Content = (touch) ? Properties.Resources.TouchReady : Properties.Resources.TouchError;
            SetStatus((touch && tuio) ? TouchInjectorStatus.Ready : TouchInjectorStatus.Error);
            autostartCheckBox.IsChecked = await ApplicationAutostart.IsAutostartAsync("TouchInjector");
            autostartCheckBox.Checked += CheckBox_Changed;
            autostartCheckBox.Unchecked += CheckBox_Changed;
        }
        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
                this.Hide();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.WindowState == WindowState.Normal && !closeIt)//the user clicked the '[X]' control
            {
                e.Cancel = true;//only close via the tray context menu
                this.Hide();//minimize to tray
            }
            else
            {
                trayIcon.Visible = false;//delete tray icon
                channel.Disconnect();//disconnect tuio
                base.OnClosing(e);
            }
        }
        #endregion

        #region Initialization
        private void InitTrayIcon()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Click += new EventHandler(delegate
                {
                    this.Show();
                    this.Activate();
                });
            System.Windows.Forms.MenuItem quitItem = new System.Windows.Forms.MenuItem(Properties.Resources.TrayQuit, new EventHandler(delegate
            {
                closeIt = true;
                this.Close();
            })) { Shortcut = Shortcut.CtrlQ, ShowShortcut = false };
            System.Windows.Forms.MenuItem feedbackItem = new System.Windows.Forms.MenuItem(Properties.Resources.TrayFeedback, new EventHandler(delegate
            {
                System.Diagnostics.Process.Start("mailto:" + Properties.Resources.DeveloperEmail + "?subject=TouchInjector Feedback");
            })) { Shortcut = Shortcut.CtrlF, ShowShortcut = false };
            System.Windows.Forms.MenuItem helpItem = new System.Windows.Forms.MenuItem(Properties.Resources.TrayHelp, new EventHandler(delegate
            {
                Version v = Assembly.GetExecutingAssembly().GetName().Version;
                System.Windows.MessageBox.Show(string.Format("Version: {0}\n", v) + Properties.Resources.HelpText, Properties.Resources.TrayHelp, MessageBoxButton.OK);
            })) { Shortcut = Shortcut.CtrlH, ShowShortcut = false };
            trayIcon.ContextMenu = new System.Windows.Forms.ContextMenu(new System.Windows.Forms.MenuItem[] { helpItem, feedbackItem, quitItem });
            trayIcon.Visible = true;
        }
        private async Task<bool> Connect()
        {
            await Task.Delay(0);
            portBox.IsEnabled = false;//lock the port box
            if (string.IsNullOrWhiteSpace(portBox.Text)) portBox.Text = "3333";
            Properties.Settings.Default.ListeningPort = Convert.ToInt32(portBox.Text);
            Properties.Settings.Default.Save();
            return channel.Connect(Properties.Settings.Default.ListeningPort);            
        }
        private async Task<bool> InitTouch()
        {
            await Task.Delay(0);
            feedbackCheckBox.IsEnabled = false;//lock the feedback box
            Boolean showFeedback = Convert.ToBoolean(feedbackCheckBox.IsChecked);
            return TCD.System.TouchInjection.TouchInjector.InitializeTouchInjection((uint)maxTouchPoints, showFeedback ? TouchFeedback.INDIRECT : TouchFeedback.NONE);
        }
        #endregion
     
        #region UI/Management
        //Refreshing
        private void SetStatus(TouchInjectorStatus status)
        {
            tuioStatus.Content = (channel.IsRunning) ? Properties.Resources.TouchReady : (status == TouchInjectorStatus.Deactivated) ? Properties.Resources.TuioDeactivated : (status == TouchInjectorStatus.Initializing) ? Properties.Resources.TuioInitializing : Properties.Resources.TuioError;
            startStopButton.IsEnabled = (status != TouchInjectorStatus.Initializing);
            portBox.IsEnabled = !channel.IsRunning;
            feedbackCheckBox.IsEnabled = !channel.IsRunning;
            trayIcon.Text = "TouchInjector - " + status;
            switch (status)
            {
                case TouchInjectorStatus.Deactivated:
                    this.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/iconGray.ico"));
                    trayIcon.Icon = Properties.Resources.iconGray;
                    startStopButton.Content = Properties.Resources.ControlStart;
                    break;
                case TouchInjectorStatus.Error:
                    this.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/iconRed.ico"));
                    trayIcon.Icon = Properties.Resources.iconRed;
                    startStopButton.Content = Properties.Resources.ControlRetry;
                    break;
                case TouchInjectorStatus.Initializing:
                    this.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/iconYellow.ico"));
                    trayIcon.Icon = Properties.Resources.iconYellow;
                    startStopButton.Content = Properties.Resources.ControlStart;
                    break;
                case TouchInjectorStatus.Ready:
                    this.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Resources/iconGreen.ico"));
                    trayIcon.Icon = Properties.Resources.iconGreen;
                    startStopButton.Content = Properties.Resources.ControlStop;
                    break;
            }
        }
        //Start/Stop
        private async void StartStop_Click(object sender, RoutedEventArgs e)
        {
            bool error = false;
            if (channel.IsRunning)
                channel.Disconnect();
            else
            {
                error = !await InitTouch();
                error = !await Connect();
            }
            SetStatus((channel.IsRunning) ? TouchInjectorStatus.Ready : (error) ? TouchInjectorStatus.Error : TouchInjectorStatus.Deactivated);
        }
        //Autostart
        private async void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            autostartCheckBox.Checked -= CheckBox_Changed;
            autostartCheckBox.Unchecked -= CheckBox_Changed;
            //
            bool succ = await ApplicationAutostart.SetAutostartAsync((bool)autostartCheckBox.IsChecked, "TouchInjector", Properties.Resources.AutostartTaskDescription, "-minimized", true);
            autostartCheckBox.IsChecked = (autostartCheckBox.IsChecked == succ);
            //
            autostartCheckBox.Checked += CheckBox_Changed;
            autostartCheckBox.Unchecked += CheckBox_Changed;
        }
        #endregion

        #region Screens, TUIO and Touch
        private void screenSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            targetArea = ((screenSelector.SelectedItem as System.Windows.Controls.ComboBoxItem).Tag as Screen).Bounds;
        }
        private void ScanScreens()
        {
            System.Windows.Controls.ComboBoxItem autoItem = new System.Windows.Controls.ComboBoxItem() { Content = "Primary" };
            foreach (MonitorInfo mi in MonitorInfo.GetAllMonitors())
            {
                screenSelector.Items.Add(new System.Windows.Controls.ComboBoxItem() { Content = mi.FriendlyName, Tag = mi.Screen });
                if (mi.Screen.Primary)
                    autoItem.Tag = mi.Screen;
            }
            screenSelector.Items.Insert(0, autoItem);
            screenSelector.SelectedIndex = 0;
            //TODO: Inject to all screens
            //screenSelector.Items.Add(new System.Windows.Controls.ComboBoxItem() { Content = "All" });
        }
        private void TuioChannel_OnTuioRefresh(TuioTime t)
        {
            //TODO: re-enable frequent screen monitoring
            //if (frameCount % checkScreenEvery == 0)
            //    ScanScreens();
            //loop through the TuioObjects
            List<PointerTouchInfo> toFire = new List<PointerTouchInfo>();
            List<long> removeList = new List<long>();
            foreach (var kvp in channel.CursorList)
            {
                TuioCursor cur = kvp.Value.TuioCursor;
                IncomingType type = kvp.Value.Type;
                int[] injectionCoordinates = ToInjectionCoordinates(cur.X, cur.Y);
                int radius = 12;
                //make a new pointertouchinfo with all neccessary information
                PointerTouchInfo contact = new PointerTouchInfo();
                contact.PointerInfo.pointerType = PointerInputType.TOUCH;
                contact.TouchFlags = TouchFlags.NONE;
                //contact.Orientation = (uint)cur.getAngleDegrees();//this is only valid for TuioObjects
                contact.Pressure = 1024;
                contact.TouchMasks = TouchMask.CONTACTAREA | TouchMask.ORIENTATION | TouchMask.PRESSURE;
                contact.PointerInfo.PtPixelLocation.X = injectionCoordinates[0];
                contact.PointerInfo.PtPixelLocation.Y = injectionCoordinates[1];
                contact.PointerInfo.PointerId = (uint)cur.CursorID;

                contact.ContactArea.left = injectionCoordinates[0] - radius;
                contact.ContactArea.right = injectionCoordinates[0] + radius;
                contact.ContactArea.top = injectionCoordinates[1] - radius;
                contact.ContactArea.bottom = injectionCoordinates[1] + radius;
                //contact.PointerInfo.FrameId = frameCount;

                //set the right flags
                if (type == IncomingType.New)
                {
                    contact.PointerInfo.PointerFlags = PointerFlags.DOWN | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                    kvp.Value.Type = IncomingType.Update;
                }
                else if (type == IncomingType.Update)
                    contact.PointerInfo.PointerFlags = PointerFlags.UPDATE | PointerFlags.INRANGE | PointerFlags.INCONTACT;
                else if (type == IncomingType.Remove)
                {
                    contact.PointerInfo.PointerFlags = PointerFlags.UP;
                    removeList.Add(kvp.Key);
                }

                //add it to 'toFire'
                toFire.Add(contact);
            }

            //fire the events
            bool success = TCD.System.TouchInjection.TouchInjector.InjectTouchInput(toFire.Count, toFire.ToArray());

            //remove those with type == IncomingType.Remove
            foreach (long key in removeList)
                channel.CursorList.Remove(key);//remove from the tuio channel

            //count up
            frameCount++;
        }

        private int[] ToInjectionCoordinates(float x, float y)
        {
            int[] result = new int[2];            
            result[0] = targetArea.X + (int)Math.Round(x * targetArea.Width);
            result[1] = targetArea.Y + (int)Math.Round(y * targetArea.Height);
            return result;
        }
        #endregion

        private void feedbackCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Boolean showFeedback = Convert.ToBoolean(feedbackCheckBox.IsChecked);
            Properties.Settings.Default.TouchFeedback = showFeedback;
            Properties.Settings.Default.Save();
        }
    }
    enum TouchInjectorStatus
    {
        Initializing, Ready, Deactivated, Error
    }
}
