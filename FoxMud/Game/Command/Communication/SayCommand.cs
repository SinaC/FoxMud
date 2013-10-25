using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Communication
{
    [Command("say", false)]
    class SayCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: say <message>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                session.WriteLine("Say what?");
                return;
            }

            string textToSay = context.ArgumentString;
            var room = Server.Current.Database.Get<Room>(session.Player.Location);

            if (room != null)
            {
                room.SendPlayers(string.Format("%d says, \"{0}\"\n", textToSay),
                                 session.Player,
                                 null,
                                 session.Player);

                session.WriteLine(string.Format("You say, \"{0}\"\n", textToSay));
            }
        }
    }
}
