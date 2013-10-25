using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Visual;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Movement
{
    [Command("e", false, "east")]
    [Command("east", false, "east")]
    [Command("s", false, "south")]
    [Command("south", false, "south")]
    [Command("w", false, "west")]
    [Command("west", false, "west")]
    [Command("n", false, "north")]
    [Command("north", false, "north")]
    [Command("ne", false, "northeast")]
    [Command("northeast", false, "northeast")]
    [Command("nw", false, "northwest")]
    [Command("northwest", false, "northwest")]
    [Command("sw", false, "southwest")]
    [Command("southwest", false, "southwest")]
    [Command("se", false, "southeast")]
    [Command("southeast", false, "southeast")]
    [Command("u", false, "up")]
    [Command("up", false, "up")]
    [Command("d", false, "down")]
    [Command("down", false, "down")]
    class MoveCommand : PlayerCommand
    {
        private readonly string direction;

        public MoveCommand(string direction)
        {
            this.direction = direction;
        }

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: <direction>");
            session.WriteLine("Example: \"ne\" is short for northeast");
            session.WriteLine("Example: \"up\" will move you up");
        }

        public override void Execute(Session session, CommandContext context)
        {
            // todo: messages per position e.g. "can't do that while sitting", "you're fighting" etc
            if (session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You can't leave right now.");
                return;
            }

            Room room = Server.Current.Database.Get<Room>(session.Player.Location);

            if (room == null)
            {
                LookCommand.WriteNullRoomDescription(session);
                return;
            }

            if (room.HasExit(direction))
            {
                var exitTo = room.GetExit(direction);
                Room newRoom = Server.Current.Database.Get<Room>(exitTo.LeadsTo);

                if (exitTo.IsDoor && !exitTo.IsOpen)
                {
                    session.WriteLine("The door is closed.");
                    return;
                }

                room.RemovePlayer(session.Player);
                newRoom.AddPlayer(session.Player);
                session.Player.Location = newRoom.Key;

                room.SendPlayers("%d heads " + direction, session.Player, null, session.Player);
                newRoom.SendPlayers("%d arrives", session.Player, null, session.Player);
                session.Player.Send("you head " + direction, session.Player);

<<<<<<< HEAD
                var command = Server.Current.CommandLookup.FindCommand("look", false);
                command.Execute(session, CommandContext.Create("look"));

                // emit "event" for aggro mobs
                Server.Current.CombatHandler.EnterRoom(session.Player, newRoom);
=======
                var commandInfo = Server.Current.CommandLookup.FindCommand("look", false);
                commandInfo.Command.Execute(session, CommandContext.Create("look"));
>>>>>>> origin/spam-command
            }
            else
            {
                session.WriteLine("You can't go that way.");
            }
        }
    }
}
