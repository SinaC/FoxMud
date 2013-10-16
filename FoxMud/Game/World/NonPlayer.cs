using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
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
        public string[] Phrases { get; set; }
        public double TalkProbability { get; set; }
        public long MinimumTalkInterval { get; set; }
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
        private DateTime _lastTimeTalked;

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
        public string[] Phrases { get; set; }
        public double TalkProbability { get; set; }
        public long MinimumTalkInterval { get; set; }
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
                    Die(false);
            }
        }

        public void Die(bool shutdown)
        {
            if (!shutdown)
            {
                // create a corpse item with .ContainsItems equal to whatever was held/equipped
                var corpseTemplate = Server.Current.Database.Get<Template>("corpse");
                var dupedCorpse = Mapper.Map<PlayerItem>(corpseTemplate);
                foreach (var item in Inventory
                    .Select(i => new KeyValuePair<string, string>(i.Key, i.Value))
                    .Union(Equipped.Values.Select(e => new KeyValuePair<string, string>(e.Key, e.Name))))
                {
                    dupedCorpse.ContainedItems[item.Key] = item.Value;
                }

                // get area from this.RespawnRoom
                var area = Server.Current.Areas.FirstOrDefault(a => a.Rooms.Contains(Location));

                // add to .RepopQueue
                area.RepopQueue.Add(MobTemplateKey);
            }
            else
            {
                // delete inventory/equipped items' .db files
                foreach (var key in Inventory.Keys.Union(Equipped.Values.Select(e => e.Key)))
                    Server.Current.Database.Delete<PlayerItem>(key);

                // delete .db file
                Server.Current.Database.Delete<NonPlayer>(Key);
            }
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
            int minDamage, int maxDamage, List<string> allowedRooms, Dictionary<string, string> inventory, Dictionary<Wearlocation, WearSlot> equipped, string location,
            string[] phrases, double talkProbability, long minimumTalkInterval)
        {
            _guid = new Guid(key);

            Name = name;
            MobTemplateKey = mobTemplateKey;
            Status = status;
            Keywords = keywords;
            Description = description;
            RespawnRoom = respawnRoom;
            Location = location;
            Phrases = phrases;
            TalkProbability = talkProbability;
            MinimumTalkInterval = minimumTalkInterval;
            Hp = hp;
            Aggro = aggro;
            Armor = armor;
            MinDamage = minDamage;
            MaxDamage = maxDamage;
            AllowedRooms = allowedRooms ?? new List<string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
            _lastTimeTalked = DateTime.Now;
        }

        public NonPlayer()
        {
            Guid guid = Guid.NewGuid();
            while (Server.Current.Database.Exists<NonPlayer>(guid.ToString()))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;
            _lastTimeTalked = DateTime.Now;
        }

        public void Talk()
        {
            if ((DateTime.Now - _lastTimeTalked).TotalMilliseconds > MinimumTalkInterval)
            {
                // talk at random
                double prob = Server.Current.Random.NextDouble();
                if(prob < TalkProbability)
                {
                    // get random phrase
                    if (Phrases != null && Phrases.Length > 0)
                    {
                        var phrase = Phrases[Server.Current.Random.Next(Phrases.Length)];
                        
                        // say it to the room
                        var room = RoomHelper.GetPlayerRoom(Location);
                        if (room != null)
                        {
                            string message = string.Format("{0} says, \"{1}\"", Name, phrase);
                            room.SendPlayers(message, null, null, null);
                            _lastTimeTalked = DateTime.Now;
                        }
                    }
                }
            }
        }
    }
}
