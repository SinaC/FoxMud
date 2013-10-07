using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud
{
    class SessionStateBase
    {
        protected internal Session Session { get; internal set; }

        public virtual void OnInput(string input)
        {
        }

        public virtual void OnStateInitialize()
        {
        }

        public virtual void OnStateEnter()
        {
        }

        public virtual void OnStateLeave()
        {
        }

        public virtual void OnStateShutdown()
        {
        }
    }
}
