using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    interface PlayerCommand
    {
        void PrintSyntax(Session session);
        void Execute(Session session, CommandContext context);
    }

    interface CallbackCommand : PlayerCommand
    {
        void Execute(Session session, CommandContext context, out string callbackCommand);
    }
}
