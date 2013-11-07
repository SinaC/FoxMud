using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FoxMud.Game.World;

namespace FoxMud.Game
{
    class RegenHandler
    {
        private readonly Timer _timer;

        public RegenHandler(long tickTime)
        {
            _timer = new Timer(tickTime);
            _timer.Elapsed += DoRegen;
        }

        private void DoRegen(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            foreach (var player in Server.Current.Database.GetAll<Player>())
            {
                if (player.HitPoints != player.MaxHitPoints)
                {
                    // based on con, regenerate or lose some hp
                    if (player.HitPoints < Server.IncapacitatedHitPoints)
                    {
                        player.HitPoints -= 1;

                        if (player.HitPoints < Server.DeadHitPoints)
                        {
                            player.DieForReal();
                            continue;
                        }

                        player.Send("You will die soon if unaided...", null);
                    }
                    else if (player.LoggedIn)
                    {
                        var hpToGain = deltaHp(player);
                        if (player.HitPoints + hpToGain > player.MaxHitPoints)
                            player.HitPoints = player.MaxHitPoints;
                        else
                            player.HitPoints += hpToGain;
                    }
                }
            }

            foreach (var npc in Server.Current.Database.GetAll<NonPlayer>())
            {
                if (npc.HitPoints != npc.MaxHitPoints)
                {
                    var hpToGain = deltaHp(npc);
                    if (npc.HitPoints + hpToGain > npc.MaxHitPoints)
                        npc.HitPoints = npc.MaxHitPoints;
                    else
                        npc.HitPoints += hpToGain;
                }
            }
            _timer.Start();
        }

        private int deltaHp(Player player)
        {
            int hp;

            if (player.Constitution > 20)
                hp = Convert.ToInt32(player.MaxHitPoints * 0.10);
            else if (player.Constitution > 15)
                hp = Convert.ToInt32(player.MaxHitPoints * 0.08);
            else
                hp = Convert.ToInt32(player.MaxHitPoints * 0.05);

            // sit/sleep bonus
            if (player.Status == GameStatus.Sitting)
                hp += Convert.ToInt32(hp*0.03);
            if (player.Status == GameStatus.Sleeping)
                hp += Convert.ToInt32(hp*0.10);

            return hp;
        }

        private int deltaHp(NonPlayer npc)
        {
            int hp;

            if (npc.Constitution > 20)
                hp = Convert.ToInt32(npc.MaxHitPoints * 0.08);
            else if (npc.Constitution > 15)
                hp = Convert.ToInt32(npc.MaxHitPoints * 0.05);
            else
                hp = Convert.ToInt32(npc.MaxHitPoints * 0.03);

            return hp;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
