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
    interface ConnectionHandler
    {
        void Handle(Socket socket);
    }
}
