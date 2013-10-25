using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command
{
    [Command("exit", false)]
    [Command("quit", false)]
    class ExitCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: exit");
            session.WriteLine("Syntax: quit");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var player = Server.Current.Database.Get<Player>(session.Player.Key);
            if (player != null)
            {
                Server.Current.Database.Save<Player>(player);
                session.WriteLine("Saved...");
            }

            var room = Server.Current.Database.Get<Room>(session.Player.Location);
            if (room != null)
            {
                room.SendPlayers("%d disappears in a puff of smoke\n", session.Player, null, session.Player);
            }

            session.WriteLine("Disconnecting...");
            session.End(); // this will pop state
        }
    }
}
