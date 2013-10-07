using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FoxMud
{
    class ConnectionMonitor
    {
        List<Connection> activeConnections;
        Dictionary<Socket, Connection> sockToConnectionMap;

        public ConnectionMonitor()
        {
            activeConnections = new List<Connection>();
            sockToConnectionMap = new Dictionary<Socket, Connection>();
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            var connection = sender as Connection;

            if (connection == null)
                return;
            
            activeConnections.Remove(connection);
        }

        public void RegisterConnection(Connection connection)
        {
            connection.Closed += OnConnectionClosed;
            activeConnections.Add(connection);
            sockToConnectionMap.Add(connection.Socket, connection);            
        }

        public void Update()
        {
            var dictionary = activeConnections.ToDictionary(c => c.Socket);
            var read = new List<Socket>(activeConnections.Select(c => c.Socket));
            var write = new List<Socket>(activeConnections.Select(c => c.Socket));

            if (read.Count > 0 || write.Count > 0)
                Socket.Select(read, write, null, 0);

            foreach (var socket in write)
            {                
                dictionary[socket].Flush();
            }

            foreach (var socket in read)
            {                
                dictionary[socket].Fill();
            }

        }
    }
}
