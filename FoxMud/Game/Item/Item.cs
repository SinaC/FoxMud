using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Db;
using FoxMud.Game.Command;
using FoxMud.Game.World;
using Newtonsoft.Json;

namespace FoxMud.Game.Item
{
    /// <summary>
    /// a templated item; these items won't be 'owned' by anything, but merely serve
    /// as a template from which to create objects
    /// </summary>
    class Template : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        // in-game item attributes
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }
        public int DamRoll { get; set; }
        public int HitRoll { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }

        public int StrengthBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int ConstitutionBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int WisdomBonus { get; set; }
        public int CharismaBonus { get; set; }
        public int LuckBonus { get; set; }
    }


    /// <summary>
    /// a unique item, keyed by guid; these objects are used to store specific instances
    /// of an item so items can be renamed, have different attributes, etc 
    /// </summary>
    [Serializable]
    class PlayerItem : Storable, Equipable
    {
        private Guid _guid;

        public string Key
        {
            get { return _guid.ToString(); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        // in-game item attributes
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }
        public int DamRoll { get; set; }
        public int HitRoll { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }

        public int StrengthBonus { get; set; }
        public int DexterityBonus { get; set; }
        public int ConstitutionBonus { get; set; }
        public int IntelligenceBonus { get; set; }
        public int WisdomBonus { get; set; }
        public int CharismaBonus { get; set; }
        public int LuckBonus { get; set; }

        [JsonIgnore]
        public string AllowedToLoot { get; set; }

        [JsonIgnore]
        public int ContainerWeight
        {
            get
            {
                if (WearLocation != Wearlocation.Container)
                    return Weight;

                int _weight = Weight;
                foreach (var key in ContainedItems.Keys)
                {
                    var item = Server.Current.Database.Get<PlayerItem>(key);
                    if (item.WearLocation == Wearlocation.Container)
                        _weight += item.ContainerWeight;
                    else
                        _weight += item.Weight;
                }

                return _weight;
            }
        }

        [JsonConstructor]
        private PlayerItem(string key, string name, string description, string[] keywords, int weight, int value, Wearlocation wearLocation,
            int hpBonus, int armorBonus, int mindDamage, int maxDamage, int strengthBonus, int dexterityBonus, int constitutionBonus,
            int intelligenceBonus, int wisdomBonus, int charismaBonus, int luckBonus)
        {
            _guid = new Guid(key);
            Name = name;
            Description = description;
            Keywords = keywords;
            Weight = weight;
            Value = value;
            WearLocation = wearLocation;
            HpBonus = hpBonus;
            ArmorBonus = armorBonus;
            MinDamage = mindDamage;
            MaxDamage = maxDamage;
            StrengthBonus = strengthBonus;
            DexterityBonus = dexterityBonus;
            ConstitutionBonus = constitutionBonus;
            IntelligenceBonus = intelligenceBonus;
            WisdomBonus = wisdomBonus;
            CharismaBonus = charismaBonus;
            LuckBonus = LuckBonus;
        }

        // need this empty constructor for automapper
        public PlayerItem()
        {
            _guid = getNewGuid();
        }

        public PlayerItem Copy()
        {
            var copy = ItemHelper.DeepClone(this);
            copy._guid = getNewGuid();

            return copy;
        }

        private Guid getNewGuid()
        {
            Guid guid = Guid.NewGuid();
            while (Server.Current.Database.Exists<PlayerItem>(guid.ToString()))
            {
                guid = Guid.NewGuid();
            }

            return guid;
        }

        public void LookAt(Session session)
        {
            session.WriteLine(Description);

            if (WearLocation == Wearlocation.Container || WearLocation == Wearlocation.Corpse)
            {
                if (ContainedItems.Count == 0)
                {
                    session.WriteLine("\tEmpty");
                    return;
                }

                foreach (var itemLine in ContainedItems
                    .GroupBy(i => i.Value)
                    .Select(group => new
                {
                    ItemName = group.Key,
                    Count = group.Count()
                }))
                {
                    session.WriteLine("\t{0} ({1})", itemLine.ItemName, itemLine.Count);
                }
            }
        }

        public virtual void Equip(Session session)
        {
            // don't equip .None or .Key
            if (WearLocation == Wearlocation.None 
                || WearLocation == Wearlocation.Key 
                || WearLocation == Wearlocation.Container)
            {
                session.WriteLine("You can't equip that.");
                return;
            }

            if (session.Player.Equipped.ContainsKey(WearLocation))
            {
                session.WriteLine("You've already equipped that slot.");
                return;
            }

            // can't equip both hands if either single hand is equipped
            if (WearLocation == Wearlocation.BothHands
                && (session.Player.Equipped.ContainsKey(Wearlocation.LeftHand)
                    || session.Player.Equipped.ContainsKey(Wearlocation.RightHand)))
            {
                session.WriteLine("You can't hold a two-handed weapon with any other weapons.");
                return;
            }

            // can't eqiup either hand if both hands is equipped
            if ((WearLocation == Wearlocation.RightHand || WearLocation == Wearlocation.LeftHand)
                && session.Player.Equipped.ContainsKey(Wearlocation.BothHands))
            {
                session.WriteLine("You're already using both hands.");
                return;
            }

            session.Player.Equipped[WearLocation] = new WearSlot()
            {
                Key = Key,
                Name = Name
            };

            // remove from inventory
            session.Player.Inventory.Remove(Key);

            session.WriteLine("You don {0}.", Name);
            RoomHelper.GetPlayerRoom(session.Player)
                .SendPlayers(string.Format("%d dons {0}", Name), session.Player, null, session.Player);
        }

        public virtual void Unequip(Session session)
        {
            session.Player.Equipped.Remove(WearLocation);
            session.Player.Inventory[Key] = Name;

            session.WriteLine("You remove {0}", Name);
            RoomHelper.GetPlayerRoom(session.Player)
                .SendPlayers(string.Format("%d removes {0}", Name), session.Player, null, session.Player);
        }
    }
}
