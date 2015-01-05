//#define DEBUG_ON

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
                }
                else if (context.Request.Url.LocalPath.Equals("/socket.io/1/"))
                {
                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        foreach (string key in context.Request.Headers.AllKeys)
                        {
                            Console.WriteLine("Header={0} Value={1}", key, context.Request.Headers[key]);
                        }

                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine("Content: {0}", content);
                        }

                        string origin = context.Request.Headers["Origin"];

                        context.Response.ContentType = "text/plain";
                        context.Response.AddHeader("transfer-encoding", "chunked");
                        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");

                        string response =
                            "6I9ihRlAyaYSqJYx6Xpw:60:60:websocket,htmlfile,xhr-polling,jsonp-polling";
                        byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(response);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                    }
                }
                else if (context.Request.Url.LocalPath.Equals("/socket.io/1/jsonp-polling/6I9ihRlAyaYSqJYx6Xpw"))
                {
                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        foreach (string key in context.Request.Headers.AllKeys)
                        {
                            Console.WriteLine("Header={0} Value={1}", key, context.Request.Headers[key]);
                        }
                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine("Content: {0}", content);
                        }

                        string origin = context.Request.Headers["Origin"];

                        context.Response.ContentType = "text/javascript";
                        context.Response.AddHeader("transfer-encoding", "chunked");
                        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                        context.Response.AddHeader("x-xss-protection", "0");
                        context.Response.StatusCode = 200;

                        string response = @"io.j[0](""7:::1+0"");";
                        byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(response);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                    }
                }
                else if (context.Request.Url.LocalPath.Equals("/socket.io/1/xhr-polling/6I9ihRlAyaYSqJYx6Xpw"))
                {
                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        foreach (string key in context.Request.Headers.AllKeys)
                        {
                            Console.WriteLine("Header={0} Value={1}", key, context.Request.Headers[key]);
                        }
                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine("Content: {0}", content);
                        }

                        string origin = context.Request.Headers["Origin"];

                        context.Response.ContentType = "text/plain";
                        context.Response.AddHeader("transfer-encoding", "chunked");
                        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                        context.Response.AddHeader("x-xss-protection", "0");
                        context.Response.StatusCode = 200;

                        string response = @"";
                        byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(response);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                    }
                }
                else if (context.Request.Url.LocalPath.Equals("/socket.io/1/websocket/6I9ihRlAyaYSqJYx6Xpw"))
                {
                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        foreach (string key in context.Request.Headers.AllKeys)
                        {
                            Console.WriteLine("Header: {0} Value: {1}", key, context.Request.Headers[key]);
                        }

                        int chunkSize = 256;
                        byte[] buffer = new byte[chunkSize];
                        DateTime timeout = DateTime.Now + TimeSpan.FromSeconds(2);
                        while (DateTime.Now < timeout)
                        {
                            int peek = sr.Peek();
                            if (peek != -1)
                            {
                                break;
                            }
                            if (!sr.EndOfStream)
                            {
                                context.Request.InputStream.Read(buffer, 0, 256);
                                if (buffer[0] != 0)
                                {
                                    Console.WriteLine("Something!");
                                }
                            }
                            Thread.Sleep(0);
                        }
                    }
                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine("Content: {0}", content);
                        }

                        string origin = context.Request.Headers["Origin"];

                        context.Response.ContentType = "text/plain";
                        context.Response.AddHeader("transfer-encoding", "chunked");
                        context.Response.AddHeader("Access-Control-Allow-Origin", origin);
                        context.Response.AddHeader("Access-Control-Allow-Credentials", "true");
                        context.Response.AddHeader("x-xss-protection", "0");
                        context.Response.StatusCode = 200;

                        string response = @"";
                        byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(response);
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Flush();
                    }
                }
                else
                {
                    Console.WriteLine("Request: {0}", context.Request.Url.LocalPath);

                    using (StreamReader sr = new StreamReader(context.Request.InputStream))
                    {
                        string content = sr.ReadToEnd();
                        if (!string.IsNullOrEmpty(content))
                        {
                            Console.WriteLine("Content: {0}", content);
                        }
                    }
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