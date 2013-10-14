using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FoxMud.Game.World
{
    class NonPlayer
    {
        public GameStatus Status { get; set; }
        public string Name { get; set; }
        public string[] Keywords { get; set; }
        public string Description { get; set; }
        public int Hp { get; set; }
        public bool Aggro { get; set; }
        public int Armor { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public List<string> AllowedRooms { get; set; }

        [JsonIgnore]
        public bool DoesWander
        {
            get
            {
                if (AllowedRooms != null)
                {
                    return AllowedRooms.Count > 0;
                }

                return false;
            }
        }
    }
}
