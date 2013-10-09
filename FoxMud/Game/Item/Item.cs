using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Db;

namespace FoxMud.Game.Item
{
    class Item : Storable
    {
        public string Key
        {
            get { return Name.ToLower(); }
        }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
