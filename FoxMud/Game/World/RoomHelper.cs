using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.World
{
    class RoomHelper
    {
        public static Room GetPlayerRoom(Player player)
        {
            return GetPlayerRoom(player.Location);
        }

        public static Room GetPlayerRoom(string key)
        {
            return GetRoom(key);
        }

        public static Room GetRoom(string key)
        {
            var room = Server.Current.Database.Get<Room>(key);
            return room;
        }

        public static void SaveRoom(Room room)
        {
            Server.Current.Database.Save(room);
        }

        public static Area GetArea(string key)
        {
            var area = Server.Current.Database.Get<Area>(key);
            return area;
        }

        public static string GetDefaultRoomDescription()
        {
            return "This exitless room has no description.";
        }

        public static string GenerateKey(string key)
        {
            int index = 2;

            while (Server.Current.Database.Get<Room>(key.ToLower() + index.ToString()) != null)
            {
                index++;
            }

            return key + index.ToString(); ;
        }
    }
}
