using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command
{
    [Command("inventory", false)]
    [Command("i", false)]
    class InventoryCommand : PlayerCommand
    {
        public void Execute(Session session, CommandContext context)
        {
            session.WriteLine("Your inventory:");
            // render like this, accounting for multiple items
            //      small knife (2)
            //      brass knuckles (1)
            foreach (var itemLine in session
                .Player.Inventory
                .GroupBy(i => i.Name)
                .Select(group => new
                    {
                        ItemName = group.Key,
                        Count = group.Count()
                    }))
            {
                session.WriteLine("\t{0} ({1})", itemLine.ItemName, itemLine.Count);
            }
        }
    }

}
