using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Stat
{
    [Command("reroll", false, TickDelay.Reroll, 0)]
    class RerollCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: reroll");
        }

        public override void Execute(Session session, CommandContext context)
        {
            if (session.Player.Level != 0)
            {
                session.WriteLine("You can't do that now.");
                return;
            }

            var newStrength = StatResolver.RollStat();
            var newDexterity = StatResolver.RollStat();
            var newConstitution = StatResolver.RollStat();
            var newIntelligence = StatResolver.RollStat();
            var newWisdom = StatResolver.RollStat();
            var newCharisma = StatResolver.RollStat();
            var newLuck = StatResolver.RollStat();

            session.Player.BaseStrength = newStrength;
            session.Player.BaseDexterity = newDexterity;
            session.Player.BaseConstitution = newConstitution;
            session.Player.BaseIntelligence = newIntelligence;
            session.Player.BaseWisdom = newWisdom;
            session.Player.BaseCharisma = newCharisma;
            session.Player.BaseLuck = newLuck;

            session.WriteLine("Rerolling...\nSTR: {0}\nDEX: {1}\nCON: {2}\nINT: {3}\nWIS: {4}\nCHA: {5}\nLCK: {6}",
                              newStrength, newDexterity, newConstitution, newIntelligence, newWisdom, newCharisma,
                              newLuck);
        }
    }
}
