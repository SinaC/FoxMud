using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.State
{
    class EnterWorld : SessionStateBase
    {
        private readonly Player character;

        public EnterWorld(Player character)
        {
            this.character = character;
        }

        public override void OnStateInitialize()
        {
            base.OnStateInitialize();
        }

        public override void OnStateEnter()
        {
            base.OnStateEnter();
        }
    }
}
