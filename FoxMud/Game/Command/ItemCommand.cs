﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud.Game.Command
{
    static class ItemHelper
    {
        public static PlayerItem FindInventoryItem(Player player, string keyword, bool isContainer = false, bool isEquipped = false)
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

            foreach (string guid in 
                isEquipped 
                ? player.Equipped.Values.Select(w => w.Key) 
                : player.Inventory.Keys)
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

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T) formatter.Deserialize(ms);
            }
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
            session.WriteLine("put <item> <container>");
        }

        public void Execute(Session session, CommandContext context)
        {
            try
            {
                PlayerItem container = null;
                PlayerItem putItem = null;
                bool containerFound = false;
                bool itemFound = false;
                string argContainer = context.Arguments[1];
                string argItem = context.Arguments[0];

                // item in inevntory?
                foreach (var key in session.Player.Inventory.Keys)
                {
                    putItem = Server.Current.Database.Get<PlayerItem>(key);
                    if (putItem != null
                        && putItem.Keywords.Contains(argItem))
                    {
                        itemFound = true;
                        break;
                    }
                }

                if (!itemFound)
                {
                    session.WriteLine("Can't find item: {0}", argItem);
                    return;
                }

                // container in inventory?
                foreach (var key in session.Player.Inventory.Keys)
                {
                    container = Server.Current.Database.Get<PlayerItem>(key);
                    if (container != null
                        && container.WearLocation == Wearlocation.Container
                        && container.Keywords.Contains(argContainer))
                    {
                        containerFound = true;
                        break;
                    }
                }

                // container on floor?
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
                                break;
                            }
                        }
                    }
                }

                if (!containerFound)
                {
                    session.WriteLine("Can't find container: {0}", argContainer);
                    return;
                }

                // move item
                session.Player.Inventory.Remove(putItem.Key);
                container.ContainedItems.Add(putItem.Key, putItem.Name);
                session.WriteLine("You put {0} in {1}", putItem.Name, container.Name);
                return;

                // save??
            }
            catch
            {
                PrintSyntax(session);
            }

            session.WriteLine("Put what in what?");
        }
    }

    [Command("empty", false)]
    class EmptyCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("empty <container>");
        }

        public void Execute(Session session, CommandContext context)
        {
            string argContainer = context.Arguments[0];

            // find container
            PlayerItem container = ItemHelper.FindInventoryItem(session.Player, argContainer, true);
            if (container == null)
            {
                session.Write("Can't find container: {0}", argContainer);
                return;
            }

            var room = RoomHelper.GetPlayerRoom(session.Player);

            // toss on floor
            foreach (var item in container.ContainedItems.ToArray())
            {
                container.ContainedItems.Remove(item.Key);
                room.Items.Add(item.Key, item.Value);
            }

            session.WriteLine("You empty {0}", container.Name);
            room.SendPlayers(string.Format("%d empties {0}", container.Name), session.Player, null, session.Player);

            // save?
        }
    }

    [Command("fill", false)]
    [Command("f", false)]
    class FillCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("fill <container>");
        }

        public void Execute(Session session, CommandContext context)
        {
            string argContainer = context.Arguments[0];

            // container in inventory?
            PlayerItem container = ItemHelper.FindInventoryItem(session.Player, argContainer, true);
            if (container == null)
            {
                session.WriteLine("Can't find container: {0}", argContainer);
                return;
            }

            var room = RoomHelper.GetPlayerRoom(session.Player);

            // add items from room, with weight checks
            foreach (var key in room.Items.Keys.ToArray())
            {
                // get item
                var roomItem = Server.Current.Database.Get<PlayerItem>(key);
                if (roomItem != null)
                {
                    if (session.Player.Weight + roomItem.Weight <= session.Player.MaxWeight)
                    {
                        room.Items.Remove(roomItem.Key);
                        container.ContainedItems[roomItem.Key] = roomItem.Name;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            session.WriteLine("You filled your {0}", container.Name);
            room.SendPlayers(string.Format("%d filled %o {0}.", container.Name), session.Player, null, session.Player);
        }
    }

    [Command("eq", false, "eq")]
    [Command("equip", false, "equip")]
    [Command("don", false, "equip")]
    [Command("wear", false, "equip")]
    [Command("unequip", false, "unequip")]
    [Command("remove", false, "unequip")]
    [Command("rem", false, "unequip")]
    class EquipCommand : PlayerCommand
    {
        private readonly string command;

        public EquipCommand(string command)
        {
            this.command = command;
        }

        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: equip <item>");
            session.WriteLine("Syntax: don <item>");
            session.WriteLine("Syntax: wear <item>");
            session.WriteLine("Syntax: unequip <item>");
            session.WriteLine("Syntax: remove <item>");
            session.WriteLine("Syntax: rem <item>");
        }

        public void Execute(Session session, CommandContext context)
        {
            if (command == "eq")
            {
                foreach (var location in Enum.GetValues(typeof(Wearlocation)).Cast<Wearlocation>())
                {
                    if (location == Wearlocation.Key || location == Wearlocation.Container || location == Wearlocation.None)
                        continue;

                    // don't display 
                    if (session.Player.Equipped.ContainsKey(Wearlocation.BothHands)
                        && (location == Wearlocation.RightHand || location == Wearlocation.LeftHand))
                        continue;

                    // don't display both hands location if either single hand is equipped
                    if (location == Wearlocation.BothHands
                        && (session.Player.Equipped.ContainsKey(Wearlocation.RightHand)
                            || session.Player.Equipped.ContainsKey(Wearlocation.LeftHand)))
                        continue;

                    if (session.Player.Equipped.ContainsKey(location))
                        session.WriteLine("{0,-15}{1}", "<" + StringHelpers.GetWearLocation(location) + ">", session.Player.Equipped[location].Name);
                    else
                        session.WriteLine("{0,-15}Empty", "<" + StringHelpers.GetWearLocation(location) + ">");
                }

                return;
            }

            if (context.Arguments.Count == 0)
            {
                session.WriteLine("{0} what?", context.CommandName);
                return;
            }

            string argItem = context.Arguments[0];

            if (command == "equip")
            {
                // find inventory item
                var item = ItemHelper.FindInventoryItem(session.Player, argItem);
                if (item == null)
                {
                    session.WriteLine("Can't find item: {0}", argItem);
                    return;
                }

                // equip
                item.Equip(session);
            }
            else
            {
                // find equipped item
                var item = ItemHelper.FindInventoryItem(session.Player, argItem, false, true);

                if (item == null)
                {
                    session.WriteLine("You're not wearing that.");
                    return;
                }

                // remove, put in inventory
                item.Unequip(session);
            }
        }
    }

    [Command("list", false)]
    class ListCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: list");
        }

        public void Execute(Session session, CommandContext context)
        {
            // find shopkeeper
            var room = RoomHelper.GetPlayerRoom(session.Player.Location);
            var shopkeeper = room.GetNpcs().FirstOrDefault(s => s.IsShopkeeper);

            if (shopkeeper == null)
            {
                session.WriteLine("There is notihng to buy here.");
                return;
            }

            foreach (var key in shopkeeper.Inventory.Keys)
            {
                var item = Server.Current.Database.Get<PlayerItem>(key);
                session.WriteLine("{0,-15}{1}", "[" + item.Value + "]", item.Name);
            }
        }
    }


    [Command("buy", false)]
    [Command("b", false)]
    class BuyCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: buy <item>");
            session.WriteLine("Syntax: buy <qty> <item>");
        }

        public void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                session.WriteLine("Buy what?");
                return;
            }

            // find shopkeeper
            var room = RoomHelper.GetPlayerRoom(session.Player.Location);
            var shopkeeper = room.GetNpcs().FirstOrDefault(s => s.IsShopkeeper);

            if (shopkeeper == null)
            {
                session.WriteLine("There is notihng to buy here.");
                return;
            }

            int qty = 1;
            if (!int.TryParse(context.Arguments[0], out qty))
                qty = 1;
            else
                context.Arguments.RemoveAt(0);

            // get item
            PlayerItem item = null;
            foreach (var key in shopkeeper.Inventory.Keys)
            {
                var template = Server.Current.Database.Get<PlayerItem>(key);
                if (template.Keywords.Contains(context.Arguments[0]))
                {
                    item = template;
                    break;
                }
            }

            if (item == null)
            {
                session.WriteLine("They don't sell that here.");
                return;
            }

            // calculate price
            var price = item.Value * qty;
            if (session.Player.Gold < price)
            {
                session.WriteLine("You can't afford that much.");
                return;
            }

            session.Player.Gold -= price;

            // duplicate in inventory, minding inventory and weight limits
            // if over weight/inventory, dump on the floor
            for (int i = 0; i < qty; i++)
            {
                var dupedItem = item.Copy();
                if (session.Player.Inventory.Count + 1 <= session.Player.MaxInventory)
                    session.Player.Inventory[dupedItem.Key] = dupedItem.Name;
                else
                    room.AddItem(dupedItem);

                Server.Current.Database.Save(dupedItem);
            }

            session.WriteLine("You buy {0} {1}", qty, item.Name);
            room.SendPlayers("%d buys something.", session.Player, null, session.Player);
        }
    }

    [Command("sell", false)]
    class SellCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: sell <item>");
        }

        public void Execute(Session session, CommandContext context)
        {
            // validate item arg
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                session.WriteLine("Sell what?");
                return;
            }

            PlayerItem itemToSell = ItemHelper.FindInventoryItem(session.Player, context.ArgumentString, false, false);
            if (itemToSell == null)
            {
                session.WriteLine("You don't have that to sell.");
                return;
            }

            // get shopkeeper in room
            var room = RoomHelper.GetPlayerRoom(session.Player.Location);
            NonPlayer shopkeeper = null;
            foreach (var npc in room.GetNpcs())
            {
                if (npc.IsShopkeeper)
                {
                    shopkeeper = npc;
                    break;
                }
            }

            if (shopkeeper == null)
            {
                session.WriteLine("You can't sell that here.");
                return;
            }

            // destroy item, remove from inventory, give gold, save player
            var sellPrice = itemToSell.Value/2;
            session.Player.Gold += sellPrice;
            session.Player.Inventory.Remove(itemToSell.Key);
            Server.Current.Database.Save(session.Player); // save so they can't try to sell again
            session.WriteLine("You sold {0} for {1} gold.", itemToSell.Name, sellPrice);
            Server.Current.Database.Delete<PlayerItem>(itemToSell.Key);
        }
    }

}
