using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FoxMud.Game.Command
{
    class CommandInfo
    {
        public bool IsAdmin { get; set; }
        public PlayerCommand Command { get; set; }
        public TickDelay TickLength { get; set; }
    }

    class DynamicCommandLookup : CommandLookup
    {
        SortedDictionary<string, CommandInfo> commandTable;

        public DynamicCommandLookup()
        {
            ConstructCommandTable();
        }

        private void ConstructCommandTable()
        {
            commandTable = new SortedDictionary<string, CommandInfo>();

            var commandTypes = Assembly.GetExecutingAssembly().GetTypes().Where(f => f.IsClass)
                                                                         .Where(f => !f.IsAbstract)
                                                                         .Where(f => typeof(PlayerCommand).IsAssignableFrom(f));

            foreach (Type commandType in commandTypes)
            {
                var attributes = commandType.GetCustomAttributes(typeof(CommandAttribute), false)
                                            .Cast<CommandAttribute>();

                foreach (var attribute in attributes)
                {
                    commandTable.Add(attribute.CommandName,
                                     new CommandInfo()
                                     {
                                        Command = (PlayerCommand)Activator.CreateInstance(commandType, attribute.Parameters),
                                        IsAdmin = attribute.IsAdmin,
                                        TickLength = attribute.TickLength
                                     });
                }
            }           
        }

        public CommandInfo FindCommand(string commandName)
        {
            return FindCommand(commandName, false);
        }

        public CommandInfo FindCommand(string commandName, bool includeAdmin)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return null;

            commandName = commandName.ToLower();

            foreach (var item in commandTable)
            {
                if (item.Key.StartsWith(commandName))
                {
                    if (item.Value.IsAdmin && !includeAdmin)
                        continue;

                    return item.Value;
                }
            }

            return null;
        }
    }
}
