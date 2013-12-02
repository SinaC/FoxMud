using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Visual
{
    [Command("look", false, TickDelay.Instant, 0, "look")]
    [Command("l", false, TickDelay.Instant, 0, "look")]
    [Command("examine", false, TickDelay.Instant, 0, "examine")]
    [Command("glance", false, TickDelay.Instant, 0, "examine")]
    class LookCommand : PlayerCommand
    {
        private readonly string realCommandName;

        public LookCommand(string realCommandName)
        {
            this.realCommandName = realCommandName;
        }

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: look");
            session.WriteLine("Syntax: look <player>");
            session.WriteLine("Syntax: look <item>");
        }

        public static void WriteNullRoomDescription(Session session)
        {
            session.WriteLine("The Void.");
            session.WriteLine("Something went terribly wrong and you've ended up in the void.\r\n" +
                              "You're not supposed to be here. Contact a staff member and they'll get you back\r\n" +
                              "to where you belong.");
        }

        private static void WriteRoomPlayerList(Session session, Room room)
        {
            foreach (var player in room.GetPlayers())
            {
                if (player == session.Player)
                    continue;

                session.WriteLine("`K{0}`M{1} is here.", 
                    player.Status == GameStatus.Trade ? "[TRADING] " : string.Empty,
                    session.Player.GetOtherPlayerDescription(player));
            }

            foreach (var npc in room.GetNpcs())
            {
                session.WriteLine("`M{0} is here.", npc.Name);
            }
        }

        private static void WriteRoomDescription(Session session, Room room)
        {
            session.WriteLine("`G{0}", room.Title);
            session.WriteLine("`W{0}", room.Description);
        }

        private static void WriteAvailableExits(Session session, Room room)
        {
            var builder = new StringBuilder();
            builder.Append("`wAvailable exits: [ ");

            foreach (var exit in room.Exits.Keys)
            {
                builder.Append(string.Format("{0} ", exit));
            }

            builder.Append("]");
            session.WriteLine(builder.ToString());
            session.WriteLine("");
        }

        private static void WriteItemsOnFloor(Session session, Room room)
        {
            foreach (var item in room.Items)
            {
                var actualItem = Server.Current.Database.Get<PlayerItem>(item.Key);
                session.WriteLine("`G{0} lies here.", actualItem.Description);
            }
        }

        private static void WriteGoldOnFloor(Session session, Room room)
        {
            if (room.Gold > 1)
                session.WriteLine("`YA pile of coins lies here.");
            else if (room.Gold > 0)
                session.WriteLine("`YA single gold coin lies here.");
        }

        private static void PerformLookAtRoom(Session session)
        {
            var room = Server.Current.Database.Get<Room>(session.Player.Location);

            if (room == null)
            {
                WriteNullRoomDescription(session);
                return;
            }

            WriteRoomDescription(session, room);
            WriteAvailableExits(session, room);
            WriteGoldOnFloor(session, room);
            WriteItemsOnFloor(session, room);
            WriteRoomPlayerList(session, room);
        }

        private void PerformLookAtPlayer(Session session, Room room, Player player)
        {
            session.Player.Send("You look at %d.", player);
            session.WriteLine(player.Description);
            player.Send("%d looks at you.", session.Player);
            room.SendPlayers("%d looks at %D.", session.Player, player, session.Player, player);
        }

        private void PerformExaminePlayer(Session session, Room room, Player player)
        {
            session.Player.Send("You glance at %d.", player);
            session.WriteLine("{0} {1}.", player.Forename, player.HitPointDescription);
            player.Send("%d glances at you.", session.Player);
            room.SendPlayers("%d glances at %D.", session.Player, player, session.Player, player);
        }

        private void PerformExamineNpc(Session session, Room room, NonPlayer npc)
        {
            session.WriteLine("You glance at {0}.", npc.Name);
            session.WriteLine("{0} {1}.", npc.Name, npc.HitPointDescription);
            room.SendPlayers(string.Format("{0} glances at {1}.", session.Player.Forename, npc.Name), session.Player, null, session.Player);
        }

        private void PerformLookAtNpc(Session session, Room room, NonPlayer npc)
        {
            session.WriteLine("You look at {0}.", npc.Name);
            session.WriteLine("{0}", npc.Description);

            if (npc.IsShopkeeper)
            {
                session.WriteLine("{0} is carrying: ", npc.Name);
                if (npc.Inventory.Count > 0)
                {
                    foreach (var item in npc.Inventory)
                        session.WriteLine("\t{0}", item.Value);
                }
                else
                {
                    session.WriteLine("\tNothing");
                }
            }

            room.SendPlayers(string.Format("{0} looks at {1}", session.Player.Forename, npc.Name), session.Player, null, session.Player);

            session.WriteLine(string.Empty);
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (context.Arguments.Count == 0)
            {
                PerformLookAtRoom(session);
                return;
            }

            // todo: look at a directional exit

            var room = Server.Current.Database.Get<Room>(session.Player.Location);
            if (room == null)
            {
                session.WriteLine("Couldn't find anything to look at");
                return;
            }

            var player = room.LookUpPlayer(session.Player, context.ArgumentString);
            if (player != null)
            {
                if (realCommandName == "look")
                    PerformLookAtPlayer(session, room, player);
                else
                    PerformExaminePlayer(session, room, player);
                return;
            }

            var npc = room.LookUpNpc(context.ArgumentString);
            if (npc != null)
            {
                if (realCommandName == "look")
                    PerformLookAtNpc(session, room, npc);
                else
                    PerformExamineNpc(session, room, npc);
                return;
            }

            // find item to look at
            var item = ItemHelper.FindInventoryItem(session.Player, context.ArgumentString);
            
            if (item == null)
                item = ItemHelper.FindFloorItem(room, context.ArgumentString);

            if (item != null)
            {
                item.LookAt(session);
                return;
            }

            session.WriteLine("You couldn't find anything like that.");
        }
    }
}
