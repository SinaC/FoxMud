using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FoxMud
{
    class LineReceivedEventArgs : EventArgs
    {
        public LineReceivedEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; private set; }
    }
}
