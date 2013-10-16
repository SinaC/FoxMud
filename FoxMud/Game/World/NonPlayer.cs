using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;
using FoxMud.Game.Item;
using Newtonsoft.Json;

namespace FoxMud.Game.World
{
    /// <summary>
    /// this is similar to the Template class for items, in that concrete
    /// NPC's will be generated with another class
    /// </summary>
    class MobTemplate : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public GameStatus Status { get; set; }
        public string Name { get; set; }
        public string[] Keywords { get; set; }
        public string Description { get; set; }
        public string RespawnRoom { get; set; }
        public string Location { get; set; }
        public int Hp { get; set; }
        public bool Aggro { get; set; }
        public int Armor { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public List<string> AllowedRooms { get; private set; }
        public List<string> Inventory { get; private set; }
        public Dictionary<Wearlocation, string> Equipped { get; private set; }

        public MobTemplate()
        {
            AllowedRooms = new List<string>();
            Inventory = new List<string>();
            Equipped = new Dictionary<Wearlocation, string>();
        }
    }

    /// <summary>
    /// the concrete version of MobTemplate i.e. the spawning mob
    /// </summary>
    class NonPlayer : Storable
    {
        private Guid _guid;
        private int _hp;

        public string Key
        {
            get { return _guid.ToString(); }
        }

        public GameStatus Status { get; set; }
        public string Name { get; set; }
        public string MobTemplateKey { get; set; }
        public string[] Keywords { get; set; }
        public string Description { get; set; }
        public string RespawnRoom { get; set; }
        public string Location { get; set; }
        
        public bool Aggro { get; set; }
        public int Armor { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public List<string> AllowedRooms { get; private set; }
        public Dictionary<string, string> Inventory { get; private set; }
        public Dictionary<Wearlocation, WearSlot> Equipped { get; private set; }

        public int Hp
        {
            get
            {
                return _hp;
            }
            set
            {
                _hp = value;
                if (_hp <= 0)
                    Die();
            }
        }

        private void Die()
        {
            // get area from this.RespawnRoom

            // add to .RepopQueue
            throw new NotImplementedException();
        }

        [JsonIgnore]
        public bool DoesWander
        {
            get
            {
                if (AllowedRooms != null)
                    return AllowedRooms.Count > 1;

                return false;
            }
        }

        [JsonConstructor]
        private NonPlayer(string key, string name, GameStatus status, string[] keywords, string description, string respawnRoom, int hp, bool aggro, int armor, string mobTemplateKey,
            int minDamage, int maxDamage, List<string> allowedRooms, Dictionary<string, string> inventory, Dictionary<Wearlocation, WearSlot> equipped, string location)
        {
            _guid = new Guid(key);

            Name = name;
            MobTemplateKey = mobTemplateKey;
            Status = status;
            Keywords = keywords;
            Description = description;
            RespawnRoom = respawnRoom;
            Location = location;
            Hp = hp;
            Aggro = aggro;
            Armor = armor;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            AllowedRooms = allowedRooms ?? new List<string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
        }

        public NonPlayer()
        {
            Guid guid = Guid.NewGuid();
            while (Server.Current.Database.Exists<NonPlayer>(guid.ToString()))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;
        }
    }
}
