using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    abstract class PlayerCommand
    {
        public TickDelay TickLength { get; private set; }

        public PlayerCommand(TickDelay tickLength)
        {
            TickLength = TickLength;
        }

        public PlayerCommand()
            : this(TickDelay.Single)
        {

        }

        public abstract void PrintSyntax(Session session);
        public abstract void Execute(Session session, CommandContext context);
    }
}
