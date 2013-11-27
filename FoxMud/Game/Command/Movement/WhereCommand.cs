using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Movement
{
    [Command("where", false, 1)]
    class WhereCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: where");
        }

        public override void Execute(Session session, CommandContext context)
        {
            // get room
            var room = RoomHelper.GetPlayerRoom(session.Player.Location);

            if (room == null)
            {
                Server.Current.Log(LogType.Error, "WhereCommand: couldn't find room");
                return;
            }
                
            // get area
            var area = RoomHelper.GetArea(room.Area);
            if(area == null)
            {
                Server.Current.Log(LogType.Error, "WhereCommand: couldn't find area");
                return;
            }

            Dictionary<string, string> players = new Dictionary<string, string>();

            // for each room in area, add players to list
            foreach (var roomKey in area.Rooms)
            {
                var theRoom = RoomHelper.GetRoom(roomKey);
                foreach(var playerInRoom in theRoom.GetPlayers())
                {
                    if (!players.ContainsKey(playerInRoom.Forename))
                    {
                        // get proper room name
                        var playerRoomName = RoomHelper.GetRoom(playerInRoom.Location);
                        players.Add(playerInRoom.Forename, playerRoomName.Title);
                    }
                }
            }

            session.WriteLine("Players in {0}:", area.Name);

            foreach (var player in players)
            {
                session.WriteLine(string.Format("{0,-20}{1}", player.Key, player.Value));
            }
        }
    }
}
