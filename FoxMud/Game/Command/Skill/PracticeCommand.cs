using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Admin;
using FoxMud.Game.Command.Combat;
using FoxMud.Game.World;

namespace FoxMud.Game.Command.Skill
{
    [Command("practice", false, TickDelay.Instant)]
    class PracticeCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: practice");
            session.WriteLine("Syntax: practice <skill>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var skillsByLevel = Server.Current.CommandLookup.FindCommands(session.Player.Level);

            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                // show list of skills
                foreach (
                    var command in
                        skillsByLevel
                            .Where(c => c.Command.GetType() == typeof (SkillCommands)))
                {
                    session.WriteLine("`B{0,-15}`C{1:#0%}", command.CommandName,
                                      session.Player.Skills.ContainsKey(command.CommandName.ToLower())
                                          ? session.Player.Skills[command.CommandName.ToLower()]
                                          : 0);
                }

                session.WriteLine("You have {0} skill points left.", session.Player.SkillPoints);

                return;
            }

            if (session.Player.SkillPoints == 0)
            {
                session.WriteLine("You need more skill points to practice that.");
                return;
            }

            var room = RoomHelper.GetRoom(session.Player.Location);
            if (!room.CanPracticeHere)
            {
                session.WriteLine("You can't practice here.");
                return;
            }

            // validate skill
            if (session.Player.Skills.ContainsKey(context.ArgumentString.ToLower()))
            {
                session.WriteLine("You've already practiced that.");
                return;
            }

            var arg1 = context.ArgumentString.ToLower();
            var skill = Server.Current.CombatSkills.FirstOrDefault(s => s.Name.ToLower() == arg1);
            if (skill == null || !skillsByLevel.Any(s => s.CommandName.ToLower() == arg1))
            {
                session.WriteLine("Can't practice that.");
                return;
            }

            // skill found, practice
            session.Player.Skills[skill.Key] = skill.Effectiveness;
            session.Player.SkillPoints--;
            session.WriteLine("You practice {0}.", skill.Name);

            // save player
            Server.Current.Database.Save(session.Player);
        }
    }
}
