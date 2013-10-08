using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.World;

namespace FoxMud.Game.State
{
    class PlayingState : SessionStateBase
    {
        private void TryExecuteCommand(string input)
        {
            CommandContext commandContext = CommandContext.Create(input);
            Player player = Server.Current.Database.Get<Player>(Session.Player.Key);
            var command = Server.Current.CommandLookup.FindCommand(commandContext.CommandName, player.IsAdmin);

            if (command != null)
            {
                if (command is CallbackCommand)
                {
                    string callbackCommand = string.Empty;
                    (command as CallbackCommand).Execute(Session, commandContext, out callbackCommand);
                    TryExecuteCommand(callbackCommand);
                }
                else
                {
                    command.Execute(Session, commandContext);
                }
            }
            else
            {
                Session.WriteLine("Command not recognized");
            }
        }

        public override void OnStateEnter()
        {
            TryExecuteCommand("look");
            Session.Player.WritePrompt();
            base.OnStateEnter();
        }

        public override void OnInput(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                TryExecuteCommand(input);
            }

            Session.Player.WritePrompt();
            base.OnInput(input);
        }

        public override void OnStateShutdown()
        {
            Server.Current.Database.Save(Session.Player);
            var room = Server.Current.Database.Get<Room>(Session.Player.Location);
            room.RemovePlayer(Session.Player);

            base.OnStateShutdown();
        }
    }
}
