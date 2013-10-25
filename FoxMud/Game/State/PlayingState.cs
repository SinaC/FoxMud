using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            var commandInfo = Server.Current.CommandLookup.FindCommand(commandContext.CommandName, player.IsAdmin);

            if (commandInfo != null)
            {
                if (commandInfo.Command is CallbackCommand)
                {
                    string callbackCommand = string.Empty;
                    (commandInfo.Command as CallbackCommand).Execute(Session, commandContext, out callbackCommand);
                    TryExecuteCommand(callbackCommand);
                }
                else
                {
                    commandInfo.Command.Execute(Session, commandContext);
                    Thread.Sleep(TimeSpan.FromMilliseconds((long)commandInfo.TickLength));
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
