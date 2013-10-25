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
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: kill <mob>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            // validate combat
            switch (session.Player.Status)
            {
                case GameStatus.Sleeping:
                case GameStatus.Sitting:
                    session.WriteLine("Do you really think it's a good idea to fight while you're sitting or sleeping?");
                    return;
                case GameStatus.Fighting:
                    session.WriteLine("You're already fighting!");
                    return;
                case GameStatus.Incapacitated:
                case GameStatus.MortallyWounded:
                    session.WriteLine("You're dying...");
                    return;
            }

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

            var combat = new Game.Combat();
            if (npc.Status == GameStatus.Fighting)
            {
                // add player to combat
                Server.Current.CombatHandler.AddToCombat(session.Player, npc.Key);
            }
            else
            {
                // start new combat
                combat.AddFighter(session.Player);
                combat.AddMob(npc);
                Server.Current.CombatHandler.StartFight(combat);
            }
        }
    }
}
