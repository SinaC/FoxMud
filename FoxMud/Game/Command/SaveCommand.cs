using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command
{
    [Command("save", false)]
    class SaveCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: save");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var player = Server.Current.Database.Get<Player>(session.Player.Key);
            if (player != null)
            {
                Server.Current.Database.Save<Player>(player);
                session.WriteLine("Saved...");
            }
        }
    }
}
