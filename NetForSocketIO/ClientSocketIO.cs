using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace NetForSocketIO
{
    public class ClientSocketIO
    {
        public string _Host = string.Empty;
        public int _Port = 443;
        public string _Origin = string.Empty;
        public string _Session = string.Empty;

        public ClientSocketIO(string host, int port)
        {
            _Host = host;
            _Port = port;
            _Origin = host;
        }

        public ClientSocketIO(string host, int port, string origin)
        {
            _Host = host;
            _Port = port;
            _Origin = origin;
        }

        public class ConnectErrorEventArgs : EventArgs
        {
            public string Message = string.Empty;
        }

        public EventHandler<ConnectErrorEventArgs> OnConnectError;

        public void Connect()
        {
            ThreadStart ts = new ThreadStart(connect);
            Thread thread = new Thread(ts);
            thread.Start();
        }

        private void connect()
        {
            if (!getSession())
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to connect to session")
                        });
                }
                return;
            }

            if (string.IsNullOrEmpty(_Session))
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to connect to session name is empty")
                        });
                }
                return;
            }

            Console.WriteLine("Session: {0}", _Session);

            helloWebSocket();

            readSocket();
        }

        private bool getSession()
        {
            string url = string.Format("https://{0}:{1}/socket.io/1/?__sails_io_sdk_version=0.10.0&__sails_io_sdk_platform=Net4SocketIO&__sails_io_sdk_language=javascript&t={2}", _Host, _Port, DateTime.Now.Ticks);
            Console.WriteLine("Requesting: {0}", url);

            HttpWebRequest req = null;
            try
            {
                req = (HttpWebRequest) HttpWebRequest.Create(url);
//[Request Header] Origin Value=https://sailsdemo-tgraupmann.c9.io
//[Request Header] Cache-Control Value=no-cache
//[Request Header] Connection Value=keep-alive
//[Request Header] Pragma Value=no-cache
//[Request Header] Accept Value=*/*
//[Request Header] Accept-Encoding Value=gzip, deflate, sdch
//[Request Header] Accept-Language Value=en-US,en;q=0.8
//[Request Header] Host Value=localhost:8443
//[Request Header] Referer Value=https://sailsdemo-tgraupmann.c9.io/
//[Request Header] User-Agent Value=Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to request connection exception={0}", e)
                        });
                }
                return false;
            }

            HttpWebResponse res = null;
            try
            {
                res = (HttpWebResponse) req.GetResponse();
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to get connection response exception={0}", e)
                        });
                }
                return false;
            }

            try
            {
                using (StreamReader sr = new StreamReader(res.GetResponseStream()))
                {
                    String content = sr.ReadLine();
                    Console.WriteLine("Connect Content={0}", content);

                    if (content.IndexOf(":") >= 0)
                    {
                        _Session = content.Substring(0, content.IndexOf(":"));
                    }
                    else
                    {
                        if (null != OnConnectError)
                        {
                            OnConnectError.Invoke(this,
                                new ConnectErrorEventArgs() {Message = "Connect response missing session"});
                        }
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to read connect response exception={0}", e)
                        });
                }
                return false;
            }
            finally
            {
                if (null != res)
                {
                    res.Close();
                }
            }
            return true;
        }

        private bool helloWebSocket()
        {
            string url = string.Format("https://{0}:{1}/socket.io/1/websocket/{2}?__sails_io_sdk_version=0.10.0&__sails_io_sdk_platform=Net4SocketIO&__sails_io_sdk_language=javascript", _Host, _Port, _Session);
            Console.WriteLine("Requesting: {0}", url);

            HttpWebRequest req = null;
            try
            {
                req = (HttpWebRequest)HttpWebRequest.Create(url);

//[Request Header] Origin Value=https://sailsdemo-tgraupmann.c9.io
//[Request Header] Sec-WebSocket-Version Value=13
//[Request Header] Sec-WebSocket-Key Value=xpV+2WIKlE+bMOacDx5ZzA==
//[Request Header] Sec-WebSocket-Extensions Value=permessage-deflate; client_max_window_bits
//[Request Header] Cache-Control Value=no-cache
//[Request Header] Connection Value=Upgrade
//[Request Header] Pragma Value=no-cache
//[Request Header] Upgrade Value=websocket
//[Request Header] Accept-Encoding Value=gzip, deflate, sdch
//[Request Header] Accept-Language Value=en-US,en;q=0.8
//[Request Header] Host Value=localhost:8443
//[Request Header] User-Agent Value=Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36

                req.Headers.Add("Origin", string.Format("https://{0}", _Origin));
                req.Headers.Add("Sec-WebSocket-Version", "13");
                req.Headers.Add("Sec-WebSocket-Key", "uraTQs4eAQ9QLHiUdNxBwQ==");
                req.Headers.Add("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
                req.Headers.Add("Cache-Control", "no-cache");
                req.Connection = "Upgrade";
                req.Headers.Add("Pragma", "no-cache");
                req.Headers.Add("Upgrade", "websocket");
                req.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36";
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to request connection exception={0}", e)
                        });
                }
                return false;
            }

            HttpWebResponse res = null;
            try
            {
                res = (HttpWebResponse)req.GetResponse();
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to get connection response exception={0}", e)
                        });
                }
                return false;
            }
            finally
            {
                if (null != res)
                {
                    res.Close();
                }
            }
            return true;
        }

        private void readSocket()
        {
            string url = string.Format("https://{0}:{1}/socket.io/1/xhr-polling/{2}?__sails_io_sdk_version=0.10.0&__sails_io_sdk_platform=Net4SocketIO&__sails_io_sdk_language=javascript&t={3}", _Host, _Port, _Session, DateTime.Now.Ticks);
            Console.WriteLine("Requesting: {0}", url);

            HttpWebRequest req = null;
            try
            {
                req = (HttpWebRequest)HttpWebRequest.Create(url);
//[Request Header] Origin Value=https://sailsdemo-tgraupmann.c9.io
//[Request Header] Cache-Control Value=no-cache
//[Request Header] Connection Value=keep-alive
//[Request Header] Pragma Value=no-cache
//[Request Header] Accept Value=*/*
//[Request Header] Accept-Encoding Value=gzip, deflate, sdch
//[Request Header] Accept-Language Value=en-US,en;q=0.8
//[Request Header] Host Value=localhost:8443
//[Request Header] Referer Value=https://sailsdemo-tgraupmann.c9.io/
//[Request Header] User-Agent Value=Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36
                req.Headers.Add("Origin", string.Format("https://{0}", _Origin));
                req.Headers.Add("Cache-Control", "no-cache");
                req.KeepAlive = true;
                req.Headers.Add("Pragma", "no-cache");
                req.Accept = "*/*";
                req.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                req.Referer = string.Format("https://{0}/", _Origin);
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36";
                req.Method = "GET";
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to request connection exception={0}", e)
                        });
                }
                return;
            }

            HttpWebResponse res = null;
            try
            {
                res = (HttpWebResponse)req.GetResponse();
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to get connection response exception={0}", e)
                        });
                }
                return;
            }

            try
            {
                Stream requestStream = res.GetResponseStream();
                //using (StreamReader sr = new StreamReader(requestStream))
                //{
                //    String content = sr.ReadLine();
                //    Console.WriteLine("Socket Content={0}", content);
                //}

                int bytesRead = 1;
                while (bytesRead > 0)
                {
                    byte[] buffer = new byte[128];
                    bytesRead = requestStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        Console.WriteLine(string.Format("****Request Content={0}", UTF8Encoding.UTF8.GetString(buffer, 0, bytesRead)));
                    }
                }
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to read connect response exception={0}", e)
                        });
                }
                return;
            }
            finally
            {
                if (null != res)
                {
                    res.Close();
                }
            }
            return;
        }

        public void SendSocket()
        {
            ThreadStart ts = new ThreadStart(sendSocket);
            Thread thread = new Thread(ts);
            thread.Start();
        }

        private void sendSocket()
        {
            string url = string.Format("https://{0}:{1}/socket.io/1/xhr-polling/{2}?__sails_io_sdk_version=0.10.0&__sails_io_sdk_platform=Net4SocketIO&__sails_io_sdk_language=javascript&t={3}", _Host, _Port, _Session, DateTime.Now.Ticks);
            Console.WriteLine("Requesting: {0}", url);

            HttpWebRequest req = null;
            try
            {
                req = (HttpWebRequest)HttpWebRequest.Create(url);
                //[Request Header] Origin Value=https://sailsdemo-tgraupmann.c9.io
                //[Request Header] Cache-Control Value=no-cache
                //[Request Header] Connection Value=keep-alive
                //[Request Header] Pragma Value=no-cache
                //[Request Header] Accept Value=*/*
                //[Request Header] Accept-Encoding Value=gzip, deflate, sdch
                //[Request Header] Accept-Language Value=en-US,en;q=0.8
                //[Request Header] Host Value=localhost:8443
                //[Request Header] Referer Value=https://sailsdemo-tgraupmann.c9.io/
                //[Request Header] User-Agent Value=Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36
                req.Headers.Add("Origin", string.Format("https://{0}", _Origin));
                req.Headers.Add("Cache-Control", "no-cache");
                req.KeepAlive = true;
                req.Headers.Add("Pragma", "no-cache");
                req.Accept = "*/*";
                req.Headers.Add("Accept-Encoding", "gzip, deflate, sdch");
                req.Headers.Add("Accept-Language", "en-US,en;q=0.8");
                req.Referer = string.Format("https://{0}/", _Origin);
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.0 Safari/537.36";
                req.Method = "POST";
            }
            catch (Exception e)
            {
                if (null != OnConnectError)
                {
                    OnConnectError.Invoke(this,
                        new ConnectErrorEventArgs()
                        {
                            Message = string.Format("Failed to request connection exception={0}", e)
                        });
                }
                return;
            }

            // ****Request Content=5:1+::{"name":"get","args":[{"method":"get","data":"{}","url":"/leaderboard","headers":{}}]}
            Stream requestStream = req.GetRequestStream();

            string getData = @"5:1+::{""name"":""get"",""args"":[{""method"":""get"",""data"":""{}"",""url"":""/leaderboard"",""headers"":{}}]}";
            byte[] buffer = UTF8Encoding.UTF8.GetBytes(getData);
            requestStream.Write(buffer, 0, buffer.Length);
            requestStream.Flush();
            requestStream.Close();
        }
    }
}
