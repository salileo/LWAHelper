using System.Collections.Generic;
using Fiddler;

namespace NetworkMonitor
{
    public enum NetworkDataType
    {
        Unknown,
        Text,
        Image,
        JSON,
        XML,
        Multipart
    }

    public class NetworkSession
    {
        private Session _session;

        private Dictionary<string, string> _requestHeaders;
        private NetworkDataType _requestDataType;

        private Dictionary<string, string> _responseHeaders;
        private NetworkDataType _responseDataType;

        public NetworkSession(Session session)
        {
            _session = session;

            _requestHeaders = new Dictionary<string, string>();
            _requestDataType = NetworkDataType.Unknown;

            _responseHeaders = new Dictionary<string, string>();
            _responseDataType = NetworkDataType.Unknown;

            ParseRequest();
        }

        public int ID { get { return _session.id; } }
        public string URL { get { return _session.fullUrl; } }
        public int StatusCode { get { return _session.responseCode; } }

        public Dictionary<string, string> RequestHeaders { get { return _requestHeaders; } }
        public string RequestDataAsString { get { return _session.GetRequestBodyAsString(); } }
        public byte[] RequestDataAsBytes { get { return _session.requestBodyBytes; } }

        public Dictionary<string, string> ResponseHeaders { get { return _responseHeaders; } }
        public string ResponseDataAsString { get { return _session.GetResponseBodyAsString(); } }
        public byte[] ResponseDataAsBytes { get { return _session.responseBodyBytes; } }

        public void SaveToFile(string filename)
        {
            _session.SaveSession(filename, false);
        }

        internal void ParseRequest()
        {
            _requestHeaders.Clear();
            HTTPRequestHeaders headers = _session.oRequest.headers;
            foreach (HTTPHeaderItem item in headers)
                _requestHeaders.Add(item.Name, item.Value);

            string contentType = string.Empty;
            if (_requestHeaders.TryGetValue("content-type", out contentType) && !string.IsNullOrEmpty(contentType))
                _requestDataType = GetType(contentType);
        }

        internal void ParseResponse()
        {
            _responseHeaders.Clear();
            HTTPResponseHeaders headers = _session.oResponse.headers;
            foreach (HTTPHeaderItem item in headers)
                _responseHeaders.Add(item.Name, item.Value);

            string contentType = string.Empty;
            if (_responseHeaders.TryGetValue("content-type", out contentType) && !string.IsNullOrEmpty(contentType))
                _responseDataType = GetType(contentType);
        }

        private NetworkDataType GetType(string contentType)
        {
            NetworkDataType type = NetworkDataType.Unknown;
            if (string.IsNullOrEmpty(contentType))
                return type;

            contentType = contentType.ToLower();

            if (contentType.Contains("multipart/related") ||
                contentType.Contains("multipart/batching"))
            {
                type = NetworkDataType.Multipart;
            }
            else if (contentType.Contains("text/html") ||
                     contentType.Contains("application/x-javascript"))
            {
                type = NetworkDataType.Text;
            }
            else if (contentType.Contains("image/"))
            {
                type = NetworkDataType.Image;
            }
            else if (contentType.Contains("xml"))
            {
                type = NetworkDataType.XML;
            }
            else if (contentType.Contains("json"))
            {
                type = NetworkDataType.JSON;
            }

            return type;
        }
    }
}
