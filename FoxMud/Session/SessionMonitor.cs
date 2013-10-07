using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxMud.Game;

namespace FoxMud
{
    class SessionMonitor
    {
        private List<Session> activeSessions;

        public SessionMonitor()
        {
            activeSessions = new List<Session>();
        }

        private void OnSessionEnded(object sender, EventArgs e)
        {
            if (!(sender is Session))
                return;

            activeSessions.Remove((Session)sender);
        }

        public void RegisterSession(Session session)
        {
            if (activeSessions.Contains(session))
                throw new ArgumentException("Session already registered", "session");

            session.SessionEnded += OnSessionEnded;
            activeSessions.Add(session);
        }

        public IEnumerable<Session> EnumerateSessions()
        {
            return activeSessions;
        }

        public Session GetPlayerSession(Player player)
        {
            foreach (var session in activeSessions)
            {
                if (session.Player == player)
                    return session;
            }

            return null;
        }
    }
}
