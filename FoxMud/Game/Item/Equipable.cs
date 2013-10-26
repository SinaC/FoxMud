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
        None,
        Container,
        Key,
        Corpse,
    }

    enum Capacity
    {
        Small = 30,
        Medium = 250,
        Large = 750,
    }

    interface Equipable
    {
        void Equip(Session player);
        void Unequip(Session player);
    }
}
