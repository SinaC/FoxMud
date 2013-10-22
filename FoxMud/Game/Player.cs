using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        Dead,
        Frozen,
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

        [JsonConstructor]
        private Player(
            string name, string passwordHash, bool isAdmin, string prompt, Dictionary<string,string> rememberedNames, Dictionary<string, string> inventory, int hitPoints,
            int maxHitPoints, long gold, int experience, int baseDamage, int baseArmor, int maxInventory, int maxWeight, Dictionary<Wearlocation, WearSlot> equipped,
            GameStatus status, int hitRoll, int damRoll, int level)
        {
            Forename = name;
            _passwordHash = passwordHash;
            IsAdmin = isAdmin;
            Prompt = prompt;
            HitPoints = hitPoints;
            MaxHitPoints = maxHitPoints;
            Gold = gold;
            Experience = experience;
            BaseDamage = baseDamage;
            BaseArmor = baseArmor;
            MaxInventory = maxInventory;
            MaxWeight = maxWeight;
            RememberedNames = rememberedNames ?? new Dictionary<string, string>();
            Inventory = inventory ?? new Dictionary<string, string>();
            Equipped = equipped ?? new Dictionary<Wearlocation, WearSlot>();
            Status = status;
            HitRoll = hitRoll;
            DamRoll = damRoll;
            Level = level;
        }

        public Player()
        {
            IsAdmin = false;
        }

        public string Key
        {
            get { return Forename.ToLower(); }
        }

        [JsonIgnore]
        public OutputTextWriter OutputWriter { get; set; }

        public Dictionary<string, string> RememberedNames { get; private set; }
        public string Forename { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public PlayerGender Gender { get; set; }
        public bool Approved { get; set; }
        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = Hash(value); }
        }
        public bool IsAdmin { get; set; }
        public string Prompt { get; set; }
        public Dictionary<string, string> Inventory { get; private set; }
        public int HitPoints { get; set; }
        public int MaxHitPoints { get; set; }
        public long Gold { get; set; }
        public int Experience { get; set; }
        public int BaseArmor { get; set; }
        public int BaseDamage { get; set; }
        public int MaxInventory { get; set; }
        public int MaxWeight { get; set; }
        public Dictionary<Wearlocation, WearSlot> Equipped { get; private set; }
        public GameStatus Status { get; set; }
        public int HitRoll { get; set; }
        public int DamRoll { get; set; }
        public int Level { get; set; }

        [JsonIgnore]
        public int Weight
        {
            get
            {
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
        public int Damage
        {
            get
            {
                return BaseDamage;
            }
        }

        [JsonIgnore]
        public int Armor
        {
            get
            {
                return BaseArmor;
            }
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

                // player text
                var playerText = string.Format("You hit {0} for {1} damage!\n", mob.Name, damage);
                if (round.PlayerText.ContainsKey(this))
                    round.PlayerText[this] += playerText;
                else
                    round.PlayerText.Add(this, playerText);

                // group text (excludes player)
                var groupText = string.Format("{0} hits {1}!\n", Forename, mob.Name);
                if (round.ComplementGroupText.ContainsKey(this))
                    round.ComplementGroupText[this] += groupText;
                else
                    round.ComplementGroupText.Add(this, groupText);
            }
            else
            {
                var playerText = string.Format("You miss {0}!\n", mob.Name);
                if (round.PlayerText.ContainsKey(this))
                    round.PlayerText[this] += playerText;
                else
                    round.PlayerText.Add(this, playerText);

                var groupText = string.Format("{0} hits {1}!\n", Forename, mob.Name);
                if (round.ComplementGroupText.ContainsKey(this))
                    round.ComplementGroupText[this] += groupText;
                else
                    round.ComplementGroupText.Add(this, groupText);
            }

            // finally set some generic room text
            round.RoomText += string.Format("{0} is fighting {1}!\n", Forename, mob.Name);

            return round;
        }

        public CombatRound Die()
        {
            var round = new CombatRound();

            round.PlayerText[this] += "You are DEAD!!!\n";
            round.RoomText += string.Format("{0} is DEAD!!!\n", Forename);
            HitPoints = 100;
            round.PlayerText[this] += "Hit points temporarily restored...\n";
            Status = GameStatus.Dead;

            return round;
        }
    }
}
