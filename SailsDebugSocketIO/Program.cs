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
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace SailsDebugSocketIO
{
    class Program
    {
        /// <summary>
        /// The policy file
        /// </summary>
        private const string m_policy =
            "<?xml version=\"1.0\"?>\n<cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"80\"/></cross-domain-policy>";

        static Program()
        {
            try
            {
                m_httpListener.Prefixes.Add("http://*:8080/");
                m_httpListener.Prefixes.Add("https://*:8443/");
                m_httpListener.Start();
                Console.Error.WriteLine("Started listener");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to start listener: {0}", ex);
            }
        }

        /// <summary>
        /// Listen to http traffic
        /// </summary>
        static HttpListener m_httpListener = new HttpListener();

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
                HttpListenerContext context = null;
                try
                {
                    // accept http connections
                    context = m_httpListener.GetContext();

                    ParameterizedThreadStart ts = new ParameterizedThreadStart(Worker);
                    Thread thread = new Thread(ts);
                    thread.Start(context);
                }
                catch (System.Exception ex)
                {
                    Console.Error.WriteLine("SocketServer: WorkerSocketServer exception={0}", ex);
                }
                Thread.Sleep(0);
            }
        }

        // callback used to validate the certificate in an SSL conversation
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        static void Worker(Object obj)
        {
            HttpListenerContext context = obj as HttpListenerContext;

            try
            {
                string fileName = Path.GetFileName(context.Request.Url.AbsoluteUri);

                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine("Requested empty filename");
                }
                else
                {
                    Console.WriteLine("Requested: {0}", context.Request.Url.AbsoluteUri);
                }

                if (fileName.ToLower().Equals("crossdomain.xml"))
                {
                    using (StreamWriter sw = new StreamWriter(context.Response.OutputStream))
                    {
                        sw.Write(m_policy);
                        sw.Flush();
                    }
                    return;
                }

                string origin = context.Request.Headers["Origin"];
                if (string.IsNullOrEmpty(origin))
                {
                    return;
                }

                context.Response.AddHeader("Access-Control-Allow-Origin", origin);

                string targetUrl = string.Format("{0}{1}", origin, context.Request.Url.PathAndQuery);
                Console.WriteLine("Proxy: {0}", targetUrl);

                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
                foreach (string key in context.Request.Headers.AllKeys)
                {
                    try
                    {
                        Console.WriteLine("Header={0} Value={1}", key, context.Request.Headers[key]);

                        switch (key)
                        {
                            case "Connection":
                                switch (context.Request.Headers[key])
                                {
                                    case "keep-alive":
                                        req.KeepAlive = true;
                                        break;
                                    default:
                                        req.Connection = context.Request.Headers[key];
                                        break;
                                }
                                break;
                            case "Accept":
                                for (int i = 0; i < context.Request.AcceptTypes.Length; ++i)
                                {
                                    if (i == 0)
                                    {
                                        req.Accept = context.Request.AcceptTypes[0];
                                    }
                                    else
                                    {
                                        req.Accept += "," + context.Request.AcceptTypes[i];
                                    }
                                }
                                break;
                            case "Content-Length":
                                req.ContentLength = context.Request.ContentLength64;
                                break;
                            case "Content-Type":
                                req.ContentType = context.Request.ContentType;
                                break;
                            case "Host":
                                req.Host = new Uri(targetUrl).Host;
                                break;
                            case "Referer":
                                req.Referer = context.Request.UrlReferrer.AbsoluteUri;
                                break;
                            case "User-Agent":
                                req.UserAgent = context.Request.UserAgent;
                                break;
                            default:
                                req.Headers[key] = context.Request.Headers[key];
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Set property={0} failed={1}", key, ex);
                    }
                }
                req.ContentType = context.Request.ContentType;
                req.Method = context.Request.HttpMethod;

                if (req.ContentLength > 0)
                {
                    Stream streamReq = req.GetRequestStream();
                    int bytesRead = 1;
                    while (bytesRead > 0)
                    {
                        byte[] buffer = new byte[128];
                        bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            Console.WriteLine(string.Format("Byte Content={0}", UTF8Encoding.UTF8.GetString(buffer, 0, bytesRead)));
                            streamReq.Write(buffer, 0, bytesRead);
                            streamReq.Flush();
                        }
                    }
                }

                try
                {
                    HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                    foreach (string key in res.Headers.AllKeys)
                    {
                        Console.WriteLine("Response header={0} value={1}", key, res.Headers[key]);
                        try
                        {
                            switch (key)
                            {
                                case "Content-Length":
                                case "Content-Type":
                                    break;
                                default:
                                    context.Response.AddHeader(key, res.Headers[key]);
                                    break;
                            }
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine("Failed to set={0}", key);
                        }
                    }

                    context.Response.ContentType = res.ContentType;
                    if (res.ContentLength >= 0)
                    {
                        context.Response.ContentLength64 = res.ContentLength;
                    }
                    context.Response.StatusCode = (int)res.StatusCode;
                    context.Response.StatusDescription = res.StatusDescription;

                    Stream targetStream = res.GetResponseStream();

                    if (res.ContentLength > 0)
                    {
                        byte[] buffer = new byte[res.ContentLength];
                        targetStream.Read(buffer, 0, buffer.Length);
                        Console.WriteLine(string.Format("Byte Content={0}", UTF8Encoding.UTF8.GetString(buffer)));
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                        return;
                    }
                    else
                    {
                        if (req.KeepAlive)
                        {
                            byte[] buffer = new byte[128];
                            int bytesRead = 1;
                            while (bytesRead > 0)
                            {
                                bytesRead = targetStream.Read(buffer, 0, (int) buffer.Length);
                                if (bytesRead > 0)
                                {
                                    Console.WriteLine(string.Format("Byte Content={0}", UTF8Encoding.UTF8.GetString(buffer, 0, bytesRead)));
                                    context.Response.OutputStream.Write(buffer, 0, bytesRead);
                                    context.Response.OutputStream.Flush();
                                }
                                Thread.Sleep(0);
                            }
                        }
                    }

                    targetStream.Close();
                    res.Close();
                    return;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Console.Error.WriteLine("SocketServer: WorkerSocketServer exception={0}", ex);
            }
            finally
            {
                try
                {
                    // close the connection
                    if (null != context &&
                        null != context.Response)
                    {
                        context.Response.Close();
                    }
                }
                catch (Exception)
                {
                    //sometimes the stream might already be closed
                }
            }
        }
    }
}