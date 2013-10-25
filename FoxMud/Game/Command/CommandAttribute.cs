using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string commandName, bool isAdmin, TickDelay tickLength, params object[] args)
        {
            CommandName = commandName;
            IsAdmin = isAdmin;
            TickLength = tickLength;
            Parameters = args;
        }

        public CommandAttribute(string commandName, bool isAdmin, params object[] args)
            : this(commandName, isAdmin, TickDelay.Single, args)
        {
            
        }

        public string CommandName { get; private set; }
        public bool IsAdmin { get; private set; }
        public TickDelay TickLength { get; private set; }
        public object[] Parameters { get; private set; }
    }
}
