using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoxMud.Game.Command.Admin;

namespace FoxMud.Game.Command.Stat
{
    [Command("scorecard", false, 0)]
    class ScorecardCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: scorecard");
        }

        public override void Execute(Session session, CommandContext context)
        {
            var player = session.Player;
            session.WriteLine("`G--------------------------------------------------------------------------------");
            session.WriteLine("`G|Scorecard                                                                     |");
            session.WriteLine("`G--------------------------------------------------------------------------------");
            session.WriteLine("`G|`gName: `G{0,-20} `gGold: `Y${1,-44:N0}`G|", player.Forename, player.Gold, string.Empty);
            session.WriteLine("`G|`gLevel: `G{0,-4} `gExperience: `G{1,9:N0}`g/`G{2,-44:N0}`G|", player.Level, player.Experience,
                              ExperienceResolver.ExperienceRequired(player.Level + 1));
            session.WriteLine("`G--------------------------------------------------------------------------------");
            session.WriteLine("`G|`gSTR: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Strength, player.BaseStrength, string.Empty);
            session.WriteLine("`G|`gDEX: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Dexterity, player.BaseDexterity, string.Empty);
            session.WriteLine("`G|`gCON: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Constitution, player.BaseConstitution, string.Empty);
            session.WriteLine("`G|`gINT: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Intelligence, player.BaseIntelligence, string.Empty);
            session.WriteLine("`G|`gWIS: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Wisdom, player.BaseWisdom, string.Empty);
            session.WriteLine("`G|`gCHA: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Charisma, player.BaseCharisma, string.Empty);
            session.WriteLine("`G|`gLCK: `G{0,-2}`g[{1,-2}]{2,-67}`G|", player.Luck, player.BaseLuck, string.Empty);
            session.WriteLine("`G--------------------------------------------------------------------------------");
            session.WriteLine("`G|`gCurrent Location: `G{0,-60}`G|", RoomHelper.GetRoom(session.Player.Location).Title);
            session.WriteLine("`G--------------------------------------------------------------------------------");
        }
    }
}
