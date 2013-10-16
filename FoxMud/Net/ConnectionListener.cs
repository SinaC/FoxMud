using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace FoxMud
{
    class ConnectionListener : IDisposable
    {
        private readonly int port;

        private Socket listenSocket;
        private bool isRunning;
        private bool isDisposed;
        
        public ConnectionListener(int port)
        {
            listenSocket = null;
            isRunning = false;
            isDisposed = false;
            this.port = port;
        }

        public ConnectionHandler ConnectionHandler
        {
            get;
            set;
        }

        private void CheckDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException("Connection Listener is disposed");
        }

        private void OnNewConnection(Socket socket)
        {
            if (ConnectionHandler != null)
                ConnectionHandler.Handle(socket);
            else
                socket.Close();
        }

        private void OnConnectionAccepted(IAsyncResult ar)
        {
            try
            {
                try
                {
                    var socket = listenSocket.EndAccept(ar);

                    OnNewConnection(socket);
                }
                finally
                {
                    listenSocket.BeginAccept(OnConnectionAccepted, null);
                }
            }
            catch
            {
                Server.Current.Log(LogType.Warning, "OnConnectionAccepted error (probably on shutdown");
            }
        }

        public void Start()
        {
            if (isRunning)
                throw new InvalidOperationException("Connection Listener is already running");

            CheckDisposed();

            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            listenSocket.Listen(10);
            listenSocket.BeginAccept(OnConnectionAccepted, null);
        }

        public void Stop()
        {
            listenSocket.Close();
            isRunning = false;
        }

        public void Dispose()
        {
            Stop();
            listenSocket.Dispose();
            isDisposed = true;
        }
    }
}
