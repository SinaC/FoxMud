using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.Command
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)]
    public class CommandAttribute : Attribute
    {
        public CommandAttribute(string commandName, bool isAdmin, params object[] args)
        {
            CommandName = commandName;
            IsAdmin = isAdmin;
            Parameters = args;
        }

        public string CommandName { get; private set; }
        public bool IsAdmin { get; private set; }
        public object[] Parameters { get; private set; }
    }
}
