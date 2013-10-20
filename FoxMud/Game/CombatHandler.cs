using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FoxMud.Game.World;

namespace FoxMud.Game
{
    class Combat
    {
        private readonly List<Player> fighters = new List<Player>();
        private readonly List<NonPlayer> mobs = new List<NonPlayer>();
        private bool isAggro;

        public void AddFighter(Player player)
        {
            fighters.Add(player);
        }

        public void AddMob(NonPlayer npc)
        {
            mobs.Add(npc);
        }

        internal void Start()
        {
            if (fighters.Count < 1 || mobs.Count < 1)
                throw new Exception("Cannot start combat. 1 Player, 1 NonPlayer required.");

            foreach (var fighter in fighters)
            {
                fighter.Status = GameStatus.Fighting;
            }

            foreach (var mob in mobs)
            {
                // if at least one mob is aggro, the whole fight is 'aggro' i.e. mob gets first hit each round
                if (mob.Aggro)
                    isAggro = true;

                mob.Status = GameStatus.Fighting;
            }
        }

        internal void Round()
        {
            string roundText = string.Empty;

            if (isAggro)
            {
                // mob hits first
                foreach (var npc in mobs)
                {
                    // choose player to hit
                    var playerToHit = fighters.OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                    // roll to hit, if success, then hit
                    if (Server.Current.Random.Next(npc.HitRoll) > playerToHit.Armor)
                    {
                        var damage = Server.Current.Random.Next(npc.DamRoll) + 1;

                    }
                    else
                    {
                        roundText += string.Format("{0} swung and missed {1}.", npc.Name, playerToHit.Forename);
                    }
                }

                // if still players, they hit
            }
            else
            {
                // player hits first

                // if still mobs, they hit
            }
        }
    }

    /// <summary>
    /// main handler of combat
    /// </summary>
    class CombatHandler
    {
        private List<Combat> Fights { get; set; }
        private Timer _timer { get; set; }

        public CombatHandler(long combatTickRate)
        {
            Fights = new List<Combat>();
            _timer = new Timer(combatTickRate);
            _timer.Elapsed += DoCombat;
        }

        public void StartFight(Combat combat)
        {
            combat.Start();
        }

        public void EnterRoom(Player player, Room room)
        {
            // if player is first in room, check npc's for aggro
            if (!room.GetPlayers().Any())
            {
                foreach (var npc in room.GetNpcs())
                {
                    if (npc.Aggro)
                    {
                        var combat = new Combat();
                        combat.AddFighter(player);
                        combat.AddMob(npc);
                        StartFight(combat);
                        break; // this will only handle one aggro npc in the room at a time
                    }
                }
            }
        }

        private void DoCombat(object sender, ElapsedEventArgs e)
        {
            foreach (var combat in Fights)
            {
                combat.Round();
            }
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}

