using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Communication
{
    [Command("chat", false)]
    class ChatCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: chat <message>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            foreach (var playerSession in Server.Current.SessionMonitor.EnumerateSessions())
            {
                playerSession.WriteLine("{0} chats, \"{1}\"\n", session.Player.Forename, context.ArgumentString);
            }
        }
    }
}
