using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game
{
    class DirectionHelper
    {
        public static bool isValidDirection(string direction)
        {
            return
                new string[]
                    {"north", "northeast", "northwest", "south", "southeast", "southwest", "east", "west", "up", "down"}
                    .Contains(direction.ToLower());
        }

        public static string GetOppositeDirection(string direction)
        {
            switch (direction.ToLower())
            {
                case "n":
                case "north":
                    return "south";
                case "ne":
                case "northeast":
                    return "southwest";
                case "nw":
                case "northwest":
                    return "southeast";
                case "s":
                case "south":
                    return "north";
                case "sw":
                case "southwest":
                    return "northeast";
                case "se":
                case "southeast":
                    return "northwest";
                case "e":
                case "east":
                    return "west";
                case "w":
                case "west":
                    return "east";
                case "u":
                case "up":
                    return "down";
                case "d":
                case "down":
                    return "up";
                default:
                    return string.Empty;
            }
        }
    }
}
