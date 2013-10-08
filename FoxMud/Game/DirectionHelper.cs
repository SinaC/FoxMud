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
            return new string[] {"north", "south", "east", "west"}.Contains(direction.ToLower());
        }

        public static string GetOppositeDirection(string direction)
        {
            switch (direction.ToLower())
            {
                case "n":
                case "north":
                    return "south";
                case "s":
                case "south":
                    return "north";
                case "e":
                case "east":
                    return "west";
                case "w":
                case "west":
                    return "east";
                default:
                    return string.Empty;
            }
        }
    }
}
