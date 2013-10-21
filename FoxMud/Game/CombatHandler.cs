using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FoxMud.Game.World;

namespace FoxMud.Game
{
    class CombatRound
    {
        public string RoundText { get; set; }
        public string RoomText { get; set; }
        public Room Room { get; set; }
    }

    class Combat
    {
        private readonly List<Player> fighters = new List<Player>();
        private readonly List<NonPlayer> mobs = new List<NonPlayer>();
        private bool isAggro;
        private Room room;

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
                if (room == null)
                    room = RoomHelper.GetPlayerRoom(fighter.Location);

                fighter.Status = GameStatus.Fighting;
            }

            foreach (var mob in mobs)
            {
                if (room == null)
                    room = RoomHelper.GetPlayerRoom(mob.Location);

                // if at least one mob is aggro, the whole fight is 'aggro' i.e. mob gets first hit each round
                if (mob.Aggro)
                    isAggro = true;

                mob.Status = GameStatus.Fighting;
            }
        }

        internal void Round()
        {
            Queue<CombatRound> roundText = new Queue<CombatRound>();

            if (isAggro)
            {
                DoMobHits(roundText);

                HandleDeadPlayers();

                DoPlayerHits(roundText);

                HandleDeadMobs();
            }
            else
            {
                DoPlayerHits(roundText);

                HandleDeadMobs();

                DoMobHits(roundText);

                HandleDeadPlayers();
            }

            while (roundText.Peek() != null)
            {
                var combatRound = roundText.Dequeue();
                
            }
        }

        private void DoMobHits(Queue<CombatRound> roundText)
        {
            // mob hits first
            foreach (var npc in mobs)
            {
                // only attempt to hit if there are players left to hit
                if (fighters.Any(p => p.HitPoints > 0))
                {
                    // choose player to hit at random, and hit
                    var playerToHit = fighters
                        .Where(p => p.HitPoints > 0)
                        .OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    
                    npc.Hit(playerToHit);
                }
            }
        }

        private void HandleDeadPlayers()
        {
            // look for dead players
            foreach (var player in fighters.Where(p => p.HitPoints < 0).ToArray())
            {
                // kill player
                fighters.Remove(player);
                player.Die();
            }
        }

        private void DoPlayerHits(Queue<CombatRound> roundText)
        {
            // if still players, they hit
            foreach (var player in fighters)
            {
                if (mobs.Any(m => m.HitPoints > 0))
                {
                    var mobToHit = mobs
                        .Where(m => m.HitPoints > 0)
                        .OrderBy(x => Guid.NewGuid()).FirstOrDefault();
                    roundText.Enqueue(player.Hit(mobToHit));
                }
            }
        }

        private void HandleDeadMobs()
        {
            // look for dead mobs
            foreach (var mob in mobs.Where(m => m.HitPoints < 0).ToArray())
            {
                // kill mob
                mobs.Remove(mob);
                mob.Die();
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

