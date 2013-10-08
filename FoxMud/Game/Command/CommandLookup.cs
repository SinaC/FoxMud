using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    interface CommandLookup
    {
        PlayerCommand FindCommand(string commandName);
        PlayerCommand FindCommand(string commandName, bool includeAdmin);
    }
}