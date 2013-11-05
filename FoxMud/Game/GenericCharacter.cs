using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Item;
using Newtonsoft.Json;

namespace FoxMud.Game
{
    abstract class GenericCharacter
    {
        protected int _hitPoints;

        public int BaseStrength { get; set; }
        public int BaseDexterity { get; set; }
        public int BaseConstitution { get; set; }
        public int BaseIntelligence { get; set; }
        public int BaseWisdom { get; set; }
        public int BaseCharisma { get; set; }
        public int BaseLuck { get; set; }
        public int BaseDamRoll { get; set; }
        public int BaseHitRoll { get; set; }
        public int BaseHp { get; set; }
        public int BaseArmor { get; set; }
        public bool IsShopkeeper { get; set; }
        
        public int HitPoints
        {
            get { return _hitPoints; }
            set { _hitPoints = value; }
        }

        public Dictionary<string, string> Inventory { get; protected set; }
        public Dictionary<Wearlocation, WearSlot> Equipped { get; protected set; }
        public Dictionary<string, double> Skills { get; protected set; }

        [JsonIgnore]
        public int MaxHitPoints
        {
            get
            {
                if (IsShopkeeper)
                    return int.MaxValue;

                if (Equipped != null)
                    return BaseHp + Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).HpBonus);

                return BaseHp;
            }
        }

        [JsonIgnore]
        public int Armor
        {
            get
            {
                if (IsShopkeeper)
                    return int.MaxValue;

                if (Equipped != null)
                    return BaseArmor +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).ArmorBonus);

                return BaseArmor;
            }
        }

        [JsonIgnore]
        public int HitRoll
        {
            get
            {
                if (IsShopkeeper)
                    return int.MaxValue;

                if (Equipped != null)
                    return BaseHitRoll +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).HitRoll);

                return BaseHitRoll;
            }
        }

        [JsonIgnore]
        public int DamRoll
        {
            get
            {
                if (IsShopkeeper)
                    return int.MaxValue;

                if (Equipped != null)
                    return BaseDamRoll +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).DamRoll);

                return BaseDamRoll;
            }
        }

        [JsonIgnore]
        public int Strength
        {
            get
            {
                if (Equipped != null)
                    return BaseStrength +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).StrengthBonus);

                return BaseStrength;
            }
        }

        [JsonIgnore]
        public int Dexterity
        {
            get
            {
                if (Equipped != null)
                    return BaseDexterity +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).DexterityBonus);

                return BaseDexterity;
            }
        }

        [JsonIgnore]
        public int Constitution
        {
            get
            {
                if (Equipped != null)
                    return BaseConstitution +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).ConstitutionBonus);

                return BaseConstitution;
            }
        }

        [JsonIgnore]
        public int Intelligence
        {
            get
            {
                if (Equipped != null)
                    return BaseIntelligence +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).IntelligenceBonus);

                return BaseIntelligence;
            }
        }

        [JsonIgnore]
        public int Wisdom
        {
            get
            {
                if (Equipped != null)
                    return BaseWisdom +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).WisdomBonus);

                return BaseWisdom;
            }
        }

        [JsonIgnore]
        public int Charisma
        {
            get
            {
                if (Equipped != null)
                    return BaseCharisma +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).CharismaBonus);

                return BaseCharisma;
            }
        }

        [JsonIgnore]
        public int Luck
        {
            get
            {
                if (Equipped != null)
                    return BaseLuck +
                           Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).LuckBonus);

                return BaseLuck;
            }
        }

        [JsonIgnore]
        public string HitPointDescription
        {
            get
            {
                if (HitPoints >= MaxHitPoints)
                    return "is perfectly healthy";

                var hp = HitPoints / (double)MaxHitPoints;

                if (hp >= 0.9)
                    return "has a few scratches";
                if (hp >= 0.8)
                    return "has some cuts";
                if (hp >= 0.7)
                    return "has a big gash";
                if (hp >= 0.6)
                    return "has several wounds";
                if (hp >= 0.5)
                    return "is bleeding heavily";
                if (hp >= 0.4)
                    return "is gushing blood";
                if (hp >= 0.3)
                    return "is pouring blood everywhere";
                if (hp >= 0.2)
                    return "is leaking guts";
                if (hp >= 0.1)
                    return "is bleeding from every orifice possible";
                if (hp >= 0)
                    return "is about to die";
                if (hp >= Server.IncapacitatedHitPoints)
                    return "is incapacitated";

                return "is mortally wounded";
            }
        }
    }
}
