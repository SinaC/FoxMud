using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game
{
    class ExperienceResolver
    {
        public static readonly int FirstLevel = 1000;
        public static readonly int LastLevel = 1000000;
        public static readonly int Levels = 50;

        static double B = Math.Log((double)LastLevel / FirstLevel) / (Levels - 1);
        static double A = FirstLevel / (Math.Exp(B) - 1.0);

        public static bool CanLevelUp(int level, int experience)
        {
            int oldXp = Convert.ToInt32(A * Math.Exp(B * (level - 1)));
            int newXp = Convert.ToInt32(A * Math.Exp(B * level));
            return newXp - oldXp > 0;
        }

        public static int ExperienceRequired(int level)
        {
            int oldXp = Convert.ToInt32(A * Math.Exp(B * (level - 1)));
            int newXp = Convert.ToInt32(A * Math.Exp(B * level));
            return newXp - oldXp;
        }

        public static void ApplyExperience(Player player, int damage)
        {
            player.Experience += damage;
        }
    }
}
