using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;
using Newtonsoft.Json;

namespace FoxMud.Game.World
{
    class Area : Storable
    {
        [JsonConstructor]
        private Area(string name, List<string> rooms, int repopTime)
        {
            Name = name;
            Rooms = rooms;
            RepopTime = repopTime;
        }

        public Area()
        {
            Rooms = new List<string>();
            RepopQueue = new List<string>();
        }

        public string Key { get { return Name.ToLower(); } }
        public string Name { get; set; }
        public List<string> Rooms { get; private set; }
        public long RepopTime { get; set; }

        /// <summary>
        /// this "queue" is used when a mob dies. it's added here so the
        /// area knows to respawn it after its repop time
        /// </summary>
        [JsonIgnore]
        public List<string> RepopQueue { get; private set; }
    }
}
