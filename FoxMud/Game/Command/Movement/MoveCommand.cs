using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Visual;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Movement
{
    [Command("e", false, 0, "east")]
    [Command("east", false, 0, "east")]
    [Command("s", false, 0, "south")]
    [Command("south", false, 0, "south")]
    [Command("w", false, 0, "west")]
    [Command("west", false, 0, "west")]
    [Command("n", false, 0, "north")]
    [Command("north", false, 0, "north")]
    [Command("ne", false, 0, "northeast")]
    [Command("northeast", false, 0, "northeast")]
    [Command("nw", false, 0, "northwest")]
    [Command("northwest", false, 0, "northwest")]
    [Command("sw", false, 0, "southwest")]
    [Command("southwest", false, 0, "southwest")]
    [Command("se", false, 0, "southeast")]
    [Command("southeast", false, 0, "southeast")]
    [Command("u", false, 0, "up")]
    [Command("up", false, 0, "up")]
    [Command("d", false, 0, "down")]
    [Command("down", false, 0, "down")]
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
            Console.WriteLine("walk {0} {1}", direction, DateTime.Now.ToLongTimeString());
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

                // emit "event" for aggro mobs
                Server.Current.CombatHandler.EnterRoom(session.Player, newRoom);
                var commandInfo = Server.Current.CommandLookup.FindCommand("look", session.Player);
                commandInfo.Command.Execute(session, CommandContext.Create("look"));
            }
            else
            {
                session.WriteLine("You can't go that way.");
            }
        }
    }
}
