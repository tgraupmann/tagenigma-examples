using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.GZip;

namespace DebugHttpServer
{
    public class Proxy
    {
        static X509Certificate _serverCertificate = null;

        static Proxy()
        {
            _serverCertificate = X509Certificate.CreateFromCertFile("vMargeSignedByCA.cer");
        }

        public string _Host = string.Empty;
        public int _Port = 443;

        private TcpClient _clientTcpClient = null;
        private SslStream _clientSslStream = null;

        private TcpClient _serverTcpClient = null;
        private SslStream _serverSslStream = null;

        public Proxy(TcpClient client, string host, int port)
        {
            _clientTcpClient = client;
            _Host = host;
            _Port = port;
        }

        public void Connect()
        {
            try
            {
                _clientSslStream = new SslStream(_clientTcpClient.GetStream(), false);

                _clientSslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls, false);

                openServer();

                ThreadStart ts = new ThreadStart(readClient);
                Thread clientThread = new Thread(ts);
                clientThread.Start();

                ts = new ThreadStart(readServer);
                Thread serverThread = new Thread(ts);
                serverThread.Start();

                while (null != _clientTcpClient &&
                       _clientTcpClient.Connected &&
                       null != _serverTcpClient &&
                       _serverTcpClient.Connected &&
                       null != _clientSslStream &&
                       null != _serverSslStream)
                {
                    Thread.Sleep(1);
                }
            }
            catch (System.Exception ex)
            {
                Console.Error.WriteLine("SocketServer: WorkerSocketServer exception={0}", ex);
            }
            finally
            {
                closeConnections();
            }
        }

        private void readClient()
        {
            bool skipStreaming = false;
            List<byte> skipBuffer = new List<byte>();

            List<byte> buffer = new List<byte>();
            while (null != _clientTcpClient &&
                   _clientTcpClient.Connected &&
                   null != _clientSslStream &&
                   null != _serverTcpClient &&
                   _serverTcpClient.Connected &&
                   null != _serverSslStream)
            {
                Thread.Sleep(1);
                try
                {
                    int one = _clientSslStream.ReadByte();
                    if (one >= 0)
                    {
                        if (!skipStreaming)
                        {
                            buffer.Add((byte) one);
                            _serverSslStream.WriteByte((byte) one);
                            _serverSslStream.Flush();

                            string content = Encoding.UTF8.GetString(buffer.ToArray());
                            if (content.ToUpper().EndsWith("HOST: "))
                            {
                                byte[] inject = Encoding.UTF8.GetBytes(string.Format("{0}\r\n", _Host));
                                buffer.AddRange(inject);
                                _serverSslStream.Write(inject.ToArray());
                                _serverSslStream.Flush();
                                skipStreaming = true;
                                skipBuffer.Clear();
                                //Console.WriteLine("Injected host");
                            }
                            /*
                            else if (content.ToUpper().EndsWith("ACCEPT-ENCODING: "))
                            {
                                byte[] inject = Encoding.UTF8.GetBytes(string.Format("utf8\r\n"));
                                buffer.AddRange(inject);
                                _serverSslStream.Write(inject.ToArray());
                                _serverSslStream.Flush();
                                skipStreaming = true;
                                skipBuffer.Clear();
                                //Console.WriteLine("Injected encoding");
                            }
                            */

                            if (content.ToUpper().EndsWith("\r\n\r\n"))
                            {
                                Console.WriteLine("***readClient is being keptAlive***");
                                Console.WriteLine(content);
                                buffer.Clear();
                            }
                            else
                            {
                                //Console.WriteLine("Content Size: {0}", buffer.Count);
                                if (File.Exists("test.txt"))
                                {
                                    using (
                                        FileStream fs = File.Open("test.txt", FileMode.Truncate, FileAccess.Write,
                                            FileShare.ReadWrite))
                                    {
                                        
                                    }
                                }
                                using (
                                    FileStream fs = File.Open("test.txt", FileMode.OpenOrCreate, FileAccess.Write,
                                        FileShare.ReadWrite))
                                {
                                    byte[] contents = Encoding.UTF8.GetBytes("reading client...\r\n");
                                    fs.Write(contents, 0, contents.Length);
                                    fs.Flush();
                                    contents = null;
                                    try
                                    {
                                        using (MemoryStream ms = new MemoryStream(buffer.ToArray()))
                                        {
                                            using (GZipStream gzipStream = new GZipStream(ms, CompressionMode.Decompress))
                                            {
                                                gzipStream.CopyTo(fs);
                                                gzipStream.Flush();
                                                fs.Flush();
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        contents = buffer.ToArray();
                                        fs.Write(contents, 0, contents.Length);
                                        fs.Flush();
                                    }
                                }
                            }
                        }
                        else
                        {
                            skipBuffer.Add((byte)one);
                            //Console.WriteLine("Skip Buffer={0}", Encoding.UTF8.GetString(skipBuffer.ToArray()));
                            if (Encoding.UTF8.GetString(skipBuffer.ToArray()).EndsWith("\r\n"))
                            {
                                //Console.WriteLine("Found end of line");
                                skipStreaming = false;

                                //Console.WriteLine("***Client read so far***");
                                //Console.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
                            }
                        }
                        
                    }
                    else if (buffer.Count > 0)
                    {
                        Console.WriteLine("***Client read***");
                        Console.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
                        buffer.Clear();
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to read from client exception={0}", e);
                    closeConnections();
                    return;
                }
            }

            Console.WriteLine("readClient complete");
        }

        private void readServer()
        {
            List<byte> buffer = new List<byte>();
            while (null != _serverTcpClient &&
                   _serverTcpClient.Connected &&
                   null != _serverSslStream)
            {
                Thread.Sleep(1);
                try
                {
                    int one = _serverSslStream.ReadByte();
                    if (one >= 0)
                    {
                        buffer.Add((byte)one);
                        _clientSslStream.WriteByte((byte)one);
                        _clientSslStream.Flush();

                        string content = Encoding.UTF8.GetString(buffer.ToArray());
                        if (content.ToUpper().EndsWith("\r\n\r\n"))
                        {
                            //Console.WriteLine("Time to un gzip");
                        }
                    }
                    else if (buffer.Count > 0)
                    {
                        string content = Encoding.UTF8.GetString(buffer.ToArray());
                        Console.WriteLine("***Server read***");
                        Console.WriteLine(content);
                        buffer.Clear();
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to read from server exception={0}", e);
                    closeConnections();
                    return;
                }
            }
            Console.WriteLine("readServer complete");
        }

        private static bool validateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private object _lockObject = new object();

        private void closeConnections()
        {
            lock (_lockObject)
            {
                if (null != _serverSslStream)
                {
                    Console.WriteLine("Closed server ssl stream");
                    _serverSslStream.Close();
                    _serverSslStream = null;
                }
                if (null != _serverTcpClient)
                {
                    Console.WriteLine("Closed server tcp client");
                    _serverTcpClient.Close();
                    _serverTcpClient = null;
                }
                if (null != _clientSslStream)
                {
                    Console.WriteLine("Closed client ssl stream");
                    _clientSslStream.Close();
                    _clientSslStream = null;
                }
                if (null != _clientTcpClient)
                {
                    Console.WriteLine("Closed client tcp client");
                    _clientTcpClient.Close();
                    _clientTcpClient = null;
                }
            }
        }

        private void openServer()
        {
            try
            {
                _serverTcpClient = new TcpClient(_Host, _Port);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to connect to server exception={0}", e.Message);
                closeConnections();
                return;
            }

            try
            {
                _serverSslStream = new SslStream(
                    _serverTcpClient.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(validateServerCertificate),
                    null
                    );
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to create SSL stream exception={0}", e.Message);
                if (null != _serverTcpClient)
                {
                    _serverTcpClient.Close();
                }
                closeConnections();
                return;
            }

            try
            {
                _serverSslStream.AuthenticateAsClient(_Host);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to create SSL stream exception={0}", e.Message);
                closeConnections();
                return;
            }
        }
    }
}
