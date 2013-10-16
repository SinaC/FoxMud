﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Admin
{
    [Command("shutdown", true)]
    class ShutdownCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: shutdown [time in ms]");
        }

        public void Execute(Session session, CommandContext context)
        {
            int shutdownDelay = 20000;

            if (context.Arguments.Count > 0)
                int.TryParse(context.Arguments[0], out shutdownDelay);

            Server.Current.RepopHandler.Stop();

            foreach (var playerSession in Server.Current.SessionMonitor.EnumerateSessions())
                playerSession.WriteLine("A thundering roar abruptly alerts you to the impending end...");

            new System.Threading.Timer(obj => { shutdown(session); }, null, shutdownDelay, System.Threading.Timeout.Infinite);
        }

        private void shutdown(Session session)
        {
            session.WriteLine("Destroying NPC's and their items...");

            // call NonPlayer.Die(true) for every nonplayer object
            foreach (var npc in Server.Current.Database.GetAll<NonPlayer>())
                npc.Die(true);

            session.WriteLine("Shutting down now.");

            Server.Current.Stop();
        }
    }
}