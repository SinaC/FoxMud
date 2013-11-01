using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Admin;

namespace FoxMud.Game.Command.Combat
{
    [Command("punch", false, TickDelay.Single, 1, "punch")]
    [Command("kick", false, TickDelay.Double, 1, "kick")]
    [Command("pummel", false, TickDelay.Triple, 2, "pummel")]
    class SkillCommands : PlayerCommand
    {
        private readonly string realCommandName;

        public SkillCommands(string realCommandName)
        {
            this.realCommandName = realCommandName.ToLower();
        }

        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: kick <target>");
            session.WriteLine("Syntax: punch <target>");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentString))
            {
                session.WriteLine("{0} what?", context.CommandName);
                return;
            }

            var arg1 = context.ArgumentString.ToLower();

            // command lookup, should never be null or command lookup will handle
            // level check should already be done by command lookup
            var skill = Server.Current.Database.Get<CombatSkill>(realCommandName);

            // skill check i.e. does player have it?
            if (!session.Player.Skills.ContainsKey(skill.Key))
            {
                session.WriteLine("You haven't practiced that yet.");
                return;
            }

            // status check, start combat if not already there
            if (session.Player.Status != GameStatus.Fighting && session.Player.Status != GameStatus.Standing)
            {
                session.WriteLine("You can't do that right now.");
                return;
            }

            // target check
            var room = RoomHelper.GetRoom(session.Player.Location);
            var target = room.GetNpcs().FirstOrDefault(n => n.Keywords.Contains(arg1));
            if (target == null)
            {
                session.WriteLine("You can't find that here.");
                return;
            }

            // combat check
            var fight = Server.Current.CombatHandler.FindFight(session.Player);
            if (fight == null)
            {
                // start fight, since npc is found
                fight = new Game.Combat();
                fight.AddFighter(session.Player);
                fight.AddMob(target);
                Server.Current.CombatHandler.StartFight(fight);
            }

            // do it
            if (Server.Current.Random.Next() < session.Player.Skills[skill.Key])
            {
                // get damage
                var damage = Server.Current.Random.Next(skill.MinDamage, skill.MaxDamage + 1);
                // hit
                target.HitPoints -= damage;

                // increase skill effectiveness
                if (session.Player.Skills[skill.Key] < skill.MaxEffectiveness)
                    session.Player.Skills[skill.Key] += skill.HitEffectivenessIncrease + target.HitPoints <= 0
                                                            ? skill.KillingBlowEffectivenessIncrease
                                                            : 0;

                // message
                session.WriteLine("You {0} {1} for {2} damage!", context.CommandName.ToLower(), target.Name, damage);
                // room message
                room.SendPlayers(
                    string.Format("{0}{1} {2} hits {3}!", session.Player.Forename,
                                  session.Player.Forename.ToLower().EndsWith("s") ? "'" : "s", skill.Key, target.Name),
                    session.Player, null,
                    session.Player);

                // dead check
                // this feels hacky since it sort of duplicates some of the work of Combat.DoPlayerHits()
                if (target.HitPoints <= 0)
                {
                    fight.RemoveFromCombat(target);
                    target.Die();
                    var deadText = string.Format("{0} is DEAD!!!", target.Name);
                    session.WriteLine(deadText);
                    room.SendPlayers(deadText, session.Player, null, session.Player);
                }
            }
            else
            {
                // message
                session.WriteLine("Your {0} misses {1}!", context.CommandName.ToLower(), target.Name);
                // room message
                room.SendPlayers(string.Format("{0}{1} {2} misses {3}!", session.Player.Forename,
                                               session.Player.Forename.ToLower().EndsWith("s") ? "'" : "s", skill.Key,
                                               target.Name),
                                 session.Player, null,
                                 session.Player);
                // increase skill effectiveness by less
                session.Player.Skills[skill.Key] += skill.MissEffectivenessIncrease;
            }
        }
    }
}
