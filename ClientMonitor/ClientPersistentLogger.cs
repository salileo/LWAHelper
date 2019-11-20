using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Threading;

namespace ClientMonitor
{
    public delegate void ClientPersistentLoggerActiveLogChangedHandler(object sender, ClientPersistentLoggerActiveLogChangedEventArgs args);
    public class ClientPersistentLoggerActiveLogChangedEventArgs : EventArgs
    {
        public ClientPersistentLoggerActiveLogChangedEventArgs(LogFile newLog, LogFile oldLog)
        {
            this.NewLog = newLog;
            this.OldLog = oldLog;
        }

        public LogFile NewLog { get; private set; }
        public LogFile OldLog { get; private set; }
    }

    public class ClientPersistentLogger
    {
        private Dictionary<string, LogFile> _files;
        private LogFile _activeLog;
        private Timer _refreshTimer;
        private bool _processingTimer;
        internal Dispatcher _uiDispatcher;

        public ClientPersistentLogger(Dispatcher uiDispatcher)
        {
            _files = new Dictionary<string, LogFile>();
            _activeLog = null;
            _refreshTimer = null;
            _processingTimer = false;
            _uiDispatcher = uiDispatcher;
        }

        public LogFile ActiveLog
        {
            get { return _activeLog; }
            private set
            {
                if (_activeLog != value)
                {
                    LogFile oldLog = _activeLog;
                    _activeLog = value;

                    _uiDispatcher.Invoke(new FireActiveLogChangedHandler(FireActiveLogChanged), _activeLog, oldLog);
                }
            }
        }

        private delegate void FireActiveLogChangedHandler(LogFile newLog, LogFile oldLog);
        private void FireActiveLogChanged(LogFile newLog, LogFile oldLog)
        {
            if (ActiveLogChanged != null)
                ActiveLogChanged(this, new ClientPersistentLoggerActiveLogChangedEventArgs(newLog, oldLog));
        }

        public event ClientPersistentLoggerActiveLogChangedHandler ActiveLogChanged;

        public void Initialize()
        {
            _refreshTimer = new Timer(new TimerCallback(OnTimer), null, 0, 1000);
        }

        private void OnTimer(object state)
        {
            if (_processingTimer)
                return;

            _processingTimer = true;
            RefreshStatus();
            _processingTimer = false;
        }

        public void DeInitialize()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Dispose();
                _refreshTimer = null;
            }
        }

        private void RefreshStatus()
        {
            string logFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\LWATracing";
            if (!Directory.Exists(logFolder))
            {
                _files.Clear();
                ActiveLog = null;
                return;
            }

            string[] files = null;
            try
            {
                files = Directory.GetFiles(logFolder, "LWAJSPersistent*.log", SearchOption.TopDirectoryOnly);
            }
            catch (Exception) { }

            if ((files == null) || (files.Length == 0))
            {
                _files.Clear();
                ActiveLog = null;
                return;
            }

            Dictionary<string, LogFile> clone = _files;
            _files = new Dictionary<string, LogFile>();

            foreach (string filename in files)
            {
                try
                {
                    LogFile log = null;
                    if (clone.ContainsKey(filename))
                    {
                        log = clone[filename];
                        clone.Remove(filename);
                    }
                    else
                    {
                        log = new LogFile(filename, this);
                    }

                    _files[filename] = log;
                    log.Refresh();

                    if (ActiveLog == null)
                    {
                        ActiveLog = log;
                    }
                    else
                    {
                        if (File.GetLastWriteTime(filename) > File.GetLastWriteTime(ActiveLog.FileName))
                            ActiveLog = log;
                    }
                }
                catch (Exception) { }
            }

            foreach (KeyValuePair<string, LogFile> pair in clone)
                pair.Value.Close();

            clone.Clear();
        }
    }
}
