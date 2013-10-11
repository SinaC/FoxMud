using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Item
{
    enum Wearlocation
    {
        Head,
        Shoulders,
        LeftHand,
        RightHand,
        BothHands,
        Torso,
        Arms,
        Waist,
        Legs,
        Feet,
        None
    }

    interface Equipable
    {
        void Equip(Player player);
        void Unequip(Player player);
    }
}
