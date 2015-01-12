//#define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using DebugHttpServer;

namespace SailsDebugSocketIO
{
    class Program
    {
        /// <summary>
        /// Listen to socket traffic
        /// </summary>
        static TcpListener _listener = null;

        static Program()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, 8443);
                _listener.Start();
                Console.Error.WriteLine("Started listener");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to start listener: {0}", ex);
            }
        }

        static void Main(string[] args)
        {
            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            // trust sender
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                            = ((sender, cert, chain, errors) => cert.Subject.Contains("YourServerName"));

            // validate cert by calling a function
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            // keep listening and send a response
            while (true)
            {
                Thread.Sleep(1);
                TcpClient client = null;
                try
                {
                    // accept http connections
                    client = _listener.AcceptTcpClient();

                    Proxy proxy = new Proxy(client, "sailsdemo-tgraupmann.c9.io", 443);

                    ParameterizedThreadStart ts = new ParameterizedThreadStart(Worker);
                    Thread thread = new Thread(ts);
                    thread.Start(proxy);
                }
                catch (System.Exception ex)
                {
                    Console.Error.WriteLine("SocketServer: WorkerSocketServer exception={0}", ex);
                }
            }
        }

        // callback used to validate the certificate in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        static void Worker(Object obj)
        {
            if (null == obj ||
                !(obj is Proxy))
            {
                return;
            }

            Proxy proxy = obj as Proxy;
            proxy.Connect();
        }
    }
}