using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Item;

namespace FoxMud.Game.Command.Admin
{
    [Command("template", true)]
    [Command("makeitem", true)]
    class TemplateCommand : PlayerCommand
    {
        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: template <weapon|armor> \"name of item\" \"description\" <wear> <weight> <$> <min> <max>");
            session.WriteLine("Syntax: makeitem <weapon|armor> \"name of item\" \"description\" <wear> <weight> <$> <hp> <armor>");
        }

        public void Execute(Session session, CommandContext context)
        {
            /*
             * order of arguments:
             * 0.   weapon|armor
             * 1.   name
             * 2.   description
             * 3.   wear location
             * 4.   weight
             * 5.   value
             * 6.   min damage|hp bonus
             * 7.   max damage|armor bonus
             */

            try
            {
                if (context.Arguments.Count != 8)
                {
                    PrintSyntax(session);
                    return;
                }

                var type = context.Arguments[0];

                // create item
                Template newItem;
                if (type == "weapon")
                    newItem = Server.Current.Database.Get<TemplateWeapon>(context.Arguments[1]);
                else
                    newItem = Server.Current.Database.Get<TemplateArmor>(context.Arguments[1]);

                if (newItem != null && context.CommandName.ToLower() == "template")
                {
                    session.WriteLine("Item already templated: {0}", newItem.Key);
                    return;
                }

                if (newItem != null) // must be makeitem'ing on an already existent item
                {
                    session.WriteLine("Item found...skipping template: {0}", newItem.Key);
                }
                else // template doesn't exist, so it needs to be templated regardless of command
                {
                    // template
                    switch (context.Arguments[0].ToLower())
                    {
                        case "weapon":
                            newItem = BuildWeapon(context.Arguments);
                            Server.Current.Database.Save(newItem as TemplateWeapon);
                            break;
                        case "armor":
                            newItem = BuildArmor(context.Arguments);
                            Server.Current.Database.Save(newItem as TemplateArmor);
                            break;
                        default:
                            throw new Exception();
                    }

                    // notify
                    session.WriteLine("Item templated: {0}...", newItem.Name);
                }

                // duplicate the item if makeitem
                if (context.CommandName.ToLower() == "makeitem")
                {
                    PlayerItem dupedItem;

                    // this whole section feels hacky--is there a better choice of inheritance/copying to 
                    // duplicate these properties? maybe using AutoMapper or something...
                    if (type == "weapon")
                    {
                        dupedItem = (newItem as TemplateWeapon).Copy();
                        // add command-specific properties
                        if (context.Arguments.Count >= 5)
                            (dupedItem as ItemWeapon).MinDamage = Convert.ToInt32(context.Arguments[4]);
                        if (context.Arguments.Count >= 5)
                            (dupedItem as ItemWeapon).MaxDamage = Convert.ToInt32(context.Arguments[5]);

                        Server.Current.Database.Save(dupedItem as ItemWeapon);
                    }
                    else
                    {
                        dupedItem = (newItem as TemplateArmor).Copy();
                        // add command-specific properties
                        (dupedItem as ItemArmor).HpBonus = Convert.ToInt32(context.Arguments[4]);
                        (dupedItem as ItemArmor).ArmorBonus = Convert.ToInt32(context.Arguments[5]);
                        Server.Current.Database.Save(dupedItem as ItemArmor);
                    }

                    // add to inventory
                    session.Player.Inventory[dupedItem.Key] = dupedItem.Name;
                }
            }
            catch
            {
                PrintSyntax(session);
            }
        }

        private Template BuildArmor(List<string> args)
        {
            return new TemplateArmor()
                {
                    Name = args[1],
                    Description = args[2],
                    WearLocation = (Wearlocation) Enum.Parse(typeof(Wearlocation), args[3]),
                    Weight = Convert.ToInt32(args[4]),
                    Value = Convert.ToInt32(args[5]),
                    HpBonus = Convert.ToInt32(args[6]),
                    ArmorBonus = Convert.ToInt32(args[7]),
                };
        }

        private Template BuildWeapon(List<string> args)
        {
            return new TemplateWeapon()
                {
                    Name = args[1],
                    Description = args[2],
                    WearLocation = (Wearlocation) Enum.Parse(typeof(Wearlocation), args[3]),
                    Weight = Convert.ToInt32(args[4]),
                    Value = Convert.ToInt32(args[5]),
                    MinDamage = Convert.ToInt32(args[6]),
                    MaxDamage = Convert.ToInt32(args[7])
                };
        }
    }
}
