using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.Item;

namespace FoxMud.Game.State
{
    class TradeElement
    {
        public bool Approved { get; set; }
        public int Gold { get; set; }
        public List<string> Items { get; set; }
    }

    class Trade
    {
        public Dictionary<string, TradeElement> Props;

        public Trade(string trader, string tradee)
        {
            Props = new Dictionary<string, TradeElement>();
            Props[trader] = new TradeElement()
            {
                Approved = false,
                Gold = 0,
                Items = new List<string>()
            };
            Props[tradee] = new TradeElement()
            {
                Approved = false,
                Gold = 0,
                Items = new List<string>()
            };
        }

        internal string GetTraderKey()
        {
            return Props.Keys.First();
        }

        internal string GetTradeeKey()
        {
            return Props.Keys.Skip(1).First();
        }

        public bool Approved(string key)
        {
            return Props[key].Approved;
        }

        public bool IsApproved
        {
            get
            {
                return Props[GetTraderKey()].Approved && Props[GetTradeeKey()].Approved;
            }
            
        }

        public int Gold(string key)
        {
            return Props[key].Gold;
        }

        public void AddGold(string key, int gold)
        {
            Props[key].Gold += gold;
        }

        public void AddItem(string key, string item)
        {
            Props[key].Items.Add(item);
        }

        public void Approve(string key)
        {
            Props[key].Approved = true;
        }

        public void ResetApproval()
        {
            foreach (var item in Props)
                item.Value.Approved = false;
        }
    }

    class TradeState : SessionStateBase
    {
        private Trade _trade;
        private Session _otherSession;
        private string _trader;
        private string _tradee;

        public TradeState(Trade trade, Session otherSession)
        {
            _tradee = trade.GetTradeeKey();
            _trader = trade.GetTraderKey();
            _trade = trade;
            _otherSession = otherSession;
        }

        public override string ToString()
        {
            var result = string.Empty;

            result += "`Y---\n";
            foreach (var traderKey in _trade.Props.Keys)
            {
                var player = Server.Current.Database.Get<Player>(traderKey);
                result += player.Forename + " items:\n";
                result += "Gold: " + _trade.Gold(traderKey) + "\n";
                _trade.Props[traderKey].Items.ForEach(x => result += x + "\n");
                result += "---\n";
            }

            foreach (var traderKey in _trade.Props.Keys)
                result += string.Format("`W{0,-20}: {1}\n", Server.Current.Database.Get<Player>(traderKey).Forename, _trade.Props[traderKey].Approved ? "`GAPPROVED" : "`RNOT APPROVED");

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
                Session.WriteLine(this);

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
                    reprint = false;
                    break;
                case "cancel":
                case "c":
                    Cancel();
                    reprint = false;
                    break;
                case "help":
                case "h":
                    PrintHelp();
                    reprint = false;
                    break;
                case "print":
                case "p":
                    Session.WriteLine(this);
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
            Session.WriteLine("`Rh`welp - print this reference");
            Session.WriteLine("`Ra`wdd <item> - add an item to the trade");
            Session.WriteLine("`Rr`wemove <item> - remove an item from the trade");
            Session.WriteLine("`Rg`wold <amount> - add an amount of gold (can use negative numbers)");
            Session.WriteLine("`Ro`wk - approve the trade");
            Session.WriteLine("`Rc`wancel - exit the trade interface");
            Session.WriteLine("`Rp`wrint - print the current trade status");
            Session.WriteLine("`Y---");
            Session.WriteLine("`RNote: any time gold or items are added, trade must be ok'ed by both parties.");
            Session.WriteLine("`Y---");

            foreach (var traderKey in _trade.Props.Keys)
                Session.WriteLine("`W{0,-20}: {1}", Server.Current.Database.Get<Player>(traderKey).Forename, _trade.Props[traderKey].Approved ? "`GAPPROVED" : "`RNOT APPROVED");
        }

        private void Cancel()
        {
            foreach (var traderKey in _trade.Props.Keys)
            {
                var player = Server.Current.Database.Get<Player>(traderKey);
                player.Gold += _trade.Gold(traderKey);

                // return items
                foreach (var itemKey in _trade.Props[traderKey].Items)
                {
                    var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                    player.Inventory.Add(item.Key, item.Name);
                }
            }

            Session.WriteLine("You have canceled the trade.");
            _otherSession.WriteLine("`R{0} `whas canceled the trade.", Session.Player.Forename);

            Cleanup();
        }

        private void Cleanup()
        {
            Session.PopState();
            _otherSession.PopState();
            // one of these will work
            Server.Current.OpenTrades.Remove(Session);
            Server.Current.OpenTrades.Remove(_otherSession);
        }

        private void Approve(CommandContext commandContext)
        {
            _trade.Approve(Session.Player.Key);

            Session.WriteLine("You approve the trade.");
            _otherSession.WriteLine("{0} approved the trade.", Session.Player.Forename);

            if (_trade.IsApproved)
            {
                FinalizeTrade();
                Cleanup();
            }
        }

        private void FinalizeTrade()
        {
            Console.WriteLine("finalize: {0} {1}", Session.Player.Key, _otherSession.Player.Key);
            Console.WriteLine("trade: trader gold {0} tradee gold {1}", _trade.Gold(_trade.GetTraderKey()), _trade.Gold(_trade.GetTradeeKey()));

            SendToBothParties("`GYou have both approved the trade.");

            if (!ValidateTrade())
            {
                SendToBothParties("`RBoth players can't hold that much. Remove items from trade.");
                return;
            }

            // check who accepted the trade last, swap _trader and _tradee is necessary
            if (Session.Player.Key != _trader)
            {
                var temp = _trader;
                _trader = _tradee;
                _tradee = temp;
            }

            // normal trade of tradee vs trader
            foreach (var itemKey in _trade.Props[_trader].Items)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                _otherSession.Player.Inventory.Add(item.Key, item.Name);
            }

            _otherSession.Player.Gold += _trade.Gold(_trader);

            foreach (var itemKey in _trade.Props[_tradee].Items)
            {
                var item = Server.Current.Database.Get<PlayerItem>(itemKey);
                Session.Player.Inventory.Add(item.Key, item.Name);
            }

            Session.Player.Gold += _trade.Gold(_tradee);

            SendToBothParties("`gYou swap items and complete the trade.");
        }

        private bool ValidateTrade()
        {
            var traderItemsWeight = 0;
            _trade.Props[_trader].Items.ForEach(x => traderItemsWeight += Server.Current.Database.Get<PlayerItem>(x).Weight);
            
            var tradeeItemsWeight = 0;
            _trade.Props[_tradee].Items.ForEach(x => tradeeItemsWeight += Server.Current.Database.Get<PlayerItem>(x).Weight);

            return
                Session.Player.Weight + tradeeItemsWeight <= Session.Player.MaxWeight &&
                _otherSession.Player.Weight + traderItemsWeight <= _otherSession.Player.MaxWeight &&
                Session.Player.Inventory.Count + _trade.Props[_trader].Items.Count <= Session.Player.MaxInventory &&
                _otherSession.Player.Inventory.Count + _trade.Props[_tradee].Items.Count <= _otherSession.Player.MaxInventory;
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
                if (_trade.Gold(Session.Player.Key) < Math.Abs(gold) || _trade.Gold(Session.Player.Key) + gold < 0)
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
            //Console.WriteLine("session: {0}; gold: {1}; isAdding: {2}", Session.Player.Forename, gold, isAdding);

            _trade.AddGold(Session.Player.Key, gold);

            SendToSessionParty(string.Format("You {0} {1} gold {2} the trade.",
                    isAdding ? "add" : "remove",
                    Math.Abs(gold),
                    isAdding ? "to" : "from"));

            SendToOppsiteParty(string.Format("{0} {1} {2} gold {3} the trade.",
                Session.Player.Forename,
                isAdding ? "added" : "removed",
                Math.Abs(gold),
                isAdding ? "to" : "from"));
        }

        private void SendToSessionParty(string text)
        {
            Session.WriteLine(text);
        }

        private void SendToOppsiteParty(string text)
        {
            _otherSession.WriteLine(text);
        }

        private void ResetTradeApproval()
        {
            _trade.ResetApproval();
        }

        private void Remove(CommandContext commandContext)
        {
            var item = ItemHelper.LookForItem(_trade.Props[Session.Player.Key].Items, commandContext.ArgumentString);

            if(item == null)
            {
                Session.WriteLine("Couldn't find the item to remove.");
                return;
            }

            // remove and add to inventory (no weight checks)
            _trade.Props[Session.Player.Key].Items.Remove(item.Key);
            Session.Player.Inventory.Add(item.Key, item.Name);

            Session.WriteLine("You remove {0} from the trade.", item.Name);
            _otherSession.WriteLine("{0} removed {1} from the trade.", Session.Player.Forename, item.Name);

            ResetTradeApproval();
        }

        private void Add(CommandContext commandContext)
        {
            // find item in inventory
            var item = ItemHelper.FindInventoryItem(Session.Player, commandContext.ArgumentString);

            if (item == null)
            {
                Session.WriteLine("You don't have that item");
                return;
            }

            // add to traderitems
            Session.Player.Inventory.Remove(item.Key);
            _trade.AddItem(Session.Player.Key, item.Key);

            Session.WriteLine("You add {0} to the trade.", item.Name);
            _otherSession.WriteLine("{0} added {1} to the trade.", Session.Player.Forename, item.Name);

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
            _otherSession.WriteLine(text);
        }

        public override void OnStateLeave()
        {
            Session.Player.Status = GameStatus.Standing;
            _otherSession.Player.Status = GameStatus.Standing;

            base.OnStateLeave();
        }
    }
}
