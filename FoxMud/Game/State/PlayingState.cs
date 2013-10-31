using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.World;

namespace FoxMud.Game.State
{
    class SpammedCommand
    {
        public CommandContext Context { get; set; }
        public CommandInfo Info { get; set; }
    }

    class PlayingState : SessionStateBase
    {
        // for spamming commands
        private readonly Queue<SpammedCommand> queuedCommands = new Queue<SpammedCommand>();
        private bool ready = true;

        private void TryExecuteCommand(string input)
        {
            var commandContext = CommandContext.Create(input);
            var player = Server.Current.Database.Get<Player>(Session.Player.Key);
            var commandInfo = Server.Current.CommandLookup.FindCommand(commandContext.CommandName, player.IsAdmin);

            TryExecuteCommand(commandContext, commandInfo);
        }

        private void TryExecuteCommand(CommandContext commandContext, CommandInfo commandInfo)
        {
            if (commandInfo != null)
            {
                if (!ready)
                {
                    Console.WriteLine("not ready. queueing event: {0} {1}", commandContext.CommandName,
                                      DateTime.Now.ToString("mm:ss.fff"));
                    // queue command
                    queuedCommands.Enqueue(new SpammedCommand()
                    {
                        Context = commandContext,
                        Info = commandInfo
                    });
                    return;
                }

                ready = false;
                Console.WriteLine("executing command: {0} {1}", commandContext.CommandName, DateTime.Now.ToString("mm:ss.fff"));
                commandInfo.Command.Execute(Session, commandContext);
                Session.Player.WritePrompt();
                if (queuedCommands.Count > 0)
                    setTimeout(queuedCommands.Dequeue(), commandInfo.TickLength); // command already queued
                else
                    setTimeout(commandInfo.TickLength); // simply delay
            }
            else
            {
                Session.WriteLine("Command not recognized");
            }
        }

        private void setTimeout(TickDelay tickDelay)
        {
            Console.WriteLine("generic timeout: {0}", DateTime.Now.ToString("mm:ss.fff"));
            var t = new System.Timers.Timer()
                {
                    Interval = (long) tickDelay,
                    AutoReset = false,
                };
            
            t.Elapsed += makeReady;
            t.Start(); // fire this in tickDelay ms
        }

        private void setTimeout(SpammedCommand command, TickDelay tickLength)
        {
            Console.WriteLine("command timeout: {0} {1}", command.Context.CommandName, DateTime.Now.ToString("mm:ss.fff"));
            var t = new System.Timers.Timer()
            {
                Interval = (long)tickLength,
                AutoReset = false,
            };

            t.Elapsed += (sender, e) => nextCommand(sender, e, command);
            t.Start();
        }

        private void nextCommand(object sender, System.Timers.ElapsedEventArgs e, SpammedCommand command)
        {
            Console.WriteLine("nextCommand: {0} {1}", command.Context.CommandName, DateTime.Now.ToString("mm:ss.fff"));
            ready = true;
            TryExecuteCommand(command.Context, command.Info);
        }

        // MAKE READYYYYY!!!!
        private void makeReady(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("makeReady: {0}", DateTime.Now.ToString("mm:ss.fff"));
            ready = true;
            if (queuedCommands.Count <= 0)
                return;

            // a command was entered in the interim
            var command = queuedCommands.Dequeue();
            Console.WriteLine("makeReady command: {0} {1}", command.Context.CommandName, DateTime.Now.ToString("mm:ss.fff"));
            TryExecuteCommand(command.Context, command.Info);
        }

        public override void OnStateEnter()
        {
            TryExecuteCommand("look");
            base.OnStateEnter();
        }

        public override void OnInput(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                TryExecuteCommand(input);
            }

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
