using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Admin
{
    [Command("makemob", true)]
    class MakeMobCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: makemob <mob name>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                PrintSyntax(session);
                return;
            }

            var template = Server.Current.Database.Get<MobTemplate>(context.Arguments[0].ToLower());
            if (template != null)
            {
                session.WriteLine("{0} already exists...", context.Arguments[0]);
            }

            template = new MobTemplate()
                {
                    Aggro = false,
                    BaseArmor = 0,
                    BaseCharisma = 0,
                    BaseConstitution = 0,
                    BaseDamRoll = 1,
                    BaseDexterity = 0,
                    BaseHitRoll = 1,
                    BaseHp = 10,
                    BaseIntelligence = 0,
                    BaseLuck = 0,
                    BaseStrength = 0, 
                    BaseWisdom = 0,
                    Description = "fix this description",
                    HitPoints = 10,
                    IsShopkeeper = false,
                    Location = string.Empty,
                    MinimumTalkInterval = 0,
                    Name = context.Arguments[0],
                    RespawnRoom = string.Empty,
                    Status = GameStatus.Standing,
                };

            Server.Current.Database.Save(template);

            session.WriteLine("Mob template saved. Please modify stats on disk");
        }
    }
}
