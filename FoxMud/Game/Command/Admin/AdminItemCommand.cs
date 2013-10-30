using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Game.Item;

namespace FoxMud.Game.Command.Admin
{
    [Command("template", true, TickDelay.Instant)]
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
                { ItemArgType.HpBonus, 5 },
                { ItemArgType.ArmorBonus, 6 },
                { ItemArgType.Keywords, 7 },
                
            };

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: template \"name of item\" \"description\" <wear> <weight> <$> <hp> <armor> \"keywords\" ]");
            session.WriteLine("Syntax: makeitem \"name of item\" [ \"description\" <wear> <weight> <$> <hp> <armor> \"keywords\" ]");
        }

        public override void Execute(Session session, CommandContext context)
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
                    //PlayerItem dupedItem = newItem.Copy();
                    PlayerItem dupedItem = Mapper.Map<PlayerItem>(newItem);
                    
                    if (!isGenericMakeitem)
                    {
                        dupedItem.Name = context.Arguments[args[ItemArgType.Name]];
                        dupedItem.Description = context.Arguments[args[ItemArgType.Description]];
                        dupedItem.Keywords = context.Arguments[args[ItemArgType.Keywords]].Split(' ');
                        dupedItem.Weight = Convert.ToInt32(context.Arguments[args[ItemArgType.Weight]]);
                        dupedItem.Value = Convert.ToInt32(context.Arguments[args[ItemArgType.Value]]);
                        dupedItem.WearLocation = (Wearlocation) Enum.Parse(typeof (Wearlocation), context.Arguments[args[ItemArgType.WearLocation]]);
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
                    HpBonus = Convert.ToInt32(values[args[ItemArgType.HpBonus]]),
                    ArmorBonus = Convert.ToInt32(values[args[ItemArgType.ArmorBonus]]),
                };
        }

        private void Validate(List<string> values)
        {
            // wear location must match, or this will throw
            Enum.Parse(typeof (Wearlocation), values[args[ItemArgType.WearLocation]]);

            // unacceptable negative values (will throw FormatExceptions if they're not numbers
            if (Convert.ToInt32(values[args[ItemArgType.Weight]]) < 0 ||
                Convert.ToInt32(values[args[ItemArgType.Value]]) < 0)
                throw new Exception();
        }
    }

    /// <summary>
    /// both templates and duplicates an item into inventory
    /// </summary>
    [Command("container", true, TickDelay.Instant)]
    class ContainerCommand : PlayerCommand
    {
        protected readonly Dictionary<ContainerArgType, int> args = new Dictionary<ContainerArgType, int>()
            {
                { ContainerArgType.Name, 0 },
                { ContainerArgType.Description, 1 },
                { ContainerArgType.Keywords, 2 },
                { ContainerArgType.Capacity, 3 },
                { ContainerArgType.Weight, 4 },
                { ContainerArgType.Value, 5 },
            };

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: container \"name\" \"description\" \"keywords\" <capacity> <weight> <value> ");
            session.WriteLine("Syntax: container \"name\" (when container template exists)");
        }

        public override void Execute(Session session, CommandContext context)
        {
            // validate
            try
            {
                Template newItem = Server.Current.Database.Get<Template>(context.Arguments[args[ContainerArgType.Name]].ToLower());
                PlayerItem dupedItem;
                if (newItem != null)
                {
                    Duplicate(session, newItem);
                    return;
                }

                Validate(context.Arguments);
                
                if (newItem == null)
                {
                    // template
                    newItem = new Template()
                        {
                            ContainedItems = new Dictionary<string, string>(),
                            Description = context.Arguments[args[ContainerArgType.Description]],
                            Keywords = context.Arguments[args[ContainerArgType.Keywords]].Split(' '),
                            Name = context.Arguments[args[ContainerArgType.Name]],
                            Value = Convert.ToInt32(context.Arguments[args[ContainerArgType.Value]]),
                            WearLocation = Wearlocation.Container,
                            Weight = Convert.ToInt32(context.Arguments[args[ContainerArgType.Weight]]),
                        };

                    Server.Current.Database.Save(newItem);
                    session.WriteLine("Container templated...");
                }

                Duplicate(session, newItem);
            }
            catch
            {
                PrintSyntax(session);
            }
        }

        private void Duplicate(Session session, Template newItem)
        {
            PlayerItem dupedItem;
            dupedItem = Mapper.Map<PlayerItem>(newItem);
            Server.Current.Database.Save(dupedItem);

            // add to inventory
            session.Player.Inventory[dupedItem.Key] = dupedItem.Name;
            session.WriteLine("Container added to inventory...");
        }

        private void Validate(List<string> values)
        {
            Enum.Parse(typeof(Capacity), values[args[ContainerArgType.Capacity]]);

            if (Convert.ToInt32(values[args[ContainerArgType.Weight]]) < 0 ||
                Convert.ToInt32(values[args[ContainerArgType.Value]]) < 0)
                throw new Exception();
        }
    }

}
