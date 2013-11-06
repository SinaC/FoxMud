using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FoxMud.Game
{
    class AgeHandler
    {
        private readonly Timer _timer;

        public AgeHandler(long tickDelay)
        {
            _timer = new Timer(tickDelay);
            _timer.Elapsed += Grow;
        }

        private void Grow(object sender, ElapsedEventArgs e)
        {
            foreach (var player in Server.Current.Database.GetAll<Player>().Where(p => p.LoggedIn))
                player.Grow();
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
