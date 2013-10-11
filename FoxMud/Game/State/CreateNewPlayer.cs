using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.State
{
    class CreateNewPlayer : SessionStateBase
    {
        private enum State
        {
            EnterForename,
            EnterPassword,
            EnterShortDescription,
            EnterDescription,
            SelectGender,
            Finished
        }

        private State currentState;
        private string forename;
        private string password;
        private string shortDescription;
        private string description;
        private PlayerGender gender;
        private TextEditor textEditor;

        // is it necessary to have the redundant properties on this object?
        private Player player;

        public CreateNewPlayer(string playerName)
        {
            forename = playerName;

            textEditor = new TextEditor();
            textEditor.Description =
                "Please enter your character description: \r\n" +
                "This is a long multiline description of what your character looks like.\r\n" +
                "Try to be as descriptive as possible while keeping the description (after \r\n" +
                "formatting) to a page of text (23 lines).\r\n" +
                "You should not use you characters name in the description, as someone can't\r\n" +
                "infer that from just looking at you.\r\n" +
                "Descriptions of clothing and gear should not be in here, as what you are\r\n" +
                "wearing will be determined by the equipment system in game.\r\n" +
                "You should avoid subjective descriptions and it should be written in the third \r\n" +
                "person while avoiding addressing the reader directly with words like \"you\".\r\n\rn" +
                "I suggest using your operating systems text editor to write your description\r\n" +
                "so you can edit easily and make sure it is correct before pasting the final\r\n" +
                "product here and formatting it by typing the single letter command (f)\r\n\r\n" +
                "Before saving (s) make sure you have formatted your text (f).";
        }

        private void changeStateTo(State state)
        {
            currentState = state;

            switch (state)
            {
                case State.EnterForename:
                    Session.WriteLine("{0} appears to be a new name, please confirm this player's forename.\n", forename);
                    Session.WriteLine("Please enter your character's forename: ");
                    break;

                case State.EnterPassword:
                    Session.WriteLine("Enter a password: ");
                    break;

                case State.SelectGender:
                    Session.WriteLine("Please select your character's gender (m/f): ");
                    break;

                case State.EnterShortDescription:
                    Session.WriteLine("Your short description is a simple one line visual description of your");
                    Session.WriteLine("character. This is what other people see when they don't know, or haven't");
                    Session.WriteLine("remembered your name.");
                    Session.WriteLine("Don't add descriptions of clothing as what you are wearing is determined by the");
                    Session.WriteLine("in-game equipment system.");
                    Session.WriteLine("This description should be valid regardless what state you character is in, be");
                    Session.WriteLine("it awake, unconcious, asleep, angry, happy, etc.");
                    Session.WriteLine("Example: a tall, dark haired man");
                    Session.WriteLine("Please enter your character's short description:");
                    break;

                case State.EnterDescription:
                    Session.PushState(textEditor);
                    break;

                case State.Finished:
                    CreateAndSavePlayer();
                    Session.PushState(new EnterWorld(player));
                    break;
            }
        }

        private void CreateAndSavePlayer()
        {
            player = new Player()
            {
                Forename = forename,
                PasswordHash = password, // this actually hashes the password once in the mutator
                ShortDescription = shortDescription,
                Description = description,
                Location = Server.StartRoom,
                Approved = Server.AutoApprovedEnabled,
                Gender = gender,
                Prompt = ">"
            };

            Server.Current.Database.Save(player);
        }

        public override void OnStateEnter()
        {
            switch (currentState)
            {
                case State.EnterDescription:
                    if (!textEditor.Success)
                    {
                        Session.WriteLine("You must give you character a description");
                        Session.PushState(textEditor);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(textEditor.Result))
                    {
                        Session.WriteLine("You must give you character a description");
                        Session.PushState(textEditor);
                        return;
                    }
                    description = textEditor.Result;
                    changeStateTo(State.EnterShortDescription);
                    break;
                default:
                    changeStateTo(State.EnterForename);
                    break;
            }

            base.OnStateEnter();
        }

        public override void OnInput(string input)
        {
            switch (currentState)
            {
                case State.EnterForename:
                    if (!PlayerLogin.ValidateUsername(input))
                    {
                        Session.WriteLine("Invalid username");
                        changeStateTo(State.EnterForename);
                        break;
                    }

                    forename = capitalizeForename(input);
                    changeStateTo(State.EnterPassword);
                    break;

                case State.EnterPassword:
                    password = input; 
                    changeStateTo(State.SelectGender);
                    break;

                case State.SelectGender:
                    if (input.ToLower() == "m")
                    {
                        gender = PlayerGender.Male;
                        changeStateTo(State.EnterDescription);
                    }
                    else if (input.ToLower() == "f")
                    {
                        gender = PlayerGender.Female;
                        changeStateTo(State.EnterDescription);
                    }
                    else
                    {
                        Session.WriteLine("Invalid Gender");
                        changeStateTo(State.SelectGender);
                    }
                    break;

                case State.EnterShortDescription:
                    shortDescription = input;
                    changeStateTo(State.Finished);
                    break;
            }

            base.OnInput(input);
        }

        private string capitalizeForename(string input)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                string newFirstCharacter = input[0].ToString().ToUpper();
                string newRestOfName = input.Substring(1).ToLower();
                return newFirstCharacter + newRestOfName;
            }

            return input;
        }
    }
}
