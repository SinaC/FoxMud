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
            return newXp - oldXp > experience;
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

        public static void LevelUp(Player player)
        {
            player.Level++;
             
            // add hit points with stat bonuses
            int hp = Server.Current.Random.Next(10, 15);

            if (Server.Current.Random.Next(15, 26) < player.Charisma)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Constitution)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Dexterity)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Intelligence)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Luck)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Strength)
                hp++;
            if (Server.Current.Random.Next(15, 26) < player.Wisdom)
                hp++;

            player.BaseHp += hp;
            if (player.OutputWriter != null)
                player.Send(
                    string.Format(
                        "`YYou gained enough experience to advance to level {0}!\nYou gained {1} hit points!",
                        player.Level, hp), null);
        }
    }
}
