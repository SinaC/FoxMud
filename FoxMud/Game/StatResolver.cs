using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game
{
    class StatResolver
    {
        public static int RollStat()
        {
            return Server.Current.Random.Next(8, 19);
        }

        public static int RollHitPoints()
        {
            return Server.Current.Random.Next(10, 31);
        }
    }
}
