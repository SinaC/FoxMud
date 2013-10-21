using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Admin;

namespace FoxMud.Game.Command.Combat
{
    [Command("kill", false)]
    [Command("attack", false)]
    class KillCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: kill <mob>");
        }

        public void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                session.WriteLine("Kill what?");
                return;
            }

            // find mob
            var room = RoomHelper.GetRoom(session.Player.Location);
            var npc = room.GetNpcs().FirstOrDefault(n => n.Keywords.Contains(context.ArgumentString));
            if (npc == null)
            {
                session.WriteLine("You can't find that here.");
                return;
            }

            if (npc.IsShopkeeper)
            {
                session.WriteLine("You can't fight a shopkeeper.");
                return;
            }

            // validate combat
            if (session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You can't start a fight right now.");
                return;
            }

            var combat = new Game.Combat();
            combat.AddFighter(session.Player);
            combat.AddMob(npc);
            Server.Current.CombatHandler.StartFight(combat);
        }
    }
}
