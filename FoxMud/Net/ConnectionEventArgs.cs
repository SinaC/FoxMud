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
    class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(Socket socket)
        {
            Socket = socket;
        }

        public Socket Socket { get; private set; }
    }
}
