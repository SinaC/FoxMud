using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Stat
{
    [Command("level", false)]
    class LevelCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: level");
        }

        public override void Execute(Session session, CommandContext context)
        {
            session.WriteLine("You have {0} of {1} experience for level {2}", session.Player.Experience,
                              ExperienceResolver.ExperienceRequired(session.Player.Level + 1), session.Player.Level + 1);
        }
    }
}
