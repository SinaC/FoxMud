using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    class CommandContext
    {
        public string CommandName { get; private set; }
        public List<string> Arguments { get; private set; }
        public string ArgumentString { get; private set; }

        public CommandContext()
        {
            Arguments = new List<string>();
        }

        public static CommandContext Create(string input)
        {
            CommandContext result = new CommandContext();
            
            string commandName = string.Empty;
            input = input.ReadCommandLinePart(out commandName);

            result.CommandName = commandName;            
            result.ArgumentString = input.TrimStart();

            string part = string.Empty;            
            while (input != string.Empty)
            {
                input = input.ReadCommandLinePart(out part);
                result.Arguments.Add(part);                
            }

            return result;
        }
    }
}
