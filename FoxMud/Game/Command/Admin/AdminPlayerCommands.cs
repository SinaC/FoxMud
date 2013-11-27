using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Admin
{
    [Command("authorize", true, TickDelay.Instant)]
    class AuthorizeCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: authorize <player>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                PrintSyntax(session);
                return;
            }

            var player = Server.Current.Database.Get<Player>(context.ArgumentString.ToLower());
            if (player == null)
            {
                session.WriteLine("Couldn't find {0}.", context.ArgumentString.ToLower());
                return;
            }

            if (player.Approved)
            {
                session.WriteLine("{0} doesn't need authorization right now.");
                return;
            }

            player.Approved = true;
            
            var srcRoom = RoomHelper.GetRoom(player.Location);
            var dstRoom = RoomHelper.GetRoom(Server.StartRoom);
            srcRoom.RemovePlayer(player);
            player.Location = dstRoom.Key;
            dstRoom.AddPlayer(player);
            dstRoom.SendPlayers(string.Format("{0} arrives in a blinding column of light.", player.Forename),
                                player, null, player);

            player.Experience = ExperienceResolver.FirstLevel;
            Server.Current.Database.Save(player);
            var commandInfo = Server.Current.CommandLookup.FindCommand("look", player);
            commandInfo.Command.Execute(Server.Current.SessionMonitor.GetPlayerSession(player), CommandContext.Create("look"));
            player.WritePrompt();
        }
    }

    [Command("stats", true, TickDelay.Instant)]
    class StatsCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: stats");
        }

        public override void Execute(Session session, CommandContext context)
        {
            session.WriteLine("Status: {0}\r\nMinutes: {1}\r\nLoggedIn: {2}", session.Player.Status,
                              session.Player.MinutesPlayed, session.Player.LoggedIn);
        }
    }

}
