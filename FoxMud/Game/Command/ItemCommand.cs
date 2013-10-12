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
    static class ItemHelper
    {
        public static PlayerItem FindInventoryItem(Player player, string keyword)
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
                keyword = keyword.Replace(match.Groups[1].Value, String.Empty);
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
                    var item = ItemHelper.FindInventoryItem(session.Player, context.Arguments[0]);
                    
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
                // should this be receiving player responsibility??
                Server.Current.Database.Save(target);
            }
            catch
            {
                PrintSyntax(session);
            }
        }
    }

    [Command("drop", false)]
    [Command("dr", false)]
    class DropCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: drop <item>");
            session.WriteLine("Syntax: dr <item>");
        }
        
        public void Execute(Session session, CommandContext context)
        {
            // does player have item?
            PlayerItem item = ItemHelper.FindInventoryItem(session.Player, context.Arguments[0]);
            if (item == null)
            {
                session.WriteLine("Can't find item: {0}", context.Arguments[0]);
                return;
            }

            // remove from player inventory
            session.Player.Inventory.Remove(item.Key);
            Server.Current.Database.Save(session.Player);

            // drop in room
            var room = Server.Current.Database.Get<Room>(session.Player.Location);
            if (room != null)
            {
                room.AddItem(item); // this saves the room
                session.WriteLine("You drop {0}.", item.Name);
                room.SendPlayers("%d drops something.", session.Player, null, session.Player);
            }
        }
    }

    [Command("get", false)]
    class GetCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("get <item>");
            session.WriteLine("get all");
            session.WriteLine("get <item> corpse (not yet implemented...");
            session.WriteLine("get all corpse (not yet implemented...");
            session.WriteLine("get <item> <container>");
        }

        public void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                PrintSyntax(session);
                return;
            }

            // pick up everything in room, minding weight and max inventory
            if (context.Arguments[0] == "all")
            {
                bool pickedUpAnything = false;
                var room = Server.Current.Database.Get<Room>(session.Player.Location);
                if (room != null)
                {
                    foreach (var key in room.Items.Keys)
                    {
                        var item = Server.Current.Database.Get<PlayerItem>(key);
                        if (item != null
                            && session.Player.Inventory.Count + 1 < session.Player.MaxInventory // not over inventory
                            && session.Player.Weight + item.Weight < session.Player.MaxWeight) // not over weight
                        {
                            // move item from floor to inventory
                            room.RemoveItem(item);
                            session.Player.Inventory.Add(item.Key, item.Name);
                            session.WriteLine("You pick up {0}", item.Name);
                            pickedUpAnything = true;
                        }
                        else
                        {
                            session.WriteLine("You picked up all you could.");
                            return;
                        }
                    }

                    if (pickedUpAnything)
                        room.SendPlayers("%d picked up some items.", session.Player, null, session.Player);
                }
                return;
            }

            // get <item>
            // this item should be in the room
            if (context.Arguments.Count == 1)
            {
                var room = Server.Current.Database.Get<Room>(session.Player.Location);
                if (room != null)
                {
                    // find item on floor by keyword
                    foreach (var key in room.Items.Keys)
                    {
                        var item = Server.Current.Database.Get<PlayerItem>(key);
                        if (item != null
                            && session.Player.Inventory.Count + 1 < session.Player.MaxInventory // not over inventory
                            && session.Player.Weight + item.Weight < session.Player.MaxWeight // not over weight
                            && item.Keywords.Contains(context.Arguments[0])) // found by keyword
                        {
                            // move item from floor to inventory
                            room.RemoveItem(item);
                            session.Player.Inventory.Add(item.Key, item.Name);
                            session.WriteLine("You pick up {0}", item.Name);
                            room.SendPlayers("%d picked up some something.", session.Player, null, session.Player);
                            return;
                        }
                        else
                        {
                            session.WriteLine("You couldn't pick that up.");
                            return;
                        }
                    }
                }
                return;
            }

            // must've been get <item> <container>
            // check inventory for container by keyword
            PlayerItem container = null;
            PlayerItem containerItem = null;
            bool containerFound = false;
            string argContainer = context.Arguments[1].ToLower();
            string argItem = context.Arguments[0].ToLower();

            foreach (var key in session.Player.Inventory.Keys)
            {
                container = Server.Current.Database.Get<PlayerItem>(key);
                if (container != null
                    && container.WearLocation == Wearlocation.Container
                    && container.Keywords.Contains(argContainer))
                {
                    containerFound = true;
                    // look at items inside container
                    foreach (var innerKey in container.ContainedItems.Keys)
                    {
                        containerItem = Server.Current.Database.Get<PlayerItem>(innerKey);
                        if (containerItem != null
                            && containerItem.Keywords.Contains(argItem))
                        {
                            if (session.Player.Inventory.Count + 1 < session.Player.MaxInventory)
                            {
                                // attempt move item from container to inventory
                                container.ContainedItems.Remove(containerItem.Key);
                                session.Player.Inventory.Add(containerItem.Key, containerItem.Name);
                                session.WriteLine("You get {0} from {1}", containerItem.Name, container.Name);
                                return;
                            }

                            session.WriteLine("Your hands are too full.");
                            return;
                        }
                    }
                }
            }

            // check floor for container by keyword if container wasn't found in inventory
            if (!containerFound)
            {
                var room = Server.Current.Database.Get<Room>(session.Player.Location);
                if (room != null)
                {
                    foreach (var key in room.Items.Keys)
                    {
                        container = Server.Current.Database.Get<PlayerItem>(key);
                        if (container != null
                            && container.WearLocation == Wearlocation.Container
                            && container.Keywords.Contains(argContainer))
                        {
                            containerFound = true;
                            // look at items inside container
                            foreach (var innerKey in container.ContainedItems.Keys)
                            {
                                containerItem = Server.Current.Database.Get<PlayerItem>(innerKey);
                                if (containerItem != null
                                    && containerItem.Keywords.Contains(argItem))
                                {
                                    if (session.Player.Inventory.Count + 1 > session.Player.MaxInventory)
                                    {
                                        session.WriteLine("Your hands are too full.");
                                        return;
                                    }
                                    else if (session.Player.Weight + containerItem.Weight > session.Player.MaxWeight)
                                    {
                                        session.WriteLine("You can't carry that much weight.");
                                        return;
                                    }

                                    // attempt move item from container to inventory
                                    container.ContainedItems.Remove(containerItem.Key);
                                    session.Player.Inventory.Add(containerItem.Key, containerItem.Name);
                                    session.WriteLine("You get {0} from {1}", containerItem.Name, container.Name);
                                    string roomString = string.Format("%d gets {0} from {1}.", containerItem.Name, container.Name);
                                    room.SendPlayers(roomString, session.Player, null, session.Player);
                                    return;
                                }
                            }
                        }
                    }

                    session.WriteLine("Can't find a container called {0}", argContainer);
                    return;
                }
            }

            session.WriteLine("Get what from what?");
        }
    }

    [Command("put", false)]
    [Command("p", false)]
    class PutCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            throw new NotImplementedException();
        }

        public void Execute(Session session, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Command("empty", false)]
    class EmptyCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            throw new NotImplementedException();
        }

        public void Execute(Session session, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Command("fill", false)]
    [Command("f", false)]
    class FillCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            throw new NotImplementedException();
        }

        public void Execute(Session session, CommandContext context)
        {
            throw new NotImplementedException();
        }
    }
}
