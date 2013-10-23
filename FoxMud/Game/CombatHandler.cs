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
    enum CombatTextType
    {
        Player,
        Group,
        KillingBlow,
        Room
    }

    class CombatRound
    {
        private Dictionary<Player, string> playerText { get; set; }
        private Dictionary<Player, string> complementGroupText { get; set; }
        private Dictionary<Player, string> killingBlowText { get; set; }
        private string roomText { get; set; }
        private LinkedList<Player> combatOrder;

        public CombatRound()
        {
            playerText = new Dictionary<Player, string>();
            complementGroupText = new Dictionary<Player, string>();
            killingBlowText = new Dictionary<Player, string>();
            combatOrder = new LinkedList<Player>();
        }

        public Player[] PlayerKeys()
        {
            return playerText.Keys.ToArray();
        }

        public Player[] GroupKeys()
        {
            return complementGroupText.Keys.ToArray();
        }

        public Player[] KillingBlowKeys()
        {
            return killingBlowText.Keys.ToArray();
        }

        public string GetRoomText()
        {
            return roomText;
        }

        public void AddText(Player player, string text, CombatTextType type)
        {
            if (!combatOrder.Contains(player))
                combatOrder.AddLast(player);

            switch (type)
            {
                case CombatTextType.Player:
                    if (playerText.ContainsKey(player))
                        playerText[player] += text;
                    else
                        playerText.Add(player, text);
                    break;
                case CombatTextType.Group:
                    if (complementGroupText.ContainsKey(player))
                        complementGroupText[player] += text;
                    else
                        complementGroupText.Add(player, text);
                    break;
                case CombatTextType.KillingBlow:
                    if (killingBlowText.ContainsKey(player))
                        killingBlowText[player] += text;
                    else
                        killingBlowText.Add(player, text);
                    break;
                case CombatTextType.Room:
                    roomText += text;
                    break;
            }
        }

        public Dictionary<Player, string> Print()
        {
            var result = new Dictionary<Player, string>();

            // add text in order

            return result;
        }

        public static CombatRound operator +(CombatRound r1, CombatRound r2)
        {
            foreach (var player in r2.playerText)
            {
                if (!string.IsNullOrWhiteSpace(player.Value))
                {
                    if (r1.playerText.ContainsKey(player.Key))
                        r1.playerText[player.Key] += player.Value;
                    else
                        r1.playerText[player.Key] = player.Value;
                }
            }

            foreach (var player in r2.complementGroupText)
            {
                if (!string.IsNullOrWhiteSpace(player.Value))
                {
                    if (r1.complementGroupText.ContainsKey(player.Key))
                        r1.complementGroupText[player.Key] += player.Value;
                    else
                        r1.complementGroupText[player.Key] = player.Value;
                }
            }

            foreach (var player in r2.killingBlowText)
            {
                if (!string.IsNullOrWhiteSpace(player.Value))
                {
                    if (r1.killingBlowText.ContainsKey(player.Key))
                        r1.killingBlowText[player.Key] += player.Value;
                    else
                        r1.killingBlowText[player.Key] = player.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(r2.roomText))
                r1.roomText += r2.roomText;

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

        public IEnumerable<Player> GetFighters()
        {
            return fighters.AsEnumerable();
        }

        public IEnumerable<NonPlayer> GetMobs()
        {
            return mobs.AsEnumerable();
        }

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

            var textToSend = roundText.Print();

            //// send text to each player
            //foreach (var player in roundText.playerText.Keys)
            //    textToSend[player] = roundText.playerText[player];

            //// send group text exclusively to other fighters
            //foreach (var excludedPlayer in roundText.complementGroupText.Keys)
            //{
            //    var name = excludedPlayer.Forename;
            //    foreach (var player in fighters.Where(f => f.Forename != name))
            //    {
            //        if (textToSend.ContainsKey(player))
            //            textToSend[player] += roundText.complementGroupText[excludedPlayer];
            //        else
            //            textToSend[player] = roundText.complementGroupText[excludedPlayer];
            //    }
            //}

            foreach (var player in textToSend.Keys)
                player.Send(textToSend[player], null);

            // send text to room
            //room.SendPlayers(roundText.RoomText, null, null, roundText.PlayerText.Keys.ToArray());
            room.SendPlayers(roundText.GetRoomText(), null, null, fighters.ToArray());

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
                            round.AddText(playerToHit, "You are DEAD!!!\n", CombatTextType.Player);
                            round.AddText(null, string.Format("{0} is DEAD!!!\n", playerToHit.Forename), CombatTextType.Room);
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
                var killedBy = new Dictionary<NonPlayer, Player>();

                // if still players, they hit
                foreach (var player in fighters)
                {
                    if (mobs.Any(m => m.HitPoints > 0))
                    {
                        var mobToHit = mobs
                            .Where(m => m.HitPoints > 0)
                            .OrderBy(x => Guid.NewGuid()).FirstOrDefault();

                        round += player.Hit(mobToHit);

                        // check for killing blow, could be multiple
                        if (mobToHit.HitPoints <= 0)
                        {
                            killedBy.Add(mobToHit, player);
                            // remove mob from combat, so it can't be hit any more
                            mobs.Remove(mobToHit);
                            if (mobs.Count == 0)
                                break;
                        }
                    }
                }

                if (killedBy.Count > 0)
                {
                    foreach (var kb in killedBy)
                    {
                        var groupText = string.Format("{0} is DEAD!!!\n", kb.Key.Name);
                        round.AddText(kb.Value, groupText, CombatTextType.KillingBlow);
                        round.AddText(null, string.Format("{0} killed {1}!\n", kb.Value.Forename, kb.Key.Name), CombatTextType.KillingBlow);
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

        public void AddToCombat(Player player, string mobKey)
        {
            foreach (var fight in Fights)
                if (fight.GetMobs().Any(m => m.Key == mobKey))
                    fight.AddFighter(player);
        }
    }
}

