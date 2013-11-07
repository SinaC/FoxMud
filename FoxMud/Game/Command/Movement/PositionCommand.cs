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
            if (session.Player.Status != GameStatus.Sitting && session.Player.Status != GameStatus.Sleeping)
            {
                session.WriteLine("You're not currently sitting or sleeping.");
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
            if (session.Player.Status != GameStatus.Standing && session.Player.Status != GameStatus.Sleeping)
            {
                session.WriteLine("You're not standing or sleeping.");
                return;
            }

            if (session.Player.Status == GameStatus.Standing)
                session.WriteLine("You sit down.");
            else // sleeping
                session.WriteLine("You wake and sit up.");

            session.Player.Status = GameStatus.Sitting;
        }
    }

    [Command("sleep", false, TickDelay.Instant)]
    class SleepCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: sleep");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Status != GameStatus.Standing && session.Player.Status != GameStatus.Sitting)
            {
                session.WriteLine("You're not standing or sitting.");
                return;
            }

            if (session.Player.Status == GameStatus.Standing)
                session.WriteLine("You lay down to sleep.");
            else // sitting
                session.WriteLine("You fall over asleep..");

            session.Player.Status = GameStatus.Sleeping;
        }
    }
}
