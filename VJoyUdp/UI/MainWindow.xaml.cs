#define USE_TASKBAR

#if USE_TASKBAR
using Hardcodet.Wpf.TaskbarNotification;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VJoyUdp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Mutex mut = new Mutex();
        private static bool running = false;
        private static bool active = false;
        private Timer tmr = null;

        private void Tick(object state)
        {
            if (running)
            {
#if USE_TASKBAR
                if (active)
                {

                    this.Dispatcher.Invoke(() =>
                    {
                        ico.Icon = Properties.Resources.PlayIcon;
                    });
                    active = false;
                }
                else
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ico.Icon = Properties.Resources.PlayIconGray;
                    });
                }
#endif
            }
            else
            {
                if (!serverThread.IsAlive)
                {
                    //todo: dispatch to UI thread
                    this.Dispatcher.Invoke(() =>
                    {
                        btnStart.IsEnabled = true;
                        btnStop.IsEnabled = false;
                        tmr.Dispose();
                        tmr = null;
                    });
                }
            }
        }

        private static void thread(object param)
        {
            int[] parms = (int[])param;
            int port = parms[0];
            int idx = parms[1];

            VJoyUdp.Controller.VJoyUdpServer s = new Controller.VJoyUdpServer(port, idx);

            mut.WaitOne();
            bool alive = running;
            mut.ReleaseMutex();

            while (alive)
            {
                bool tmp_active = s.run();

                mut.WaitOne();
                alive = running;
                active = active || tmp_active;
                mut.ReleaseMutex();
            }

            s.shutdown();
        }

#if USE_TASKBAR
        TaskbarIcon ico;

        public MainWindow()
        {
            InitializeComponent();

            ico = new TaskbarIcon();
            ico.Icon = Properties.Resources.PauseIcon;
            ico.Visibility = System.Windows.Visibility.Visible;
            
        }
#else
        public MainWindow()
        {
            InitializeComponent();
        }
#endif

        Thread serverThread;

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            short port;
            if (Int16.TryParse(tbPort.Text, out port) && !running)
            {
                running = true;
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;
#if USE_TASKBAR
                ico.Icon = Properties.Resources.PlayIconGray;
#endif

                serverThread = new Thread(MainWindow.thread);
                serverThread.Start(new int[] { port, cbJoyIdx.SelectedIndex });

                if (tmr == null)
                {
                    tmr = new Timer(Tick, null, 500, 500);
                }
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mut.WaitOne();
            running = false;
            mut.ReleaseMutex();

            btnStop.IsEnabled = false;

#if USE_TASKBAR
            ico.Icon = Properties.Resources.PauseIcon;
#endif
        }
    }
}
