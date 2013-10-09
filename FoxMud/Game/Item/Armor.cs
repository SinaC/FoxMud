using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Item
{
    class TemplateArmor : Template
    {
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }
    }

    class ItemArmor : PlayerItem
    {
        public int HpBonus { get; set; }
        public int ArmorBonus { get; set; }

        public ItemArmor(TemplateArmor template)
            :base(template)
        {
            HpBonus = template.HpBonus;
            ArmorBonus = template.ArmorBonus;
        }
    }
}
