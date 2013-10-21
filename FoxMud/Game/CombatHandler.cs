using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using FoxMud.Game.World;

namespace FoxMud.Game
{
    class CombatRound
    {
        public Dictionary<Player, string> PlayerText { get; set; }
        public string RoomText { get; set; }

        public CombatRound()
        {
            PlayerText = new Dictionary<Player, string>();
        }

        public static CombatRound operator +(CombatRound r1, CombatRound r2)
        {
            foreach (var item in r2.PlayerText)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    if (r1.PlayerText.ContainsKey(item.Key))
                        r1.PlayerText[item.Key] += item.Value;
                    else
                        r1.PlayerText[item.Key] = item.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(r2.RoomText))
                r1.RoomText += r2.RoomText;

            return r1;
        }
    }

    class Combat
    {
        private readonly List<Player> fighters = new List<Player>();
        private readonly List<NonPlayer> mobs = new List<NonPlayer>();
        private bool isAggro;
        private Room room;
        private Dictionary<string, string> killingBlows = new Dictionary<string, string>();

        public bool Fighting
        {
            get { return fighters.Count > 0 && mobs.Count > 0; }
        }

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

        internal void Round(long combatTickRate)
        {
            var roundText = new CombatRound();

            if (isAggro)
            {
                roundText += DoMobHits();

                roundText += HandleDeadPlayers();

                roundText += DoPlayerHits();

                roundText += HandleDeadMobs();
            }
            else
            {
                roundText += DoPlayerHits();

                roundText += HandleDeadMobs();

                roundText += DoMobHits();

                roundText += HandleDeadPlayers();
            }

            // send text to each player
            foreach (var player in roundText.PlayerText.Keys)
                player.Send(roundText.PlayerText[player], player);

            // send text to room
            room.SendPlayers(roundText.RoomText, null, null, roundText.PlayerText.Keys.ToArray());

            Thread.Sleep((int) combatTickRate);
        }

        internal void End()
        {
            foreach (var fighter in fighters)
                fighter.Status = GameStatus.Standing;

            foreach (var mob in mobs)
                mob.Status = GameStatus.Standing;
        }

        private CombatRound DoMobHits()
        {
            //Console.WriteLine("Enter DoMobHits");
            var round = new CombatRound();

            try
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

                        round += npc.Hit(playerToHit);

                        if (playerToHit.HitPoints <= 0)
                        {
                            round.PlayerText[playerToHit] += "You are DEAD!!!\n";
                            round.RoomText += string.Format("{0} is DEAD!!!\n", playerToHit.Forename);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return round;
        }

        private CombatRound HandleDeadPlayers()
        {
            //Console.WriteLine("Enter HandleDeadPlayers");
            var round = new CombatRound();

            try
            {
                // look for dead players
                foreach (var player in fighters.Where(p => p.HitPoints <= 0).ToArray())
                {
                    // kill player
                    fighters.Remove(player);
                    round += player.Die();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return round;
        }

        private CombatRound DoPlayerHits()
        {
            //Console.WriteLine("Enter DoPlayerHits");
            var round = new CombatRound();

            try
            {
                // if still players, they hit
                foreach (var player in fighters)
                {
                    if (mobs.Any(m => m.HitPoints > 0))
                    {
                        var mobToHit = mobs
                            .Where(m => m.HitPoints > 0)
                            .OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                        round += player.Hit(mobToHit);

                        // check for killing blow
                        if (mobToHit.HitPoints <= 0)
                        {
                            round.PlayerText[player] += string.Format("You killed {0}!!!\n", mobToHit.Name);
                            round.RoomText += string.Format("{0} killed {1}!\n", player.Forename, mobToHit.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return round;
        }

        private CombatRound HandleDeadMobs()
        {
            //Console.WriteLine("Enter HandleDeadMobs");
            var round = new CombatRound();

            try
            {
                // look for dead mobs
                foreach (var mob in mobs.Where(m => m.HitPoints <= 0).ToArray())
                {
                    // kill mob
                    mobs.Remove(mob);
                    round += mob.Die();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return round;
        }
    }

    /// <summary>
    /// main handler of combat
    /// </summary>
    class CombatHandler
    {
        private List<Combat> Fights { get; set; }
        private System.Timers.Timer _timer { get; set; }
        private long _combatTickRate;

        public CombatHandler(long combatTickRate)
        {
            Fights = new List<Combat>();
            _combatTickRate = combatTickRate;
            _timer = new System.Timers.Timer(_combatTickRate);
            _timer.Elapsed += DoCombat;
        }

        public void StartFight(Combat combat)
        {
            Fights.Add(combat);
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
            _timer.Stop();
            foreach (var combat in Fights.ToArray())
            {
                try
                {
                    combat.Round(_combatTickRate);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // end fight
                if (!combat.Fighting)
                {
                    combat.End();
                    Fights.Remove(combat);
                }
            }
            _timer.Start();
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}

