using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.Item;

namespace FoxMud.Game.State
{
    class TradeState : SessionStateBase
    {
        private bool TraderApproved;
        private bool TradeeApproved;
        private int TraderGold;
        private int TradeeGold;
        private Session TraderSession;
        private Session TradeeSession;
        private List<string> TraderItems;
        private List<string> TradeeItems;

        public TradeState(Session traderSession, Session tradeeSession)
        {
            TraderApproved = TradeeApproved = false;
            TraderGold = TradeeGold = 0;
            TraderSession = traderSession;
            TradeeSession = tradeeSession;
            TraderItems = new List<string>();
            TradeeItems = new List<string>();
        }

        public override string ToString()
        {
            var result = string.Empty;

            result += "`Y---\n";
            result += TraderSession.Player.Forename + " items:\n";
            result += "Gold: " + TraderGold + "\n";
            TraderItems.ForEach(x => string.Format("{0}\n", x));
            result += "---\n";
            result += TradeeSession.Player.Forename + " items:\n";
            result += "Gold: " + TradeeGold + "\n";
            TradeeItems.ForEach(x => string.Format("{0}\n", x));
            result += "---\n";

            return result;
        }

        public override void OnInput(string input)
        {
            bool reprint = false;

            if(!string.IsNullOrEmpty(input))
            {
                reprint = TryExecuteCommand(input);
            }

            if (reprint)
            {
                TraderSession.WriteLine(this);
                TradeeSession.WriteLine(this);
            }

            base.OnInput(input);
        }

        private bool TryExecuteCommand(string input)
        {
            bool reprint = true;

            var commandContext = CommandContext.Create(input);
            var player = Server.Current.Database.Get<Player>(Session.Player.Key);
            var commandInfo = Server.Current.CommandLookup.FindCommand(commandContext.CommandName, player);

            // switch case for add/remove/exit/cancel/etc
            switch (commandContext.CommandName.ToLower())
            {
                case "add":
                case "a":
                    Add(commandContext);
                    break;
                case "remove":
                case "r":
                    Remove(commandContext);
                    break;
                case "gold":
                case "g":
                    Gold(commandContext);
                    break;
                case "ok":
                case "o":
                    Approve(commandContext);
                    break;
                case "cancel":
                case "c":
                    Cancel();
                    break;
                case "help":
                case "h":
                    PrintHelp();
                    reprint = false;
                    break;
                default:
                    Session.WriteLine("`rCommand not recognized.");
                    reprint = false;
                    break;
            }

            return reprint;
        }

        private void PrintHelp()
        {
            Session.WriteLine("`YTrade Interface Commands");
            Session.WriteLine("`Y---");
            Session.WriteLine("`Wh`Yelp - print this reference");
            Session.WriteLine("`Wa`Ydd <item> - add an item to the trade");
            Session.WriteLine("`Wr`Yemove <item> - remove an item from the trade");
            Session.WriteLine("`Wg`Yold <amount> - add an amount of gold (can use negative numbers)");
            Session.WriteLine("`Wo`Yk - approve the trade");
            Session.WriteLine("`Wc`Yancel - exit the trade interface");
            Session.WriteLine("`YNote: any time gold or items are added, trade must be ok'ed by both parties.");
            Session.WriteLine("`Y---");
            Session.WriteLine("`W{0}: {1}", TraderSession.Player.Forename, TraderApproved ? "`GAPPROVED" : "`RNOT APPROVED");
            Session.WriteLine("`W{0}: {1}", TradeeSession.Player.Forename, TradeeApproved ? "`GAPPROVED" : "`RNOT APPROVED");
        }

        private void Cancel()
        {
            // return items
            foreach (var itemKey in TraderItems)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                TraderSession.Player.Inventory.Add(item.Key, item.Name);
            }

            foreach (var itemKey in TradeeItems)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                TradeeSession.Player.Inventory.Add(item.Key, item.Name);
            }

            // return gold
            TraderSession.Player.Gold += TraderGold;
            TradeeSession.Player.Gold += TradeeGold;

            Session.WriteLine("You have canceled the trade.");
            
            if (Session == TraderSession)
                TradeeSession.WriteLine("`R{0} `whas canceled the trade.", TraderSession.Player.Forename);
            else
                TraderSession.WriteLine("`R{0} `whas canceled the trade.", TradeeSession.Player.Forename);

            Cleanup();
        }

        private void Cleanup()
        {
            TraderSession.PopState();
            TradeeSession.PopState();
            Server.Current.OpenTrades.Remove(TraderSession);
        }

        private void Approve(CommandContext commandContext)
        {
            TraderApproved = true;
            Session.WriteLine("You have approved the trade.");

            if (TradeeApproved && TraderApproved)
            {
                FinalizeTrade();
                Cleanup();
            }
                
        }

        private void FinalizeTrade()
        {
            SendToBothParties("`GYou have both approved the trade.");

            if (!ValidateTrade())
            {
                SendToBothParties("`RBoth players can't hold that much. Remove items from trade.");
                return;
            }

            // swap items, gold, etc
            foreach (var itemKey in TraderItems)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                TradeeSession.Player.Inventory.Add(item.Key, item.Name);
            }

            TradeeSession.Player.Gold += TraderGold;

            foreach (var itemKey in TradeeItems)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                Session.Player.Inventory.Add(item.Key, item.Name);
            }

            Session.Player.Gold += TradeeGold;

            SendToBothParties("You swap items and complete the trade.");
        }

        private bool ValidateTrade()
        {
            var traderItemsWeight = 0;
            TraderItems.ForEach(x => traderItemsWeight += Server.Current.Database.Get<PlayerItem>(x).Weight);
            
            var tradeeItemsWeight = 0;
            TradeeItems.ForEach(x => tradeeItemsWeight += Server.Current.Database.Get<PlayerItem>(x).Weight);

            return
                Session.Player.Weight + tradeeItemsWeight <= Session.Player.MaxWeight &&
                TradeeSession.Player.Weight + traderItemsWeight <= TradeeSession.Player.MaxWeight &&
                Session.Player.Inventory.Count + TradeeItems.Count <= Session.Player.MaxInventory &&
                TradeeSession.Player.Inventory.Count + TraderItems.Count <= TradeeSession.Player.MaxInventory;
        }

        private void Gold(CommandContext commandContext)
        {
            int gold = 0;
            if (!int.TryParse(commandContext.ArgumentString, out gold))
            {
                SendToSessionParty("Syntax: gold <amount of gold>");
                return;
            }

            if (gold > Session.Player.Gold || gold == 0)
            {
                SendToSessionParty("You don't have that much gold.");
                return;
            }

            // player is attempting to remove gold from the trade
            if (gold < 0)
            {
                if(TraderGold < Math.Abs(gold) || TraderGold + gold < 0)
                {
                    SendToSessionParty("You can't remove that much gold.");
                    return;
                }

                AddOrRemoveGold(gold, false);
            }
            else
            {
                AddOrRemoveGold(gold, true);
            }

            ResetTradeApproval();
        }

        private void AddOrRemoveGold(int gold, bool isAdding)
        {
            Console.WriteLine("session: {0}; gold: {1}; isAdding: {2}", Session.Player.Forename, gold, isAdding);
            if (Session == TraderSession)
            {
                TraderSession.Player.Gold += isAdding ? gold * -1 : gold;
                TraderGold += gold;
                
                SendToSessionParty(string.Format("You {0} {1} gold {2} the trade.", 
                    isAdding ? "add" : "remove", 
                    Math.Abs(gold),
                    isAdding ? "to" : "from"));

                SendToOppsiteParty(string.Format("{0} {1} {2} gold {3} the trade.",
                    TraderSession.Player.Forename,
                    isAdding ? "added" : "removed",
                    Math.Abs(gold),
                    isAdding ? "to" : "from"));
            }
            else
            {
                TradeeSession.Player.Gold += isAdding ? gold * -1 : gold;
                TradeeGold += gold;

                SendToSessionParty(string.Format("You {0} {1} gold {2} the trade.",
                    isAdding ? "add" : "remove",
                    Math.Abs(gold),
                    isAdding ? "to" : "from"));

                SendToOppsiteParty(string.Format("{0} {1} {2} gold {3} the trade.",
                    TradeeSession.Player.Forename,
                    isAdding ? "added" : "removed",
                    Math.Abs(gold),
                    isAdding ? "to" : "from"));
            }
        }

        private void SendToSessionParty(string text)
        {
            if (Session == TraderSession)
                TraderSession.WriteLine(text);
            else
                TradeeSession.WriteLine(text);
        }

        private void SendToOppsiteParty(string text)
        {
            if (Session == TraderSession)
                TradeeSession.WriteLine(text);
            else
                TraderSession.WriteLine(text);
        }

        private void ResetTradeApproval()
        {
            TraderApproved = TradeeApproved = false;
        }

        private void Remove(CommandContext commandContext)
        {
            var thisSession = Session == TraderSession ? TraderSession : TradeeSession;
            var otherSession = Session == TraderSession ? TradeeSession : TraderSession;
            var thisItems = Session == TraderSession ? TraderItems : TradeeItems;

            var item = ItemHelper.LookForItem(thisItems, commandContext.ArgumentString);

            if(item == null)
            {
                thisSession.WriteLine("Couldn't find the item to remove.");
                return;
            }

            // remove and add to inventory (no weight checks)
            thisItems.Remove(item.Key);
            thisSession.Player.Inventory.Add(item.Key, item.Name);

            thisSession.WriteLine("You remove {0} from the trade.", item.Name);
            otherSession.WriteLine("{0} removed {1} from the trade.", thisSession.Player.Forename, item.Name);

            ResetTradeApproval();
        }

        private void Add(CommandContext commandContext)
        {
            var thisSession = Session == TraderSession ? TraderSession : TradeeSession;
            var otherSession = Session == TraderSession ? TradeeSession : TraderSession;
            var thisItems = Session == TraderSession ? TraderItems : TradeeItems;

            // find item in inventory
            var item = ItemHelper.FindInventoryItem(thisSession.Player, commandContext.ArgumentString);

            if (item == null)
            {
                thisSession.WriteLine("You don't have that item");
                return;
            }

            // add to traderitems
            thisSession.Player.Inventory.Remove(item.Key);
            thisItems.Add(item.Key);

            thisSession.WriteLine("You add {0} to the trade.", item.Name);
            otherSession.WriteLine("{0} added {1} to the trade.", thisSession.Player.Forename, item.Name);

            ResetTradeApproval();
        }

        public override void OnStateEnter()
        {
            Session.WriteLine("`YTrade Interface: type `Whelp`Y for commands.");
            PrintHelp();
            Session.Player.Status = GameStatus.Trade;

            base.OnStateEnter();
        }

        private void SendToBothParties(string text)
        {
            Session.WriteLine(text);
            TradeeSession.WriteLine(text);
        }

        public override void OnStateLeave()
        {
            TraderSession.Player.Status = GameStatus.Standing;
            TradeeSession.Player.Status = GameStatus.Standing;

            base.OnStateLeave();
        }
    }
}
