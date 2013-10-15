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
        List<Player> fighters;
        List<MobTemplate> mobs;

        public Combat()
        {
            // change player status? or will this be handled by calling xode?
        }
    }

    /// <summary>
    /// main handler of combat
    /// </summary>
    class CombatHandler
    {
        public List<Combat> fights { get; set; }
        private Timer _timer { get; set; }

        public CombatHandler(long tickRate)
        {
            fights = new List<Combat>();
            _timer = new Timer(tickRate);
            _timer.Elapsed += DoCombat;
        }

        private void DoCombat(object sender, ElapsedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            _timer.Start();
        }
    }
}

