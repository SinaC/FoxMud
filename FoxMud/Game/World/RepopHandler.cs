using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AutoMapper;
using FoxMud.Game.Item;

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
            _timer.Stop();

            foreach (var area in Server.Current.Areas)
            {
                if ((DateTime.Now - area.LastRepop).TotalMilliseconds > area.RepopTime)
                {
                    try
                    {
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
                        var log = string.Format("Respawn Error: {0}", ex.StackTrace);
                        Console.WriteLine(log);
                        Server.Current.Log(LogType.Error, log);
                    }
                }

                // handle corpses
                foreach (var key in area.Rooms)
                {
                    var room = Server.Current.Database.Get<Room>(key);
                    foreach (var corpseKey in room.CorpseQueue.Where(k => k.Value < DateTime.Now))
                    {
                        var corpse = Server.Current.Database.Get<PlayerItem>(corpseKey.Key);
                        
                        // delete the corpse and all items in it
                        foreach (var corpseItemKey in corpse.ContainedItems.Keys)
                            Server.Current.Database.Delete<PlayerItem>(corpseItemKey);

                        room.SendPlayers(string.Format("{0} withers and blows away...", corpse.Name), null, null, null);
                    }
                }
            }

            // fixme: this is going to get dead/old npc's as well
            foreach (var npc in Server.Current.Database.GetAll<NonPlayer>())
            {
                npc.TalkOrWalk();
            }

            _timer.Start();
        }

        public void Start()
        {
            _timer.Enabled = true;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
