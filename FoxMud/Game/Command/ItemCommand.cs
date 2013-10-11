using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud.Game.Command
{
    [Command("inventory", false)]
    [Command("i", false)]
    class InventoryCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: inventory");
            session.WriteLine("Syntax: i");
        }

        public void Execute(Session session, CommandContext context)
        {
            session.WriteLine("Your inventory:");

            if (session.Player.Inventory.Count == 0)
            {
                session.WriteLine("\tEmpty\n");
                return;
            }

            // render like this, accounting for multiple items
            //      small knife (2)
            //      brass knuckles (1)
            foreach (var itemLine in session
                .Player.Inventory
                .GroupBy(i => i.Value)
                .Select(group => new
                    {
                        ItemName = group.Key,
                        Count = group.Count()
                    }))
            {
                session.WriteLine("\t{0} ({1})", itemLine.ItemName, itemLine.Count);
            }
            session.WriteLine("\n");
        }
    }

    [Command("give", false)]
    class GiveCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: give <item> <player>");
            session.WriteLine("Syntax: give <number> coin <player>");
        }

        public void Execute(Session session, CommandContext context)
        {
            try
            {
                bool givingGold = context.Arguments[1] == "coin";
                context.Arguments.Remove("coin");

                Player target = Server.Current.Database.Get<Player>(context.Arguments[1]);
                if (target == null)
                {
                    session.WriteLine("Who is {0}?", context.Arguments[1]);
                    return;
                }

                Room room = Server.Current.Database.Get<Room>(session.Player.Location);
                if (room == null)
                {
                    throw new Exception();
                }

                // players must be in the same room
                if (target.Location != room.Key)
                {
                    session.WriteLine("{0} is not here.", target.Forename);
                    return;
                }

                if (givingGold)
                {
                    long gold = Convert.ToInt64(context.Arguments[0]);
                    if (session.Player.Gold < gold)
                    {
                        session.WriteLine("You don't have that much...");
                        return;
                    }

                    // remove gold from session player
                    session.Player.Gold -= gold;
                    // give gold to target player
                    target.Gold += gold;
                }
                else
                {
                    // find item
                    var item = FindInventoryItem(session.Player, context.Arguments[0]);
                    
                    if (item == null)
                    {
                        session.WriteLine("Couldn't find {0}", context.Arguments[0]);
                        return;
                    }

                    // remove from player inventory
                    session.Player.Inventory.Remove(item.Key);

                    // messages
                    session.WriteLine("You give {0} to {1}.", item.Name, target.Forename);
                    target.Send(string.Format("{0} gives you {1}.", session.Player.Forename, item.Name), session.Player, target);
                    room.SendPlayers("%d gives something to %D", session.Player, target, new[] { session.Player, target });

                    // add to target inventory
                    target.Inventory.Add(item.Key, item.Name);
                }

                Server.Current.Database.Save(session.Player);
                Server.Current.Database.Save(target);
            }
            catch
            {
                PrintSyntax(session);
            }
        }

        // returns null if not found
        private PlayerItem FindInventoryItem(Player player, string keyword)
        {
            PlayerItem item = null;
            int ordinal = 1;
            int count = 0;

            Regex regex = new Regex(@"^((\d+)\.)");
            Match match = regex.Match(keyword);

            // parse ordinal number of item
            if (match.Success)
            {
                ordinal = Convert.ToInt32(match.Groups[2].Value); 
                // remove X. from string so it doesn't try to match on e.g. "2.knife"
                keyword = keyword.Replace(match.Groups[1].Value, string.Empty);
            }
            
            foreach (string guid in player.Inventory.Keys)
            {
                var temp = Server.Current.Database.Get<PlayerItem>(guid);

                // get item by key (guid)
                if (temp != null && temp.Keywords.Contains(keyword))
                {
                    count++;
                    if (count == ordinal)
                    {
                        item = temp;
                        break;
                    }
                }
            }

            return item;
        }
    }

    [Command("drop", false)]
    [Command("dr", false)]
    class DropCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            throw new NotImplementedException();
        }
        
        public void Execute(Session session, CommandContext context)
        {
            session.WriteLine("Not yet implemented...");
        }
    }

    [Command("get", false)]
    class GetCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            throw new NotImplementedException();
        }

        public void Execute(Session session, CommandContext context)
        {
            session.WriteLine("Not yet implemented...");
        }
    }
}
