using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    interface CommandLookup
    {
        CommandInfo FindCommand(string commandName);
        CommandInfo FindCommand(string commandName, Player player);
        IEnumerable<CommandInfo> FindCommands(int level);
    }
}