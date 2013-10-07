using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    interface PlayerCommand
    {
        void Execute(Session session, CommandContext context);
    }
}
