using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AutoMapper;
using FoxMud.Db;
using FoxMud.Game.Command;
using FoxMud.Game.Item;
using Newtonsoft.Json;

namespace FoxMud.Game.World
{
    /// <summary>
    /// this is similar to the Template class for items, in that concrete
    /// NPC's will be generated with another class
    /// </summary>
    class MobTemplate : GenericCharacter, Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }

        public GameStatus Status { get; set; }
        public string Name { get; set; }
        public string[] Keywords { get; set; }
        public string Description { get; set; }
        public string[] RespawnRoom { get; set; }
        public string Location { get; set; }
        public string[] Phrases { get; set; }
        public double TalkProbability { get; set; }
        public long MinimumTalkInterval { get; set; }
        public bool Aggro { get; set; }
        public List<string> AllowedRooms { get; set; }
        public new List<string> Inventory { get; private set; }
        public new Dictionary<Wearlocation, string> Equipped { get; private set; }
        public new Dictionary<string, Tuple<double, double>> Skills { get; private set; }
        public int Gold { get; set; }
        public int MaxGold { get; set; }
        public int MinGold { get; set; }

        public MobTemplate()
        {
            AllowedRooms = new List<string>();
            Inventory = new List<string>();
            Equipped = new Dictionary<Wearlocation, string>();
            Skills = new Dictionary<string, Tuple<double, double>>();
        }
    }

    /// <summary>
    /// the concrete version of MobTemplate i.e. the spawning mob
    /// </summary>
    class NonPlayer : GenericCharacter, Storable
    {
        private Guid _guid;
        private DateTime _lastTimeTalked;
        private DateTime _lastTimeWalked;
        private bool _skillLoopStarted;
        private bool _skillReady;

        public string Key
        {
            get { return _guid.ToString(); }
        }

        public GameStatus Status { get; set; }
        public string Name { get; set; }
        public string MobTemplateKey { get; set; }
        public string[] Keywords { get; set; }
        public string Description { get; set; }
        public string[] RespawnRoom { get; set; }
        public string Location { get; set; }
        public string[] Phrases { get; set; }
        public double TalkProbability { get; set; }
        public long MinimumTalkInterval { get; set; }
        public bool Aggro { get; set; }
        public List<string> AllowedRooms { get; private set; }
        public int Gold { get; set; }
        public int MaxGold { get; set; }
        public int MinGold { get; set; }
        // hack: slightly overrides the Player-style Skills property
        public new Dictionary<string, Tuple<double, double>> Skills { get; private set; }

        [JsonConstructor]
        private NonPlayer(string key, string name, GameStatus status, string[] keywords, string description, string[] respawnRoom, int hitPoints, bool aggro, int baseArmor, 
            string mobTemplateKey, int baseHitRoll, int baseDamRoll, List<string> allowedRooms, Dictionary<string, string> inventory, int gold, int maxGold, int minGold,
            Dictionary<Wearlocation, WearSlot> equipped, string location, string[] phrases, double talkProbability, long minimumTalkInterval, bool isShopkeeper,
            Dictionary<string,Tuple<double, double>> skills)
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
            _hitPoints = hitPoints;
            Aggro = aggro;
            BaseArmor = baseArmor;
            BaseHitRoll = baseHitRoll;
            BaseDamRoll = baseDamRoll;
            AllowedRooms = allowedRooms ?? new List<string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
            Skills = skills ?? new Dictionary<string, Tuple<double, double>>();
            _skillReady = true;
            _lastTimeTalked = DateTime.Now;
            _lastTimeWalked = DateTime.Now;
            IsShopkeeper = isShopkeeper;
            Gold = gold;
            MaxGold = maxGold;
            MinGold = minGold;
        }

        public NonPlayer()
        {
            var guid = Guid.NewGuid();
            while (Server.Current.Database.Exists<NonPlayer>(guid.ToString()))
            {
                guid = Guid.NewGuid();
            }

            _guid = guid;
            _lastTimeTalked = DateTime.Now;
            _lastTimeWalked = DateTime.Now;
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

        public string GetRespawnRoom()
        {
            if (RespawnRoom == null || RespawnRoom.Length == 0)
                return string.Empty;

            if (RespawnRoom != null && RespawnRoom.Length > 1)
                return RespawnRoom[Server.Current.Random.Next(0, RespawnRoom.Length)];

            return RespawnRoom[0];
        }

        public CombatRound Die(bool shutdown = false)
        {
            var round = new CombatRound();

            if (!shutdown)
            {
                // create a corpse item with .ContainsItems equal to whatever was held/equipped
                var corpseTemplate = Server.Current.Database.Get<Template>("corpse");
                var dupedCorpse = Mapper.Map<PlayerItem>(corpseTemplate);
                foreach (var item in Inventory
                    .Select(i => new KeyValuePair<string, string>(i.Key, i.Value))
                    .Union(Equipped.Values.Select(e => new KeyValuePair<string, string>(e.Key, e.Name))))
                {
                    var corpseItem = Server.Current.Database.Get<PlayerItem>(item.Key);
                    dupedCorpse.ContainedItems[corpseItem.Key] = corpseItem.Name;
                }

                dupedCorpse.Name = string.Format("The corpse of {0}", Name);
                dupedCorpse.Description = string.Format("The corpse of {0}", Name.ToLower());
                dupedCorpse.Keywords = new List<string>() { "corpse", Name }.ToArray();
                dupedCorpse.WearLocation = Wearlocation.Corpse;
                dupedCorpse.Gold = getRandomGold(Gold);

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

                round.AddText(null, string.Format("`R{0} is DEAD!!!", Name), CombatTextType.Room);

                // get area from this.RespawnRoom
                var area = Server.Current.Areas.FirstOrDefault(a => a.Key == room.Area);

                // add to .RepopQueue
                area.RepopQueue.Add(MobTemplateKey);
            }
            else
            {
                // delete inventory/equipped items' .db files
                foreach (var key in Inventory.Keys.Union(Equipped.Values.Select(e => e.Key)))
                    ItemHelper.DeleteItem(key);
            }

            // delete .db file
            Server.Current.Database.Delete<NonPlayer>(Key);

            return round;
        }

        private int getRandomGold(int gold)
        {
            if (gold == 0)
                return 0;

            return Server.Current.Random.Next(MinGold, MaxGold);
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

                var damageAction = CombatHelper.GetDamageAction(player, damage);

                var playerText = string.Format("{0} {1} you for {2} damage!\n", Name, damageAction.Plural, damage);
                round.AddText(player, playerText, CombatTextType.Player);

                var groupText = string.Format("{0} {1} {2}!\n", Name, damageAction.Plural, player.Forename);
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

            if (!_skillLoopStarted && Skills.Count > 0)
                doSkillLoop(null, null);

            return round;
        }

        private void doSkillLoop(object sender, ElapsedEventArgs e)
        {
            _skillLoopStarted = true;
            _skillReady = true;

            if (HitPoints > 0) // don't try to hit if dead
            {
                // get random skill and delay
                var keys = new List<string>(Skills.Keys);
                var size = Skills.Count;
                var skillKey = keys[Server.Current.Random.Next(size)];
                var skill = Server.Current.CombatSkills.FirstOrDefault(s => s.Key == skillKey);
                var command = Server.Current.CommandLookup.FindCommand(skillKey, true);
                
                if (skill == null || command == null)
                {
                    Server.Current.Log(string.Format("Can't find NPC skill: {0}", skillKey));
                    return;
                }

                var frequency = Skills[skillKey].Item1;
                var effectiveness = Skills[skillKey].Item2;
                // if frequency check hits
                if (Server.Current.Random.NextDouble() < frequency)
                {
                    // find the fight
                    var fight = Server.Current.CombatHandler.FindFight(this);

                    if (fight == null)
                        return;

                    // get random player to hit
                    var playerToHit = fight.GetFighters().OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                    if (playerToHit == null)
                        return;
                    
                    // get room
                    var room = RoomHelper.GetPlayerRoom(playerToHit.Location);

                    // if effectiveness hits
                    if (Server.Current.Random.NextDouble() < effectiveness)
                    {
                        // do skill hit
                        var damage = Server.Current.Random.Next(skill.MinDamage, skill.MaxDamage + 1);
                        var damageAction = CombatHelper.GetDamageAction(playerToHit, damage);

                        // message
                        playerToHit.Send(string.Format("{0}{1} {2} {3} you for {4} damage!", Name, Name.EndsWith("s") ? "'" : "'s", skillKey.ToLower(), damageAction.Plural, damage), null);
                        playerToHit.HitPoints -= damage;
                        
                        room.SendPlayers(
                            string.Format("{0}{1} {2} {3} {4}", 
                                Name, Name.EndsWith("s") ? "'" : "'s", skillKey.ToLower(), damageAction.Plural, playerToHit.Forename), 
                            playerToHit, null, playerToHit);


                        // check if player dead
                        if (playerToHit.HitPoints <= 0)
                        {
                            // almost identical code used in combat handler when mob kills player in normal combat
                            playerToHit.Die(); // changes status

                            var statusText = Player.GetStatusText(playerToHit.Status).ToUpper();

                            var playerText = string.Format("You are {0}!!!", statusText);
                            if (playerToHit.HitPoints < Server.DeadHitPoints)
                            {
                                playerText += " You have respawned, but you're in a different location.\n" +
                                              "Your corpse will remain for a short while, but you'll want to retrieve your\n" +
                                              "items in short order.";

                                playerToHit.DieForReal();
                            }

                            var groupText = string.Format("{0} is {1}!!!", playerToHit.Forename, statusText);

                            playerToHit.Send(playerText, null);
                            room.SendPlayers(groupText, playerToHit, null, playerToHit);

                            fight.RemoveFromCombat(playerToHit);

                            if (!fight.GetFighters().Any())
                            {
                                fight.End();
                                return; // so timer doesn't start again
                            }
                        }
                    }
                    else
                    {
                        // miss message
                        playerToHit.Send(string.Format("{0}{1} {2} misses you!", Name, Name.EndsWith("s") ? "'" : "'s", skillKey.ToLower()), null);
                        room.SendPlayers(
                            string.Format("{0}{1} {2} misses {3}", Name, skillKey.EndsWith("s") ? "'" : "'s",
                                          skillKey.ToLower(),
                                          playerToHit.Forename), playerToHit, null,
                            playerToHit);
                    }
                }

                _skillReady = false;

                // set delay and call this method again
                var t = new Timer()
                    {
                        AutoReset = false,
                        Interval = (long) command.TickLength,
                    };

                t.Elapsed += doSkillLoop;
                t.Start();
            }
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
            if ((DateTime.Now - _lastTimeTalked).TotalMilliseconds <= MinimumTalkInterval)
                return;

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

        protected void Walk()
        {
            if ((DateTime.Now - _lastTimeWalked).TotalMilliseconds <= Server.MobWalkInterval)
                return;

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
