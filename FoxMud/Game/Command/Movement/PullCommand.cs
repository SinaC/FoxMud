using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Admin;

namespace FoxMud.Game.Command.Movement
{
    [Command("pull", false, TickDelay.Double, 0)]
    class PullCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: pull <item>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                session.WriteLine("Pull what?");
                return;
            }

            var arg1 = context.Arguments[0];

            var room = RoomHelper.GetRoom(session.Player.Location);
            var item = ItemHelper.FindFloorItem(room, arg1.ToLower());

            if (item == null)
            {
                session.WriteLine("You can't find that.");
                return;
            }

            if (!item.CanPull)
            {
                session.WriteLine("You can't pull that.");
                return;
            }

            session.WriteLine("You pull {0}", item.Name);
            
            switch (item.TemplateKey)
            {
                    // this means new code every time a new pull object is created
                case "a dark lever":
                    session.WriteLine("A bell echoes through eternity...");
                    foreach (var admin in Server.Current.Database.GetAll<Player>().Where(p => p.IsAdmin))
                        admin.Send(string.Format("`R{0} needs authorization...", session.Player.Forename), session.Player);
                        
                    break;
            }
        }
    }
}
