using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FoxMud.Game.World
{
    class RepopHandler
    {
        private long _repopTate;
        private Timer _timer;

        public RepopHandler(long repopRate)
        {
            _repopTate = repopRate;
            _timer = new Timer(_repopTate);
            _timer.Elapsed += DoRepop();
        }

        private ElapsedEventHandler DoRepop()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}
