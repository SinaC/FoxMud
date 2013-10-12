using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Game.Item;

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
            Mapper.CreateMap<Template, PlayerItem>();

            using (var server = new Server())
            {
                server.Start();
            }
        }
    }
}
