using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud.Game
{
    /// <summary>
    /// basically copies the list of Template items into a list of generated PlayerItems
    /// </summary>
    class NonPlayerInventoryResolver : ValueResolver<MobTemplate, Dictionary<string, string>>
    {
        protected override Dictionary<string, string> ResolveCore(MobTemplate source)
        {
            var items = new Dictionary<string, string>();

            foreach (var key in source.Inventory)
            {
                var item = Server.Current.Database.Get<Template>(key);
                var dupedItem = Mapper.Map<PlayerItem>(item);
                Server.Current.Database.Save(dupedItem);
                items[dupedItem.Key] = dupedItem.Name;
            }

            return items;
        }
    }

    class NonPlayerEquippedResolver : ValueResolver<MobTemplate, Dictionary<Wearlocation, WearSlot>>
    {
        protected override Dictionary<Wearlocation, WearSlot> ResolveCore(MobTemplate source)
        {
            var items = new Dictionary<Wearlocation, WearSlot>();

            foreach (var item in source.Equipped)
            {
                var template = Server.Current.Database.Get<Template>(item.Value);
                var dupedItem = Mapper.Map<PlayerItem>(template);
                Server.Current.Database.Save(dupedItem);
                items[item.Key] = new WearSlot()
                {
                    Key = dupedItem.Key,
                    Name = dupedItem.Name,
                };
            }

            return items;
        }
    }
}
