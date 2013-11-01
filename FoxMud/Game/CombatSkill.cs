using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;

namespace FoxMud.Game
{
    class CombatSkill : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public string Name { get; set; }
        public double Effectiveness { get; set; }
        public double MaxEffectiveness { get; set; }
        public double MissEffectivenessIncrease { get; set; }
        public double HitEffectivenessIncrease { get; set; }
        public double KillingBlowEffectivenessIncrease { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
    }
}
