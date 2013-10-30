using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Stat
{
    [Command("whois", false, TickDelay.Instant)]
    class WhoisCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("whois <player>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                session.WriteLine("Who is who?");
                return;
            }

            var target = Server.Current.Database.Get<Player>(context.ArgumentString.ToLower());
            if (target == null)
            {
                session.WriteLine("You couldn't find them.");
                return;
            }

            session.WriteLine(target.Whois());
        }
    }
}
