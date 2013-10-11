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
        protected readonly Dictionary<ItemArgType, int> args = new Dictionary<ItemArgType, int>()
            {
                { ItemArgType.Name, 0 },
                { ItemArgType.Description, 1 },
                { ItemArgType.WearLocation, 2 },
                { ItemArgType.Weight, 3 },
                { ItemArgType.Value, 4 },
                { ItemArgType.MinDamage, 5 },
                { ItemArgType.MaxDamage, 6 },
                { ItemArgType.HpBonus, 7 },
                { ItemArgType.ArmorBonus, 8 },
                { ItemArgType.Keywords, 9 },
                
            };

        public void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: template \"name of item\" \"description\" <wear> <weight> <$> <min> <max> <hp> <armor> \"keywords\" ]");
            session.WriteLine("Syntax: makeitem \"name of item\" [ \"description\" <wear> <weight> <$> <min> <max> <hp> <armor> \"keywords\" ]");
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
                if (context.CommandName.ToLower() == "template" && context.Arguments.Count != args.Max(v => v.Value) + 1)
                {
                    session.WriteLine("template command expecets {0} arguments.", args.Max(v => v.Value) + 1);
                    PrintSyntax(session);
                    return;
                }

                // if only one arg, will be assumed to be "item key" 
                bool isGenericMakeitem = context.CommandName.ToLower() == "makeitem" && context.Arguments.Count == 1;

                Template newItem = null;

                // check whether item already exists
                newItem = Server.Current.Database.Get<Template>(context.Arguments[args[ItemArgType.Name]]);

                if (newItem != null && context.CommandName.ToLower() == "template")
                {
                    session.WriteLine("Aborting: item already templated: {0}", newItem.Key);
                    return;
                }

                if (context.CommandName.ToLower() == "template" || !isGenericMakeitem)
                    Validate(context.Arguments);

                if (context.CommandName.ToLower() == "template" || newItem == null)
                {
                    newItem = BuildItem(context.Arguments);
                    Server.Current.Database.Save(newItem);

                    // notify
                    session.WriteLine("Info: item templated: {0}...", newItem.Name);
                }
                
                if(context.CommandName.ToLower() == "makeitem")
                {
                    PlayerItem dupedItem = newItem.Copy();
                    
                    if (!isGenericMakeitem)
                    {
                        dupedItem.Name = context.Arguments[args[ItemArgType.Name]];
                        dupedItem.Description = context.Arguments[args[ItemArgType.Description]];
                        dupedItem.Keywords = context.Arguments[args[ItemArgType.Keywords]].Split(' ');
                        dupedItem.Weight = Convert.ToInt32(context.Arguments[args[ItemArgType.Weight]]);
                        dupedItem.Value = Convert.ToInt32(context.Arguments[args[ItemArgType.Value]]);
                        dupedItem.WearLocation = (Wearlocation) Enum.Parse(typeof (Wearlocation), context.Arguments[args[ItemArgType.WearLocation]]);
                        dupedItem.MinDamage = Convert.ToInt32(context.Arguments[args[ItemArgType.MinDamage]]);
                        dupedItem.MaxDamage = Convert.ToInt32(context.Arguments[args[ItemArgType.MaxDamage]]);
                        dupedItem.ArmorBonus = Convert.ToInt32(context.Arguments[args[ItemArgType.ArmorBonus]]);
                        dupedItem.HpBonus = Convert.ToInt32(context.Arguments[args[ItemArgType.HpBonus]]);
                    }

                    Server.Current.Database.Save(dupedItem);

                    // add to inventory
                    session.Player.Inventory[dupedItem.Key] = dupedItem.Name;
                    session.WriteLine("Info: item duplicated in inventory...");
                }
            }
            catch
            {
                PrintSyntax(session);
            }
        }

        private Template BuildItem(List<string> values)
        {
            return new Template()
                {
                    Name = values[args[ItemArgType.Name]],
                    Description = values[args[ItemArgType.Description]],
                    Keywords = values[args[ItemArgType.Keywords]].Split(' '),
                    WearLocation = (Wearlocation)Enum.Parse(typeof(Wearlocation), values[args[ItemArgType.WearLocation]]),
                    Weight = Convert.ToInt32(values[args[ItemArgType.Weight]]),
                    Value = Convert.ToInt32(values[args[ItemArgType.Value]]),
                    MinDamage = Convert.ToInt32(values[args[ItemArgType.MinDamage]]),
                    MaxDamage = Convert.ToInt32(values[args[ItemArgType.MaxDamage]]),
                    HpBonus = Convert.ToInt32(values[args[ItemArgType.HpBonus]]),
                    ArmorBonus = Convert.ToInt32(values[args[ItemArgType.ArmorBonus]]),
                };
        }

        private void Validate(List<string> values)
        {
            // wear location must match, or this will throw
            Enum.Parse(typeof (Wearlocation), values[args[ItemArgType.WearLocation]]);

            // unacceptable negative values
            if (Convert.ToInt32(values[args[ItemArgType.MinDamage]]) < 0 ||
                Convert.ToInt32(values[args[ItemArgType.MaxDamage]]) < 0 ||
                Convert.ToInt32(values[args[ItemArgType.Weight]]) < 0 ||
                Convert.ToInt32(values[args[ItemArgType.Value]]) < 0)
                throw new Exception();

            // min must be less than max
            if (Convert.ToInt32(values[args[ItemArgType.MinDamage]]) > Convert.ToInt32(values[args[ItemArgType.MaxDamage]]))
                throw new Exception();
        }
    }
}
