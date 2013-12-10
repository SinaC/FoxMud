using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.State;

namespace FoxMud.Game.Command.Admin
{
    [Command("mapping", true, TickDelay.Instant)]
    class MappingCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: mapping");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You need to be standing to start mapping.");
                return;
            }

            session.PushState(new MappingState());
        }
    }
}
