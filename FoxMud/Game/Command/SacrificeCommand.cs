using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command
{
    [Command("sacrifice", false, TickDelay.Single, 1)]
    class SacrificeCommand : PlayerCommand
    {
        public SacrificeCommand() { }

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: sacrifice corpse");
            session.WriteLine("Syntax: sac <item>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var room = RoomHelper.GetPlayerRoom(session.Player);

            // find item
            var item = ItemHelper.FindFloorItem(room, context.ArgumentString);

            if (item == null)
            {
                session.WriteLine("You couldn't find that.");
                return;
            }

            // remove from room
            room.RemoveItem(item);

            // notify player/room
            session.WriteLine("You sacrifice {0}.", item.Name);
            room.SendPlayers(string.Format("{0} sacrifices {1}.", session.Player.Forename, item.Name), 
                session.Player, null, session.Player);

            // delete from db
            ItemHelper.DeleteItem(item.Key);
        }
    }
}
