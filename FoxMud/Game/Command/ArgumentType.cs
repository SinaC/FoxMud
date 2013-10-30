using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command
{
    public enum ItemArgType
    {
        Type,
        Name,
        Description,
        WearLocation,
        Weight,
        Value,
        HpBonus,
        ArmorBonus,
        Keywords,
    }

    public enum ContainerArgType
    {
        Name,
        Description,
        Keywords,
        Capacity,
        Weight,
        Value,
    }
}
