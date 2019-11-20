using System;
using System.Collections.Generic;
using System.Threading;
using Fiddler;
using System.Windows.Threading;

namespace NetworkMonitor
{
    public delegate void NetworkLoggerSessionUpdatedHandler(object sender, NetworkLoggerSessionUpdatedEventArgs args);
    public class NetworkLoggerSessionUpdatedEventArgs : EventArgs
    {
        public NetworkLoggerSessionUpdatedEventArgs(NetworkSession session)
        {
            this.Session = session;
        }

        public NetworkSession Session { get; private set; }
    }

    public class NetworkLogger
    {
        private Dictionary<Session, NetworkSession> _sessions;
        private object _sessionLock;
        private Dispatcher _uiDispatcher;

        private Proxy oSecureEndpoint = null;
        private string sSecureEndpointHostname = "localhost";

        public NetworkLogger(Dispatcher uiDispatcher)
        {
            _sessions = new Dictionary<Session, NetworkSession>();
            _sessionLock = new object();
            _uiDispatcher = uiDispatcher;
        }

        public event NetworkLoggerSessionUpdatedHandler SessionStart;
        public event NetworkLoggerSessionUpdatedHandler SessionEnd;

        public void Initialize()
        {
            //FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
            //FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };

            FiddlerApplication.BeforeRequest += new SessionStateHandler(BeforeRequestCallback);
            FiddlerApplication.BeforeResponse += new SessionStateHandler(BeforeResponseCallback);
            FiddlerApplication.AfterSessionComplete += new SessionStateHandler(AfterSessionCompleteCallback);
            CONFIG.IgnoreServerCertErrors = false;
            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);

            // For forward-compatibility with updated FiddlerCore libraries, it is strongly recommended that you 
            // start with the DEFAULT options and manually disable specific unwanted options.
            FiddlerCoreStartupFlags oFCSF = FiddlerCoreStartupFlags.Default;

            // E.g. uncomment the next line if you don't want FiddlerCore to act as the system proxy
            // oFCSF = (oFCSF & ~FiddlerCoreStartupFlags.RegisterAsSystemProxy);
            // or uncomment the next line if you don't want to decrypt SSL traffic.
            // oFCSF = (oFCSF & ~FiddlerCoreStartupFlags.DecryptSSL);
            //
            // NOTE: Because we haven't disabled the option to decrypt HTTPS traffic, makecert.exe 
            // must be present in this executable's folder.
            const int listenPort = 8877;
            FiddlerApplication.Startup(listenPort, oFCSF);
            oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(7777, true, sSecureEndpointHostname);
        }

        public void DeInitialize(Exception exp)
        {
            if (exp != null)
                FiddlerApplication.ReportException(exp, "UnhandledException");

            //do the most important thing first
            //this will revert the system proxy settings
            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();
            FiddlerApplication.Shutdown();
            Thread.Sleep(500);

            lock (_sessionLock)
            {
                _sessions.Clear();
            }
        }

        private void BeforeRequestCallback(Session oSession)
        {
            string url = oSession.url.ToLower();
            if (url.Contains("microsoft.com") &&
                (url.Contains("/meet/") ||
                 url.Contains("/lwa/") ||
                 url.Contains("/ucwa/") ||
                 url.Contains("/datacollabweb/") ||
                 url.Contains("/reach/") ||
                 url.Contains("/webticket/")))
            {
                lock (_sessionLock)
                {
                    NetworkSession session = new NetworkSession(oSession);
                    _sessions[oSession] = session;

                    // In order to enable response tampering, buffering mode must
                    // be enabled; this allows FiddlerCore to permit modification of
                    // the response in the BeforeResponse handler rather than streaming
                    // the response to the client as the response comes in.
                    oSession.bBufferResponse = true;

                    _uiDispatcher.Invoke(new FireSessionStartMethod(FireSessionStart), session);
                }
            }

            if ((oSession.hostname == sSecureEndpointHostname) && (oSession.port == 7777))
            {
                oSession.utilCreateResponseAndBypassServer();
                oSession.oResponse.headers.HTTPResponseStatus = "200 Ok";
                oSession.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                oSession.oResponse["Cache-Control"] = "private, max-age=0";
                oSession.utilSetResponseBody("<html><body>Request for https://" + sSecureEndpointHostname + ":7777 received. Your request was:<br/><plaintext>" + oSession.oRequest.headers.ToString());
            }
        }

        private delegate void FireSessionStartMethod(NetworkSession session);
        private void FireSessionStart(NetworkSession session)
        {
            if (SessionStart != null)
                SessionStart(this, new NetworkLoggerSessionUpdatedEventArgs(session));
        }

        private void BeforeResponseCallback(Session oSession)
        {
            //nothing to do here
        }

        private void AfterSessionCompleteCallback(Session oSession)
        {
            string url = oSession.url.ToLower();
            if (url.Contains("microsoft.com") &&
                (url.Contains("/meet/") ||
                 url.Contains("/lwa/") ||
                 url.Contains("/ucwa/") ||
                 url.Contains("/datacollabweb/") ||
                 url.Contains("/reach/") ||
                 url.Contains("/webticket/")))
            {
                lock (_sessionLock)
                {
                    NetworkSession session = null;
                    if (_sessions.ContainsKey(oSession))
                    {
                        session = _sessions[oSession];
                        _sessions.Remove(oSession);
                    }
                    else
                    {
                        session = new NetworkSession(oSession);
                    }

                    _uiDispatcher.Invoke(new FireSessionEndMethod(FireSessionEnd), session);
                }
            }
        }

        private delegate void FireSessionEndMethod(NetworkSession session);
        private void FireSessionEnd(NetworkSession session)
        {
            if (SessionEnd != null)
                SessionEnd(this, new NetworkLoggerSessionUpdatedEventArgs(session));
        }
    }
}
