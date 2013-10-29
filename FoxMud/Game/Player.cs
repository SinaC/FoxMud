using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using FoxMud.Db;
using FoxMud.Game.Item;
using FoxMud.Game.World;
using FoxMud.Text;
using Newtonsoft.Json;

namespace FoxMud.Game
{
    enum PlayerGender
    {
        Male,
        Female,
        Unknown
    }

    enum GameStatus
    {
        Sitting,
        Standing,
        Sleeping,
        Fighting,
        Incapacitated, // something like >= -3 hp
        MortallyWounded, // will die if unaided
        Dead,
    }

    struct WearSlot
    {
        public string Key { get; set; }
        public string Name { get; set; }
    }

    class Player : Storable
    {
        private string _passwordHash;
        private int _weight;
        private int _experience;
        private int _hitPoints;

        [JsonConstructor]
        private Player(
            string name, string passwordHash, bool isAdmin, string prompt, Dictionary<string,string> rememberedNames, Dictionary<string, string> inventory, int hitPoints,
            long gold, int experience, Dictionary<Wearlocation, WearSlot> equipped, GameStatus status, int hitRoll, int damRoll, int level, string respawnRoom, 
            int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma, int luck, int age, int baseHp)
        {
            Forename = name;
            _passwordHash = passwordHash;
            IsAdmin = isAdmin;
            Prompt = prompt;
            _hitPoints = hitPoints;
            BaseHp = baseHp;
            Gold = gold;
            _experience = experience;
            RememberedNames = rememberedNames ?? new Dictionary<string, string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
            Status = status;
            BaseHitRoll = hitRoll;
            BaseDamRoll = damRoll;
            Level = level;
            Age = age;
            RespawnRoom = respawnRoom;
            BaseStrength = strength;
            BaseDexterity = dexterity;
            BaseConstitution = constitution;
            BaseIntelligence = intelligence;
            BaseWisdom = wisdom;
            BaseCharisma = charisma;
            BaseLuck = luck;
        }

        public Player()
        {
            IsAdmin = false;
            BaseCharisma = StatResolver.RollStat();
            BaseConstitution = StatResolver.RollStat();
            BaseDexterity = StatResolver.RollStat();
            BaseIntelligence = StatResolver.RollStat();
            BaseLuck = StatResolver.RollStat();            
            BaseStrength = StatResolver.RollStat();
            BaseWisdom = StatResolver.RollStat();
            BaseDamRoll = 1;
            BaseHitRoll = 1;
            BaseHp = StatResolver.RollHitPoints();
            _hitPoints = BaseHp;
            Inventory = new Dictionary<string, string>();
            Equipped = new Dictionary<Wearlocation, WearSlot>();
        }

        public string Key
        {
            get { return Forename.ToLower(); }
        }

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
        public Dictionary<string, string> RememberedNames { get; private set; }
        public string Forename { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public PlayerGender Gender { get; set; }
        public bool Approved { get; set; }
        public bool IsAdmin { get; set; }
        public string Prompt { get; set; }
        public Dictionary<string, string> Inventory { get; private set; }
        public long Gold { get; set; }
        public Dictionary<Wearlocation, WearSlot> Equipped { get; private set; }
        public GameStatus Status { get; set; }
        public int Level { get; set; }
        public int Age { get; set; }
        public string RespawnRoom { get; set; }

        [JsonIgnore]
        public OutputTextWriter OutputWriter { get; set; }

        public int HitPoints
        {
            get { return _hitPoints; }
            set { _hitPoints = value; }
        }

        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = Hash(value); }
        }
        
        public int Experience
        {
            get { return _experience; }
            set
            {
                if (Level == ExperienceResolver.Levels)
                    return;

                _experience += value;
                if (ExperienceResolver.CanLevelUp(Level, _experience))
                {
                    Level++;
                    if (OutputWriter != null)
                        Send(string.Format("You gained enough experience to advance to level {0}", Level), null);
                }
            }
        }

        [JsonIgnore]
        public int MaxHitPoints
        {
            get { return BaseHp + Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).HpBonus); }
        }
        
        [JsonIgnore]
        public int HitRoll
        {
            get
            {
                return BaseHitRoll +
                     Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).HitRoll);
            }
        }
        
        [JsonIgnore]
        public int DamRoll
        {
            get
            {
                return BaseDamRoll +
                   Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).DamRoll);
            }
        }
        
        [JsonIgnore]
        public int Strength
        {
            get
            {
                return BaseStrength +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).StrengthBonus);
            }
        }

        [JsonIgnore]
        public int Dexterity
        {
            get
            {
                return BaseDexterity +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).DexterityBonus);
            }
        }

        [JsonIgnore]
        public int Constitution
        {
            get
            {
                return BaseConstitution +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).ConstitutionBonus);
            }
        }

        [JsonIgnore]
        public int Intelligence
        {
            get
            {
                return BaseIntelligence +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).IntelligenceBonus);
            }
        }

        [JsonIgnore]
        public int Wisdom
        {
            get
            {
                return BaseWisdom +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).WisdomBonus);
            }
        }

        [JsonIgnore]
        public int Charisma
        {
            get
            {
                return BaseCharisma +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).CharismaBonus);
            }
        }

        [JsonIgnore]
        public int Luck
        {
            get
            {
                return BaseLuck +
                       Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).LuckBonus);
            }
        }

        [JsonIgnore]
        public int Weight
        {
            get
            {
                _weight = 0;

                foreach (var key in Inventory.Keys.Union(Equipped.Values.Select(w => w.Key)))
                {
                    var item = Server.Current.Database.Get<PlayerItem>(key);
                    if (item != null)
                    {
                        if (item.WearLocation == Wearlocation.Container)
                            _weight += item.ContainerWeight;
                        else
                            _weight += item.Weight;
                    }
                }

                return _weight;
            }
        }

        [JsonIgnore]
        public int Armor
        {
            get { return Equipped.Sum(e => Server.Current.Database.Get<PlayerItem>(e.Value.Key).ArmorBonus); }
        }

        [JsonIgnore]
        public int MaxInventory
        {
            get { return Strength <= 10 ? 10 : Strength + 2; }
        }

        [JsonIgnore]
        public int MaxWeight
        {
            get { return Strength <= 10 ? 100 : (Strength*50) - 400; }
        }

        private static string Hash(string value)
        {
            using (SHA256 hashMethod = SHA256.Create())
            {
                var toHash = Encoding.ASCII.GetBytes(value);
                var hashedBytes = hashMethod.ComputeHash(toHash);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool CheckPassword(string password)
        {
            var hash = Hash(password);

            return PasswordHash == hash;
        }

        // todo bug#30 add ability to change password

        public string GetOtherPlayerDescription(Player subject)
        {
            if (subject == this)
                return this.Forename;

            if (RememberedNames.ContainsKey(subject.Key))
            {
                return RememberedNames[subject.Key];
            }

            return subject.ShortDescription;
        }

        public void Send(string format, Player subject)
        {
            Send(format, subject, null);
        }

        public void Send(string format, Player subject, Player target)
        {
            OutputWriter.WriteLine(StringHelpers.BuildString(format, this, subject, target));
            WritePrompt();
        }

        public void WritePrompt()
        {
            OutputWriter.WriteLine("<{0}/{1}hp ${2:N0}>", HitPoints, MaxHitPoints, Gold);
        }

        public static string NameToKey(string name)
        {
            return name.ToLower();
        }

        public CombatRound Hit(NonPlayer mob)
        {
            var round = new CombatRound();

            if (Server.Current.Random.Next(HitRoll) + 1 >= mob.Armor)
            {
                // hit
                var damage = Server.Current.Random.Next(DamRoll) + 1;
                mob.HitPoints -= damage;

                // apply experience
                ExperienceResolver.ApplyExperience(this, damage);

                // player text
                var playerText = string.Format("You hit {0} for {1} damage!\n", mob.Name, damage);
                round.AddText(this, playerText, CombatTextType.Player);

                // group text (excludes player)
                var groupText = string.Format("{0} hits {1}!\n", Forename, mob.Name);
                round.AddText(this, groupText, CombatTextType.Group);
            }
            else
            {
                var playerText = string.Format("You miss {0}!\n", mob.Name);
                round.AddText(this, playerText, CombatTextType.Player);

                var groupText = string.Format("{0} hits {1}!\n", Forename, mob.Name);
                round.AddText(this, groupText, CombatTextType.Group);
            }

            // finally set some generic room text
            round.AddText(null, string.Format("{0} is fighting {1}!\n", Forename, mob.Name), CombatTextType.Room);

            return round;
        }

        public void Die()
        {
            if (HitPoints >= Server.DeadHitPoints)
            {
                Status = HitPoints >= Server.IncapacitatedHitPoints
                             ? GameStatus.Incapacitated
                             : GameStatus.MortallyWounded;
            }
            else
            {
                Status = GameStatus.Dead;
            }
        }

        public void DieForReal()
        {
            Status = GameStatus.Sitting;
            HitPoints = 1;
            
            var deathRoom = RoomHelper.GetPlayerRoom(Location);
            deathRoom.RemovePlayer(this);

            // create corpse
            var corpse = Server.Current.Database.Get<Template>("corpse");
            var dupedCorpse = Mapper.Map<PlayerItem>(corpse);
            var corpseName = string.Format("The corpse of {0}", Forename);
            dupedCorpse.AllowedToLoot = Key; // this should be the only place this is used
            dupedCorpse.Name = corpseName;
            dupedCorpse.Description = corpseName;
            dupedCorpse.Keywords = new [] {"corpse", Forename};
            dupedCorpse.WearLocation = Wearlocation.Corpse;
            Console.WriteLine("NEW CORPSE: {0}", dupedCorpse.Key);
            deathRoom.CorpseQueue[dupedCorpse.Key] = DateTime.Now.AddMilliseconds(Server.CorpseDecayTime);

            // copy player items to corpse
            foreach (var item in Inventory
                .Select(x => new {Key = x.Key, Value = x.Value})
                .Union(Equipped.Values.Select(x => new {Key = x.Key, Value = x.Name})
                .ToArray()))
            {
                dupedCorpse.ContainedItems.Add(item.Key, item.Value);
            }

            // clear inventory/equipped
            Inventory.Clear();
            Equipped.Clear();

            // cache, don't save
            Server.Current.Database.Put(dupedCorpse);
            deathRoom.AddItem(dupedCorpse);
            
            var room = RoomHelper.GetPlayerRoom(RespawnRoom);
            Location = RespawnRoom;
            room.AddPlayer(this);
            
            Server.Current.Database.Save(this);
        }

        public static string GetStatusText(GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Dead:
                    return "Dead";
                case GameStatus.Fighting:
                    return "Fighting";
                case GameStatus.Incapacitated:
                    return "Incapacitated";
                case GameStatus.MortallyWounded:
                    return "Mortally wounded";
                case GameStatus.Sitting:
                    return "Sitting";
                case GameStatus.Sleeping:
                    return "Sleeping";
                case GameStatus.Standing:
                    return "Standing";
                default:
                    return string.Empty;
            }
        }
    }
}
