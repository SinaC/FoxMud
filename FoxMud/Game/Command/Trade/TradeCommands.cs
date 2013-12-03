using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.State;

namespace FoxMud.Game.Command.Trade
{
    [Command("trade", false, 1)]
    class TradeCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: trade <player>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You can't start a trade right now");
                return;
            }

            try
            {
                // first, determine if the target has already requested a trade
                var target = Server.Current.Database.Get<Player>(context.ArgumentString.ToLower());

                if (target == null || !target.LoggedIn)
                {
                    session.WriteLine("Can't find that person.");
                    return;
                }

                if (target.Key == session.Player.Key)
                {
                    session.WriteLine("Can't trade with yourself.");
                    return;
                }

                var targetSession = Server.Current.SessionMonitor.GetPlayerSession(target);

                if (targetSession == null)
                {
                    Server.Current.Log(LogType.Error, "couldn't find session to start trade.");
                    session.WriteLine("problem loading session...");
                    return;
                }

                if (Server.Current.OpenTrades.ContainsKey(targetSession) &&
                    Server.Current.OpenTrades[targetSession] == session.Player.Key)
                {
                    // there is an open trade, validate
                    if (target.Location != session.Player.Location || !target.LoggedIn)
                    {
                        session.WriteLine("That person isn't here right now.");
                        return;
                    }

                    if (target.Status != GameStatus.Standing)
                    {
                        session.WriteLine("That player can't start the requested trade at the moment.");
                        return;
                    }

                    var text = "Starting trade now...";
                    session.WriteLine(text);
                    targetSession.WriteLine(text);

                    // start the trade
                    var trade = new Game.State.Trade(targetSession.Player.Key, session.Player.Key);

                    var tradeState = new TradeState(trade, session);
                    targetSession.PushState(tradeState);
                    var otherTradeState = new TradeState(trade, targetSession);
                    session.PushState(otherTradeState);
                }
                else
                {
                    // new trade request
                    Server.Current.OpenTrades.Add(session, target.Key);
                    session.WriteLine("Your trade request has been sent to {0}...", targetSession.Player.Forename);
                    targetSession.WriteLine("{0} would like to start a trade with you...", session.Player.Forename);
                    targetSession.WriteLine("Type `wtrade {0}`g to begin the trade.", session.Player.Forename);
                }
            }
            catch
            {
                PrintSyntax(session);
            }
        }
    }

}
