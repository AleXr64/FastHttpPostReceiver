using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AleXr64.FastHttpPostReceiver
{
    public delegate void HttpPostReceiver(HttpPostData data);

    public class HttpPostListener
    {
        private const string __response = "HTTP/1.0 200 OK\r\n\r\n";
        private static readonly byte[] __responseBytes = Encoding.ASCII.GetBytes(__response);
        private readonly Thread _listenerThread;
        private readonly Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly IPEndPoint endPoint;
        private readonly object locker = new object();
        private bool __exitFlag;

        public HttpPostListener(ushort port)
        {
            _listenerThread = new Thread(ListenerLoop) { Name = "HttpPostListener" };
            endPoint = new IPEndPoint(IPAddress.Loopback, port);
        }

        public HttpPostListener(string host, ushort port)
        {
            _listenerThread = new Thread(ListenerLoop) { Name = "HttpPostListener" };
            var addresses = Dns.GetHostAddresses(host);
            endPoint = addresses.Length > 1 ? new IPEndPoint(IPAddress.Any, port) : new IPEndPoint(addresses[0], port);
        }

        public event HttpPostReceiver OnDataReceived;

        private void ListenerLoop()
        {
            while(IsNotExit())
            {
                var t = Task.Run(() => _serverSocket.Accept());
                while(!t.IsCompleted)
                {
                    t.Wait(10);
                    if(!IsNotExit())
                    {
                        break;
                    }
                }

                if(t.IsCompleted)
                {
                    var s = t.Result;
                    Task.Run(() => ProcessClientSocket(s));
                }
                else
                {
                    break;
                }
            }
        }

        private void ProcessClientSocket(Socket socket)
        {
            while(socket.Available == 0)
            {
                Thread.Yield();
            }

            var count = socket.Available;
            var buffer = new byte[count];
            socket.Receive(buffer);
            socket.Send(__responseBytes);
            socket.Close();

            using(var stream = new StreamReader(new MemoryStream(buffer)))
            {
                var exit = false;
                var headers = new List<HttpHeader>();
                var query = string.Empty;
                while(!stream.EndOfStream && !exit)
                {
                    var line = stream.ReadLine();
                    if(string.IsNullOrEmpty(line))
                    {
                        exit = true;
                    }
                    else
                    {
                        if(line.Contains(':'))
                        {
                            var parts = line.Split(':');
                            headers.Add(new HttpHeader(parts[0], parts[1]));
                        }
                        else
                        {
                            query = line;
                            if(!query.StartsWith("POST"))
                            {
                                return;
                            }
                        }
                    }
                }

                byte[] messageBytes = null;

                var contentLength = headers.FirstOrDefault(x => x.Name == "Content-Length");
                if(!stream.EndOfStream && contentLength.Name != null)
                {
                    if(int.TryParse(contentLength.Value, out var contentBytesCount))
                    {
                        messageBytes = new byte[contentBytesCount];
                        Array.Copy(buffer, buffer.Length - contentBytesCount, messageBytes, 0, contentBytesCount);
                    }
                }

                var result = new HttpPostData(headers.ToArray(), messageBytes ?? new byte[0], query);
                OnDataReceived?.Invoke(result);
            }
        }

        private bool IsNotExit()
        {
            bool exit;
            lock(locker)
            {
                exit = __exitFlag;
            }

            return !exit;
        }

        public void Start()
        {
            _serverSocket.Bind(endPoint);
            _serverSocket.Listen(100);
            _listenerThread.Start();
        }

        public void Stop()
        {
            lock(locker)
            {
                __exitFlag = true;
            }

            _listenerThread.Join();
            _serverSocket.Close();
            _serverSocket.Dispose();
        }
    }
}
