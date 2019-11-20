using System;
using System.IO;
using System.Text;

namespace ClientMonitor
{
    public delegate void LogFileDataAddedHandler(object sender, LogFileDataAddedEventArgs args);
    public class LogFileDataAddedEventArgs : EventArgs
    {
        public LogFileDataAddedEventArgs(string appendedString)
        {
            this.Data = appendedString;
        }

        public string Data { get; private set; }
    }

    public class LogFile
    {
        private FileStream _stream = null;
        private StringBuilder _data;
        private object _dataLock;
        private ClientPersistentLogger _logger;

        public LogFile(string filename, ClientPersistentLogger logger)
        {
            this.FileName = filename;
            _logger = logger;
            _data = new StringBuilder();
            _dataLock = new object();

            _stream = File.OpenRead(filename);
            _stream.Seek(0, SeekOrigin.End);

            string str = File.ReadAllText(filename);
            lock (_dataLock)
            {
                _data.Append(str);
            }

            _logger._uiDispatcher.Invoke(new FireDataAddedHandler(FireDataAdded), str);
        }

        internal void Refresh()
        {
            int len = (int)(_stream.Length - _stream.Position);
            byte[] bytes = new byte[len + 1];
            int actualLen = _stream.Read(bytes, 0, len);
            bytes[actualLen] = 0;

            string str = Encoding.UTF8.GetString(bytes);
            lock (_dataLock)
            {
                _data.Append(str);
            }

            _logger._uiDispatcher.Invoke(new FireDataAddedHandler(FireDataAdded), str);
        }

        private delegate void FireDataAddedHandler(string str);
        private void FireDataAdded(string str)
        {
            if (DataAdded != null)
                DataAdded(this, new LogFileDataAddedEventArgs(str));
        }

        internal void Close()
        {
            _stream.Close();
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

        public event LogFileDataAddedHandler DataAdded;
    }
}
