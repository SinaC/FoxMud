using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Movement
{
    [Command("stand", false, TickDelay.Instant)]
    class StandCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: stand");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Status != GameStatus.Sitting)
            {
                session.WriteLine("You're not currently sitting.");
                return;
            }

            session.Player.Status = GameStatus.Standing;
            session.WriteLine("You stand up.");
        }
    }

    [Command("sit", false, TickDelay.Instant)]
    class SitCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: sit");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You're not currently standing.");
                return;
            }

            session.Player.Status = GameStatus.Sitting;
            session.WriteLine("You sit down.");
        }
    }

    // todo: sleep command
}
