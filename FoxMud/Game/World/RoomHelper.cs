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
            return Server.Current.Database.Get<Room>(key);
        }
    }
}
