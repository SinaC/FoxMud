using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;

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
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }
    }


    /// <summary>
    /// a unique item, keyed by guid; these objects are used to store specific instances
    /// of an item so items can be renamed, have different attributes, etc 
    /// </summary>
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
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public Dictionary<string, string> ContainedItems { get; set; }

        // need this empty constructor for automapper
        public PlayerItem()
        {
            Guid guid = Guid.NewGuid();
            while (itemExists(guid))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;
        }

        protected bool itemExists(Guid guid)
        {
            return Server.Current.Database.Get<PlayerItem>(guid.ToString()) != null;
        }

        public virtual void Equip(Player player)
        {
            throw new NotImplementedException();
        }

        public virtual void Unequip(Player player)
        {
            throw new NotImplementedException();
        }
    }
}
