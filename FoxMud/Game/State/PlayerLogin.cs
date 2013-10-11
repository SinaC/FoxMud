using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxMud.Game.State
{
    class PlayerLogin : SessionStateBase
    {
        private enum State
        {
            RequestUserName,
            RequestPassword
        }

        private State currentState;
        private string playerName;
        private string password;
        private Player player;
        private const int MaxPasswordAttempts = 3;
        private int PasswordAttempts = 0;

        private void changeStateTo(State state)
        {
            currentState = state;

            switch (state)
            {
                case State.RequestUserName:
                    Session.Write("Username (type any name to create a character): ");
                    break;
                case State.RequestPassword:
                    // todo bug#28 turn off echo for password input
                    Session.Write("Password: ");
                    break;
            }
        }

        private Player GetPlayer(string playerName)
        {
            var findPlayer = Server.Current.Database.Get<Player>(Player.NameToKey(playerName));

            return findPlayer;
        }

        public override void OnStateEnter()
        {
            if (player != null)
            {
                // new player has been created, enter world
                Session.PushState(new EnterWorld(player));
            }

            changeStateTo(State.RequestUserName);

            base.OnStateEnter();
        }

        public override void OnInput(string input)
        {
            switch (currentState)
            {
                case State.RequestUserName:
                    playerName = input;
                    player = GetPlayer(playerName);

                    if (player == null)
                    {
                        // todo: bug#29 validate usernames

                        if (!ValidateUsername(input))
                        {
                            Session.WriteLine("Invalid username");
                            changeStateTo(State.RequestUserName);
                            break;
                        }

                        // create new character
                        Session.PushState(new CreateNewPlayer(playerName));
                        break;
                    }

                    // finally, prompt for password
                    changeStateTo(State.RequestPassword);
                    break;

                case State.RequestPassword:
                    password = input;
                    if (!player.CheckPassword(password))
                    {
                        Session.WriteLine("Invalid password");
                        changeStateTo(State.RequestPassword);
                        PasswordAttempts++;
                        if (PasswordAttempts >= MaxPasswordAttempts)
                            Session.End();
                        break;
                    }

                    var session = Server.Current.SessionMonitor.GetPlayerSession(player);

                    if (session != null)
                    {
                        Session.WriteLine("Taking control of another active session...");
                        Session.HandConnectionTo(session);
                    }

                    Session.Player = player;
                    Session.PushState(new EnterWorld(player));
                    break;
            }

            base.OnInput(input);
        }

        public static bool ValidateUsername(string input)
        {
            return Regex.Match(input, @"^[a-zA-Z]{3,20}$").Success;
        }
    }
}
