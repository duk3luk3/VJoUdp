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
        private Timer tmr = null;

        private void Tick(object state)
        {
            if (!running)
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
                s.run();

                mut.WaitOne();
                alive = running;
                mut.ReleaseMutex();
            }

            s.shutdown();
        }



        public MainWindow()
        {
            InitializeComponent();
        }

        Thread serverThread;

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            short port;
            if (Int16.TryParse(tbPort.Text, out port) && !running)
            {
                running = true;
                btnStart.IsEnabled = false;
                btnStop.IsEnabled = true;

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
        }
    }
}
