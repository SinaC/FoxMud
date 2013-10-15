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
        /// populate all rooms in all areas, etc
        /// </summary>
        private void mudBoot()
        {
            // get all MobTemplates
            foreach (var mob in Server.Current.Database.GetAll<MobTemplate>())
            {
                // copy into NonPlayer
                NonPlayer npc = Mapper.Map<NonPlayer>(mob);

                // generate inventory and equipped


                // get room, put in room
            }
        }

        private void DoRepop(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}
