using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    interface CommandLookup
    {
        CommandInfo FindCommand(string commandName, bool isNpc = false);
        CommandInfo FindCommand(string commandName, Player player, bool isNpc = false);
        IEnumerable<CommandInfo> FindCommands(int level);
    }
}