using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AutoMapper;

namespace FoxMud.Game.World
{
    class RepopHandler
    {
        private long _repopRate;
        private Timer _timer;

        public RepopHandler(long repopRate)
        {
            mudBoot(); // initially, spawn everything according to usual rules
            _repopRate = repopRate;
            _timer = new Timer(_repopRate);
            _timer.Elapsed += DoRepop;
        }

        /// <summary>
        /// initially, spawn all mobs
        /// </summary>
        private void mudBoot()
        {
            // get all MobTemplates
            foreach (var mob in Server.Current.Database.GetAll<MobTemplate>())
            {
                // copy into NonPlayer, mapping generates inventory and equipped
                var npc = Mapper.Map<NonPlayer>(mob);
                npc.MobTemplateKey = mob.Key;
                Server.Current.Database.Save(npc); // do we even need to persist NonPlayer objects, or can they be managed in memory?

                // get room, put in room
                RoomHelper.GetPlayerRoom(npc.RespawnRoom).AddNpc(npc);
            }

            foreach (var area in Server.Current.Areas)
                area.LastRepop = DateTime.Now;
        }

        private void DoRepop(object sender, ElapsedEventArgs e)
        {
            foreach (var area in Server.Current.Areas)
            {
                if ((DateTime.Now - area.LastRepop).TotalMilliseconds > area.RepopTime)
                {
                    try
                    {
                        _timer.Stop();
                        // broadcast repop message to players in area
                        foreach (var roomKey in area.Rooms)
                            foreach (var player in RoomHelper.GetPlayerRoom(roomKey).GetPlayers())
                                player.Send(area.RepopMessage, null);

                        foreach (var key in area.RepopQueue.ToArray())
                        {
                            var mob = Server.Current.Database.Get<MobTemplate>(key);
                            var npc = Mapper.Map<NonPlayer>(mob);
                            RoomHelper.GetPlayerRoom(npc.RespawnRoom).AddNpc(npc);
                            area.RepopQueue.Remove(key);
                        }

                        area.LastRepop = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("DoRepop error: {0}", ex.StackTrace);
                    }
                    finally
                    {
                        _timer.Start();
                    }
                }
            }
        }

        public void Start()
        {
            _timer.Enabled = true;
            _timer.Start();
        }
    }
}
