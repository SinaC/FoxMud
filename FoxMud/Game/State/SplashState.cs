using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.State
{
    class SplashState : SessionStateBase
    {
        private string splash;

        public override void OnStateInitialize()
        {
            string splashPath =
                Path.Combine(
                    Server.DataDir,
                    "splash.txt");

            splash = File.ReadAllText(splashPath);

            base.OnStateInitialize();
        }

        public override void OnStateEnter()
        {
            Session.WriteLine(splash);
            Session.PushState(new PlayerLogin());
            base.OnStateEnter();
        }
    }
}
