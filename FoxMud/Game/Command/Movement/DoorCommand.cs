using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Movement
{
    [Command("open", false)]
    class OpenCommand : PlayerCommand
    {
        public void Execute(Session session, CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                session.WriteLine("Open what?");
                return;
            }

            Room room = Server.Current.Database.Get<Room>(session.Player.Location);
            string exitToFind = context.Arguments[0].ToLower();
            var exit = room.FindExitByPartialName(exitToFind);
            if (exit == null || !exit.IsDoor)
            {
                session.WriteLine("Theres nothing to open...");
                return;
            }
            if (exit.IsOpen)
            {
                session.WriteLine("The door is already open");
                return;
            }
            exit.IsOpen = true;
            session.Player.Send("You open the door.", session.Player);
            room.SendPlayers("%d opens a door.", session.Player, null, session.Player);

            // Now update the linked room
            var linkedRoom = Server.Current.Database.Get<Room>(exit.LeadsTo);
            if (linkedRoom == null)
                return;
            RoomExit linkedExit = linkedRoom.FindExitByLinkedRoom(room.Key);
            if (linkedExit == null || !linkedExit.IsDoor || linkedExit.IsOpen)
                return;
            linkedExit.IsOpen = true;
            linkedRoom.SendPlayers("A door opens.", null, null);
        }
    }

    [Command("close", false)]
    class CloseCommand : PlayerCommand
    {
        public void Execute(Session session, CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                session.WriteLine("Close what?");
                return;
            }

            Room room = Server.Current.Database.Get<Room>(session.Player.Location);
            string exitToFind = context.Arguments[0].ToLower();
            var exit = room.FindExitByPartialName(exitToFind);
            if (exit == null || !exit.IsDoor)
            {
                session.WriteLine("Theres nothing to close...");
                return;
            }
            if (!exit.IsOpen)
            {
                session.WriteLine("The door is already closed");
                return;
            }
            exit.IsOpen = false;
            session.Player.Send("You close the door.", session.Player);
            room.SendPlayers("%d closes a door.", session.Player, null, session.Player);

            // Now update the linked room
            var linkedRoom = Server.Current.Database.Get<Room>(exit.LeadsTo);
            if (linkedRoom == null)
                return;
            RoomExit linkedExit = linkedRoom.FindExitByLinkedRoom(room.Key);
            if (linkedExit == null || !linkedExit.IsDoor || !linkedExit.IsOpen)
                return;
            linkedExit.IsOpen = false;
            linkedRoom.SendPlayers("A door closes.", null, null);
        }
    }
}
