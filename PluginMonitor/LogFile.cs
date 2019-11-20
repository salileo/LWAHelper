using System;
using System.IO;
using System.Text;

namespace PluginMonitor
{
    public delegate void LogFileDataChangedHandler(object sender, LogFileDataChangedEventArgs args);
    public class LogFileDataChangedEventArgs : EventArgs
    {
        public LogFileDataChangedEventArgs()
        {
        }
    }

    public delegate void LogFileDataAppendedHandler(object sender, LogFileDataAppendedEventArgs args);
    public class LogFileDataAppendedEventArgs : EventArgs
    {
        public LogFileDataAppendedEventArgs(string appendedString)
        {
            this.Data = appendedString;
        }

        public string Data { get; private set; }
    }

    public class LogFile
    {
        private PluginLogger _logger;
        private StringBuilder _data;
        private object _dataLock;
        private DateTime _fileCreationTime;
        private DateTime _fileWriteTime;
        private int _filePos;

        public LogFile(string filename, PluginLogger logger)
        {
            this.FileName = filename;
            _logger = logger;
            _filePos = 0;
            _dataLock = new object();

            _fileCreationTime = File.GetCreationTime(FileName);
            _fileWriteTime = File.GetLastWriteTime(FileName);

            FileStream stm = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            int len = (int)(stm.Length);
            byte[] bytes = new byte[len + 1];
            int actualLen = stm.Read(bytes, 0, len);
            bytes[actualLen] = 0;
            _filePos = actualLen;
            stm.Close();

            string str = Encoding.Unicode.GetString(bytes);
            lock (_dataLock)
            {
                _data = new StringBuilder(str);
            }

            _logger._uiDispatcher.Invoke(new FireDataChangedHandler(FireDataChanged));
        }

        private delegate void FireDataChangedHandler();
        private void FireDataChanged()
        {
            if (DataChanged != null)
                DataChanged(this, new LogFileDataChangedEventArgs());
        }

        internal void Refresh()
        {
            DateTime creationTime = File.GetCreationTime(FileName);
            DateTime writeTime = File.GetLastWriteTime(FileName);

            if (_fileCreationTime != creationTime)
            {
                _fileCreationTime = creationTime;
                _fileWriteTime = writeTime;

                FileStream stm = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                int len = (int)(stm.Length);
                byte[] bytes = new byte[len + 1];
                int actualLen = stm.Read(bytes, 0, len);
                bytes[actualLen] = 0;
                _filePos = actualLen;
                stm.Close();

                string str = Encoding.Unicode.GetString(bytes);
                lock (_dataLock)
                {
                    _data = new StringBuilder(str);
                }

                _logger._uiDispatcher.Invoke(new FireDataChangedHandler(FireDataChanged));
            }
            else if (_fileWriteTime != writeTime)
            {
                _fileWriteTime = writeTime;

                FileStream stm = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stm.Seek(_filePos, SeekOrigin.Begin);
                int len = (int)(stm.Length - stm.Position);
                byte[] bytes = new byte[len + 1];
                int actualLen = stm.Read(bytes, 0, len);
                bytes[actualLen] = 0;
                _filePos += actualLen;
                stm.Close();

                string str = Encoding.Unicode.GetString(bytes);
                lock (_dataLock)
                {
                    _data.Append(str);
                }

                _logger._uiDispatcher.Invoke(new FireDataAppendedHandler(FireDataAppended), str);
            }
        }

        private delegate void FireDataAppendedHandler(string str);
        private void FireDataAppended(string str)
        {
            if (DataAppended != null)
                DataAppended(this, new LogFileDataAppendedEventArgs(str));
        }

        public string FileName { get; private set; }
        public string Data
        {
            get
            {
                lock (_dataLock)
                {
                    return _data.ToString();
                }
            }
        }

        public event LogFileDataChangedHandler DataChanged;
        public event LogFileDataAppendedHandler DataAppended;
    }
}
