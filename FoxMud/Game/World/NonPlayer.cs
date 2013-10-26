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
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; set; }
        public bool Aggro { get; set; }
        public int Armor { get; set; }
        public int HitRoll { get; set; }
        public int DamRoll { get; set; }
        public List<string> AllowedRooms { get; private set; }
        public List<string> Inventory { get; private set; }
        public Dictionary<Wearlocation, string> Equipped { get; private set; }
        public bool IsShopkeeper { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public int Luck { get; set; }

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
        private DateTime _lastTimeTalked;
        private DateTime _lastTimeWalked;

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
        public int HitRoll { get; set; }
        public int DamRoll { get; set; }
        public List<string> AllowedRooms { get; private set; }
        public Dictionary<string, string> Inventory { get; private set; }
        public Dictionary<Wearlocation, WearSlot> Equipped { get; private set; }
        public bool IsShopkeeper { get; set; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Charisma { get; set; }
        public int Luck { get; set; }

        public CombatRound Die(bool shutdown = false)
        {
            CombatRound round = new CombatRound();

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

                dupedCorpse.Name = string.Format("The corpse of {0}", Name);
                dupedCorpse.Description = string.Format("The corpse of {0}", Name.ToLower());
                dupedCorpse.Keywords = new List<string>() {"corpse", Name}.ToArray();
                dupedCorpse.WearLocation = Wearlocation.Corpse;

                // put corpse in room
                var room = RoomHelper.GetPlayerRoom(Location);
                room.AddItem(dupedCorpse);
                Console.WriteLine("NEW CORPSE: {0}", dupedCorpse.Key);
                room.CorpseQueue[dupedCorpse.Key] = DateTime.Now.AddMilliseconds(Server.CorpseDecayTime);

                // must cache since we're not saving
                Server.Current.Database.Put(dupedCorpse);

                // delete mob
                room.RemoveNpc(this);
                Server.Current.Database.Delete<NonPlayer>(Key);

                round.AddText(null, string.Format("{0} is DEAD!!!", Name), CombatTextType.Room);

                // get area from this.RespawnRoom
                var area = Server.Current.Areas.FirstOrDefault(a => a.Key == room.Area);

                // add to .RepopQueue
                area.RepopQueue.Add(MobTemplateKey);
            }
            else
            {
                // delete inventory/equipped items' .db files
                foreach (var key in Inventory.Keys.Union(Equipped.Values.Select(e => e.Key)))
                    Server.Current.Database.Delete<PlayerItem>(key);
            }

            // delete .db file
            Server.Current.Database.Delete<NonPlayer>(Key);

            return round;
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
        private NonPlayer(string key, string name, GameStatus status, string[] keywords, string description, string respawnRoom, int hitPoints, bool aggro, int armor, string mobTemplateKey,
            int hitRoll, int damRoll, List<string> allowedRooms, Dictionary<string, string> inventory, Dictionary<Wearlocation, WearSlot> equipped, string location,
            string[] phrases, double talkProbability, long minimumTalkInterval, bool isShopkeeper)
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
            HitPoints = hitPoints;
            Aggro = aggro;
            Armor = armor;
            HitRoll = hitRoll;
            DamRoll = damRoll;
            AllowedRooms = allowedRooms ?? new List<string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
            _lastTimeTalked = DateTime.Now;
            _lastTimeWalked = DateTime.Now;
            IsShopkeeper = IsShopkeeper;
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
            _lastTimeWalked = DateTime.Now;
        }

        public CombatRound Hit(Player player)
        {
            var round = new CombatRound();

            // roll to hit, if success, then hit
            if (Server.Current.Random.Next(HitRoll) + 1 >= player.Armor)
            {
                // hit
                var damage = Server.Current.Random.Next(DamRoll) + 1;
                player.HitPoints -= damage;
                
                var playerText = string.Format("{0} hits you for {1} damage!\n", Name, damage);
                round.AddText(player, playerText, CombatTextType.Player);

                var groupText = string.Format("{0} hits {1}!\n", Name, player.Forename);
                round.AddText(player, groupText, CombatTextType.Group);
            }
            else
            {
                // miss
                var playerText = string.Format("{0} misses you!", Name);
                round.AddText(player, playerText, CombatTextType.Player);

                var groupText = string.Format("{0} misses {1}!\n", Name, player.Forename);
                round.AddText(player, groupText, CombatTextType.Group);
            }

            return round;
        }

        public void TalkOrWalk()
        {
            if (Phrases != null && Phrases.Length > 0
                && AllowedRooms != null && AllowedRooms.Count > 1)
            {
                if (Server.Current.Random.Next(2) == 0)
                    Talk();
                else
                    Walk();
            }
            else if (Phrases != null && Phrases.Length > 0)
                Talk();
            else if (AllowedRooms != null && AllowedRooms.Count > 1)
                Walk();
        }

        protected void Talk()
        {
            if ((DateTime.Now - _lastTimeTalked).TotalMilliseconds > MinimumTalkInterval)
            {
                // set the new interval
                _lastTimeTalked = DateTime.Now;

                // talk at random
                double prob = Server.Current.Random.NextDouble();
                if (prob < TalkProbability && Phrases != null && Phrases.Length > 0)
                {
                    var phrase = Phrases[Server.Current.Random.Next(Phrases.Length)];

                    // say it to the room
                    var room = RoomHelper.GetPlayerRoom(Location);
                    if (room != null)
                    {
                        string message = string.Format("{0} says, \"{1}\"", Name, phrase);
                        room.SendPlayers(message, null, null, null);
                    }
                }
            }
        }

        protected void Walk()
        {
            if ((DateTime.Now - _lastTimeWalked).TotalMilliseconds > Server.MobWalkInterval)
            {
                _lastTimeWalked = DateTime.Now;

                var room = RoomHelper.GetPlayerRoom(Location);

                // get allowed exits
                var allowedExits = room.Exits.Where(e => AllowedRooms.Contains(e.Value.LeadsTo) && e.Value.IsOpen).ToList();

                if (allowedExits.Any() && Server.Current.Random.NextDouble() < 0.5)
                {
                    var exit = allowedExits.Skip(Server.Current.Random.Next(allowedExits.Count())).FirstOrDefault();

                    room.RemoveNpc(this);
                    var newRoom = RoomHelper.GetPlayerRoom(exit.Value.LeadsTo);
                    newRoom.AddNpc(this);
                    Location = newRoom.Key;
                    room.SendPlayers(string.Format("{0} heads {1}.", Name, DirectionHelper.GetDirectionWord(exit.Key)),
                                     null, null, null);
                    newRoom.SendPlayers(
                        string.Format("{0} arrives from the {1}.", Name, DirectionHelper.GetOppositeDirection(exit.Key)),
                        null, null, null);
                }
            }
        }
    }
}
