using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.State;

namespace FoxMud.Game
{
    class StartupConnectionHandler : ConnectionHandler
    {
        private readonly ConnectionMonitor connectionMonitor;
        private readonly SessionMonitor sessionMonitor;

        public StartupConnectionHandler(ConnectionMonitor connectionMonitor,
                                        SessionMonitor sessionMonitor)
        {
            this.sessionMonitor = sessionMonitor;
            this.connectionMonitor = connectionMonitor;
        }

        public void Handle(Socket socket)
        {
            Connection connection = new Connection(socket);
            connectionMonitor.RegisterConnection(connection);
            Session session = new Session(connection);
            sessionMonitor.RegisterSession(session);
            session.PushState(new SplashState());
        }
    }
}
