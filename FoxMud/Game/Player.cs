﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using FoxMud.Db;
using FoxMud.Text;
using Newtonsoft.Json;

namespace FoxMud.Game
{
    enum PlayerGender
    {
        Male,
        Female,
        Unknown
    }

    class Player : Storable
    {
        private string _passwordHash;

        [JsonConstructor]
        private Player(string name, string passwordHash, bool isAdmin, string prompt)
        {
            Forename = name;
            _passwordHash = passwordHash;
            IsAdmin = isAdmin;
            Prompt = prompt;
        }

        public Player()
        {
            IsAdmin = false;
        }

        public string Key
        {
            get { return Forename.ToLower(); }
        }

        [JsonIgnore]
        public OutputTextWriter OutputWriter { get; set; }

        public string Forename { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public PlayerGender Gender { get; set; }
        public bool Approved { get; set; }
        public string PasswordHash
        {
            get { return _passwordHash; }
            set { _passwordHash = Hash(value); }
        }
        public bool IsAdmin { get; set; }
        public string Prompt { get; set; }

        private static string Hash(string value)
        {
            using (SHA256 hashMethod = SHA256.Create())
            {
                var toHash = Encoding.ASCII.GetBytes(value);
                var hashedBytes = hashMethod.ComputeHash(toHash);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool CheckPassword(string password)
        {
            var hash = Hash(password);

            return PasswordHash == hash;
        }

        // todo bug#30 add ability to change password

        public string GetOtherPlayerDescription(Player subject)
        {
            if (subject == this)
                return this.Forename;

            return subject.ShortDescription;
        }

        public void Send(string format, Player subject)
        {
            Send(format, subject, null);
        }

        public void Send(string format, Player subject, Player target)
        {
            OutputWriter.WriteLine(StringHelpers.BuildString(format, this, subject, target));
            WritePrompt();
        }

        public void WritePrompt()
        {
            OutputWriter.WriteLine(Prompt);
        }

        public static string NameToKey(string name)
        {
            return name.ToLower();
        }
    }
}
