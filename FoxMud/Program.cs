using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxMud
{
    /// <summary>
    /// simple mud. prototype a basic tcp server that accepts multiple connections
    /// and echoes any input received from the connection's prompt
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            using (Server server = new Server())
            {
                server.Start();
            }
            
        }
    }
}
