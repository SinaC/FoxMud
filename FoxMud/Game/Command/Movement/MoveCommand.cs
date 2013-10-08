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
    class MoveCommand : PlayerCommand
    {
        private readonly string direction;

        public MoveCommand(string direction)
        {
            this.direction = direction;
        }

        public void Execute(Session session, CommandContext context)
        {
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

                var command = Server.Current.CommandLookup.FindCommand("look", false);
                command.Execute(session, CommandContext.Create("look"));
            }
            else
            {
                session.WriteLine("You can't go that way.");
            }
        }
    }
}
