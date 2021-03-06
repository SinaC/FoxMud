﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FoxMud.Game.Command
{
    class CommandInfo
    {
        public bool IsAdmin { get; set; }
        public int MinimumLevel { get; set; }
        public PlayerCommand Command { get; set; }
        public TickDelay TickLength { get; set; }
        public string CommandName { get; set; }
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

            Server.Current.Log("Loading commands...");

            foreach (var commandType in commandTypes)
            {
                var attributes = commandType.GetCustomAttributes(typeof(CommandAttribute), false)
                                            .Cast<CommandAttribute>();

                foreach (var attribute in attributes)
                {
                    Server.Current.Log(string.Format("{0}...", attribute.CommandName), false);

                    commandTable.Add(attribute.CommandName,
                                     new CommandInfo()
                                     {
                                        Command = (PlayerCommand)Activator.CreateInstance(commandType, attribute.Parameters),
                                        IsAdmin = attribute.IsAdmin,
                                        MinimumLevel = attribute.MinimumLevel,
                                        TickLength = attribute.TickLength,
                                        CommandName = StringHelpers.Capitalize(attribute.CommandName),
                                     });

                    Server.Current.Log("done");
                }
            }           
        }

        public CommandInfo FindCommand(string commandName, bool isNpc = false)
        {
            return FindCommand(commandName, new Player() {}, isNpc);
        }

        public CommandInfo FindCommand(string commandName, Player player, bool isNpc = false)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return null;

            commandName = commandName.ToLower();

            foreach (var item in commandTable)
            {
                if (item.Key.StartsWith(commandName))
                {
                    // if npc, ignore level/admin checks etc
                    if (isNpc)
                        return item.Value;

                    // check minimum level if player isn't admin
                    if (player.Level < item.Value.MinimumLevel && !player.IsAdmin)
                        continue;
                    
                    if (item.Value.IsAdmin && !player.IsAdmin)
                        continue;

                    return item.Value;
                }
            }

            return null;
        }

        public IEnumerable<CommandInfo> FindCommands(int level)
        {
            return commandTable.Values.Where(c => c.MinimumLevel <= level);
        }
    }
}
