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
            return Server.Current.Database.Get<Room>(player.Location);
        }
    }
}
