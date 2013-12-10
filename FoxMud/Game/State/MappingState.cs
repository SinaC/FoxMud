using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command;
using FoxMud.Game.World;

namespace FoxMud.Game.State
{
    class MappingState : SessionStateBase
    {
        enum MappingStatus
        {
            Walking,
            NeedsTitle,
        }

        private MappingStatus _status;
        private string _direction;

        public MappingState()
        {
            _status = MappingStatus.Walking;
        }

        public override void OnInput(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                TryExecuteCommand(input);
            }

            base.OnInput(input);
        }

        public override void OnStateInitialize()
        {
            Session.WriteLine("Entering mapping mode...");
            Session.WriteLine("Walk in a direction `Rwithout`R an exit to create a room automatically.");
            Session.WriteLine("Type `Gexitmapping`G to get out of mapping mode...");

            base.OnStateInitialize();
        }

        public override void OnStateShutdown()
        {
            Session.WriteLine("`RExiting mapping mode...");

            base.OnStateShutdown();
        }

        private void TryExecuteCommand(string input)
        {
            if (input.ToLower() == "exitmapping")
            {
                Session.PopState();
                return;
            }

            var room = RoomHelper.GetPlayerRoom(Session.Player);

            if (_status == MappingStatus.Walking)
            {
                var direction = DirectionHelper.GetDirectionWord(input);

                if (string.IsNullOrEmpty(direction))
                {
                    Session.WriteLine("That's not a direction.");
                    return;
                }

                if (room.HasExit(direction))
                {
                    var commandInfo = Server.Current.CommandLookup.FindCommand(direction, Session.Player);
                    commandInfo.Command.Execute(Session, CommandContext.Create(direction));
                    return;
                }

                // set mode to request title
                Session.WriteLine("`RCreating exit, Please enter a title...");
                _status = MappingStatus.NeedsTitle;
                _direction = direction;
            }
            else
            {
                // user is inputting a title
                var checkRoom = Server.Current.Database.Get<Room>(input.ToLower());
                var calculatedKey = input.ToLower();

                if (checkRoom != null)
                {
                    calculatedKey = RoomHelper.GenerateKey(input);
                    Session.WriteLine("`RRoom already exists. Using title: `G{0}`R.", calculatedKey);
                }

                var newRoom = new Room()
                {
                    Area = room.Area,
                    Description = RoomHelper.GetDefaultRoomDescription(),
                    Key = calculatedKey,
                    Title = input,
                };

                newRoom.Exits.Add(DirectionHelper.GetOppositeDirection(_direction), new RoomExit()
                {
                    IsDoor = false,
                    IsOpen = true,
                    LeadsTo = room.Key
                });

                Server.Current.Database.Save(newRoom);

                room.Exits.Add(_direction, new RoomExit()
                {
                    IsDoor = false,
                    IsOpen = true,
                    LeadsTo = newRoom.Key
                });

                Server.Current.Database.Save(room);

                var commandInfo = Server.Current.CommandLookup.FindCommand(_direction, Session.Player);
                commandInfo.Command.Execute(Session, CommandContext.Create(_direction));
                _status = MappingStatus.Walking;
            }
        }

        public void Move(string direction)
        {
            // if exit exists, just walk

            // else, create exit, goto room, request title
        }
    }
}
