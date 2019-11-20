using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace NetworkMonitor
{
    public class NetworkUtils
    {
        public static string GetLocalhostFQDN()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            return string.Format("{0}.{1}", properties.HostName, properties.DomainName);
        }

        public static string GetHostIpAddress()
        {
            IPAddress[] ipAddrsForHostName = Dns.GetHostAddresses(Dns.GetHostName());
            List<IPAddress> ipAddresses = new List<IPAddress>();

            foreach (IPAddress ipAddress in ipAddrsForHostName)
            {
                //only consider IPV4
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ipAddresses.Add(ipAddress);
            }

            if (ipAddresses.Count <= 0)
                return string.Empty;

            return ipAddresses[0].ToString();
        }

        public static void CleanUpBrowserCache()
        {
            try
            {
                string internetCachePath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
                string[] fileNames = null;

                fileNames = System.IO.Directory.GetFiles(internetCachePath, "*.js", System.IO.SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception) { }
                }

                fileNames = System.IO.Directory.GetFiles(internetCachePath, "*.css", System.IO.SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception) { }
                }

                fileNames = System.IO.Directory.GetFiles(internetCachePath, "*.png", System.IO.SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception) { }
                }

                fileNames = System.IO.Directory.GetFiles(internetCachePath, "*.html", System.IO.SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception) { }
                }

                fileNames = System.IO.Directory.GetFiles(internetCachePath, "*.htm", System.IO.SearchOption.AllDirectories);
                foreach (string fileName in fileNames)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }
}
