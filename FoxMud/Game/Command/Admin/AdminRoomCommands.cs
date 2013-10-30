using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Admin
{
    class RoomHelper
    {
        public static Room GetRoom(string key)
        {
            var room = Server.Current.Database.Get<Room>(key);
            return room;
        }

        public static void SaveRoom(Room room)
        {
            Server.Current.Database.Save(room);
        }
    }

    /// <summary>
    /// admin command to create a room
    /// </summary>
    [Command("makeroom", true, TickDelay.Instant)]
    class MakeRoomCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: makeroom <room name>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ArgumentString))
            {
                PrintSyntax(session);
                return;
            }

            // confirm room doesn't exist
            // todo: there will inevitably be rooms with similar names, so they'll need to be key'ed different
            var room = RoomHelper.GetRoom(context.ArgumentString);
            if (room != null)
            {
                session.WriteLine(string.Format("Room already exists: {0}", context.ArgumentString));
                return;
            }

            // create room
            room = new Room()
            {
                Key = context.ArgumentString.ToLower(),
                Title = context.ArgumentString,
                Description = "This exitless room has no description."
            };

            // save room
            Server.Current.Database.Save<Room>(room);

            // tell the admin player what the id is, so they can get to it
            session.WriteLine(string.Format("Room created: {0}", context.ArgumentString));
        }
    }

    /// <summary>
    /// admin command to jump to a specific room
    /// </summary>
    [Command("goto", true, TickDelay.Instant)]
    class GotoCommand : CallbackCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: goto <room key>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            // this feels hacky, maybe could use an extension method here instead of interface inheritance
            string dummyString;
            Execute(session, context, out dummyString);
        }

        public override void Execute(Session session, CommandContext context, out string callbackCommand)
        {
            // make player "look" as cue to show the jump
            callbackCommand = "look";

            var room = RoomHelper.GetRoom(context.ArgumentString);

            if (room != null)
            {
                // remove player from current room
                var currentRoom = RoomHelper.GetRoom(session.Player.Location);
                if (currentRoom != null)
                {
                    currentRoom.RemovePlayer(session.Player);
                }

                room.AddPlayer(session.Player);
                session.Player.Location = context.ArgumentString;
            }
        }
    }

    [Command("roomtitle", true, TickDelay.Instant)]
    class RoomTitleCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: roomtitle <new room title>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var room = RoomHelper.GetRoom(session.Player.Location);
            if (room != null)
            {
                room.Title = context.ArgumentString;
                RoomHelper.SaveRoom(room);
                session.WriteLine("Room title changed...");
            }
        }
    }

    [Command("roomdesc", true, TickDelay.Instant)]
    class DescriptionCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: roomdesc <new room description>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var room = RoomHelper.GetRoom(session.Player.Location);
            if (room != null)
            {
                room.Description = context.ArgumentString;
                RoomHelper.SaveRoom(room);
                session.WriteLine("Room description changed...");
            }
        }
    }

    /// <summary>
    /// creates an exit from the current room
    /// syntax: makeexit direction leadsto
    /// example: makeexit north void
    /// example: makeexit south <close> awesome room
    /// </summary>
    [Command("makeexit", true, TickDelay.Instant)]
    class MakeExitCommand : CallbackCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: makeexit north <close> room of awesomeness");
        }

        public override void Execute(Session session, CommandContext context)
        {
            string dummyString;
            Execute(session, context, out dummyString);
        }
        
        public override void Execute(Session session, CommandContext context, out string callbackCommand)
        {
            callbackCommand = "look";

            var currentRoom = RoomHelper.GetRoom(session.Player.Location);
            if (currentRoom != null)
            {
                try
                {
                    string direction = context.Arguments[0];
                    context.Arguments.Remove(direction);

                    if (!DirectionHelper.isValidDirection(direction))
                    {
                        session.WriteLine("{0} is not a valid direction", direction);
                        PrintSyntax(session);
                    }
                    if (currentRoom.HasExit(direction))
                    {
                        session.WriteLine("Room already has {0} exit", direction);
                        PrintSyntax(session);
                        return;
                    }

                    // handle doors
                    string openClose = context.Arguments[0];
                    bool isDoor;
                    bool isOpen;
                    if (openClose == "<open>" || openClose == "<close>")
                    {
                        isDoor = true;
                        isOpen = openClose == "<open>";
                        context.Arguments.Remove(openClose);
                    }
                    else
                    {
                        isDoor = false;
                        isOpen = true;
                    }

                    // at this point, direction has been removed. all that remains if the new room key/name
                    string dstRoomKey = string.Join(" ", context.Arguments).Trim().ToLower();
                    var dstRoom = RoomHelper.GetRoom(dstRoomKey);
                    if (dstRoom == null)
                    {
                        session.WriteLine("Room key not found: {0}", dstRoomKey);
                        PrintSyntax(session);
                        return;
                    }

                    // fixme: confirm dstRoom doesn't already have the opposite exit e.g. if creating
                    // north exit, dstRoom should not already have south exit

                    currentRoom.Exits.Add(direction, new RoomExit()
                        {
                            LeadsTo = dstRoomKey,
                            IsDoor = isDoor,
                            IsOpen = isOpen
                        });

                    string oppositeDirection = DirectionHelper.GetOppositeDirection(direction);

                    dstRoom.Exits.Add(oppositeDirection, new RoomExit()
                        {
                            LeadsTo = currentRoom.Key,
                            IsDoor = isDoor,
                            IsOpen = isOpen
                        });

                    RoomHelper.SaveRoom(currentRoom);
                    RoomHelper.SaveRoom(dstRoom);
                }
                catch
                {
                    PrintSyntax(session);
                }
            }
        }
    }
}
