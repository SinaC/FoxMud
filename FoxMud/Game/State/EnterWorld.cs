using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

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
            Session.Player = this.character;
            character.OutputWriter = Session;

            var room = Server.Current.Database.Get<Room>(character.Location);

            if (room != null)
                room.AddPlayer(character);

            base.OnStateInitialize();
        }

        public override void OnStateEnter()
        {
            Session.Write("\f");
            Session.PushState(new PlayingState());
            var room = Server.Current.Database.Get<Room>(Session.Player.Location);

            if (room != null)
            {
                room.SendPlayers("%d arrives.\n", Session.Player, null, Session.Player);
            }

            base.OnStateEnter();
        }
    }
}
