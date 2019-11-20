using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NetworkMonitor;
using PluginMonitor;
using ClientMonitor;
using PSHelper;

namespace LWAHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkLogger _networkLogs;
        private PluginLogger _pluginLogs;
        private ClientPersistentLogger _clientLogs;

        public MainWindow()
        {
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);
            InitializeComponent();

            _networkLogs = new NetworkLogger(this.Dispatcher);
            _networkLogs.SessionStart += new NetworkLoggerSessionUpdatedHandler(network_NetworkSessionStart);
            _networkLogs.SessionEnd += new NetworkLoggerSessionUpdatedHandler(network_NetworkSessionEnd);
            _networkLogs.Initialize();

            _pluginLogs = new PluginLogger(this.Dispatcher);
            _pluginLogs.ActiveLogChanged += new PluginLoggerActiveLogChangedHandler(_pluginLogs_ActiveLogChanged);
            _pluginLogs.Initialize();
        }

        void  MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_networkLogs != null)
            {
                _networkLogs.DeInitialize(null);
                _networkLogs.SessionStart -= network_NetworkSessionStart;
                _networkLogs.SessionEnd -= network_NetworkSessionEnd;
                _networkLogs = null;
            }

            if (_pluginLogs != null)
            {
                _pluginLogs.DeInitialize();
                _pluginLogs = null;
            }
        }

        void network_NetworkSessionStart(object sender, NetworkLoggerSessionUpdatedEventArgs args)
        {
            NetworkSession session = args.Session;

            ListBoxItem item = new ListBoxItem();
            string data = session.ID.ToString() + " : " + session.URL + " , <pending> " + session.StatusCode.ToString() + "\nRequest=\n" + session.RequestDataAsString + "\nResponse=\n" + session.ResponseDataAsString;
            item.Content = data;
            network_log_data.Items.Add(item);
        }

        void network_NetworkSessionEnd(object sender, NetworkLoggerSessionUpdatedEventArgs args)
        {
            NetworkSession session = args.Session;

            ListBoxItem item = new ListBoxItem();
            string data = session.ID.ToString() + " : " + session.URL + " , " + session.StatusCode.ToString() + "\nRequest=\n" + session.RequestDataAsString + "\nResponse=\n" + session.ResponseDataAsString;
            item.Content = data;
            network_log_data.Items.Add(item);
        }

        void _pluginLogs_ActiveLogChanged(object sender, PluginLoggerActiveLogChangedEventArgs args)
        {
            if (args.OldLog != null)
            {
                args.OldLog.DataChanged -= log_DataChanged;
                args.OldLog.DataAppended -= log_DataAppended;
            }

            if (args.NewLog != null)
            {
                args.NewLog.DataChanged += new LogFileDataChangedHandler(log_DataChanged);
                args.NewLog.DataAppended += new LogFileDataAppendedHandler(log_DataAppended);
                plugin_log_data.Text = args.NewLog.Data;
            }
        }

        void log_DataChanged(object sender, LogFileDataChangedEventArgs args)
        {
            PluginMonitor.LogFile file = sender as PluginMonitor.LogFile;
            plugin_log_data.Text = file.Data;
        }

        void log_DataAppended(object sender, LogFileDataAppendedEventArgs args)
        {
            plugin_log_data.Text += args.Data;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            cb.SelectedIndex = 0;
        }
    }

//2. replacer
//4. bug filler
}
