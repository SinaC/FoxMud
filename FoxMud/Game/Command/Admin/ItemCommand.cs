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
        private void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: template <weapon|armor> \"name of item\" <property, property, ...>");
            session.WriteLine("Syntax: makeitem <weapon|armor> \"name of item\" <property, property, ...>");
        }

        public void Execute(Session session, CommandContext context)
        {
            try
            {
                var type = context.Arguments[0];

                // create item
                Template newItem;
                if (type == "weapon")
                    newItem = Server.Current.Database.Get<TemplateWeapon>(context.Arguments[1].Replace("\"", string.Empty));
                else
                    newItem = Server.Current.Database.Get<TemplateArmor>(context.Arguments[1].Replace("\"", string.Empty));

                if (newItem != null && context.CommandName.ToLower() == "template")
                {
                    session.WriteLine("Item already templated: {0}", newItem.Key);
                    return;
                }

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

                // duplicate the item if makeitem
                if (context.CommandName.ToLower() == "makeitem")
                {
                    session.WriteLine("makeitem not yet implemented...");

                    // duplicate item
                    if (newItem is TemplateWeapon)
                    {

                    }
                    else
                    {
                        
                    }

                    // add to inventory

                    // save
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
                    Description = "This plain armor has no description yet.",
                    HpBonus = Convert.ToInt32(args[4]),
                    ArmorBonus = Convert.ToInt32(args[5]),
                    Name = args[1].Replace("\"", string.Empty),
                    Value = Convert.ToInt32(args[3]),
                    WearLocation = Wearlocation.None,
                    Weight = Convert.ToInt32(args[2])
                };
        }

        private Template BuildWeapon(List<string> args)
        {
            return new TemplateWeapon()
                {
                    Description = "This plain weapon has no description yet.",
                    MaxDamage = Convert.ToInt32(args[4]),
                    MinDamage = Convert.ToInt32(args[5]),
                    Name = args[1].Replace("\"", string.Empty),
                    Value = Convert.ToInt32(args[3]),
                    WearLocation = Wearlocation.None,
                    Weight = Convert.ToInt32(args[2])
                };
        }
    }
}
