using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Item
{
    class TemplateWeapon : Template
    {
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }

        public override PlayerItem Copy()
        {
            var newItem = new ItemWeapon(this);
            return newItem;
        }
    }

    class ItemWeapon : PlayerItem
    {
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }

        public ItemWeapon(TemplateWeapon template)
            :base(template)
        {
            MinDamage = template.MinDamage;
            MaxDamage = template.MaxDamage;
        }
    }
}
