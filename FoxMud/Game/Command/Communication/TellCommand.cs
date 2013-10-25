using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Communication
{
    [Command("tell", false)]
    class TellCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: tell <player> <message>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var recipient = context.Arguments[0];
            var recipientPlayer = Server.Current.Database.Get<Player>(recipient);
            if (recipientPlayer == null)
            {
                session.WriteLine("Who is {0}?", recipient);
                return;
            }

            context.Arguments.Remove(recipient);
            var message = context.ArgumentString;
            session.WriteLine("You tell {0}, \"{1}\"", recipient, message);
            recipientPlayer.Send(string.Format("{0} tells you, \"{1}\"", session.Player.Forename, message), session.Player);
        }
    }
}
