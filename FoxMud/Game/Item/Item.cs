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
    abstract class Template : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        /// <summary>
        /// copies an item from its template record i.e. template items have readable keys
        /// while copied items are specific instances saved to db
        /// </summary>
        /// <param name="key">the key e.g. a small knife</param>
        /// <returns>a copy of the item with unique guid key</returns>
        public virtual PlayerItem Copy()
        {
            return new PlayerItem(this);
        }
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
        public int Weight { get; set; }
        public int Value { get; set; }
        public Wearlocation WearLocation { get; set; }

        public PlayerItem(Template item)
        {
            Guid guid = Guid.NewGuid();
            while (itemExists(guid))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;

            // copy basic properties
            Name = item.Name;
            Description = item.Description;
            Weight = item.Weight;
            Value = item.Value;
            this.WearLocation = item.WearLocation;
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
