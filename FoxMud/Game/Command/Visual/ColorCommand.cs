using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Game.Command.Visual
{
    [Command("color", false)]
    class ColorCommand : PlayerCommand
    {
        public override void PrintSyntax(Session session)
        {
            session.WriteLine("Syntax: color");
        }

        public override void Execute(Session session, CommandContext context)
        {
            session.WriteLine("`000000");
            session.WriteLine("`kkkkkk");
            session.WriteLine("`rrrrrr");
            session.WriteLine("`gggggg");
            session.WriteLine("`yyyyyy");
            session.WriteLine("`bbbbbb");
            session.WriteLine("`mmmmmm");
            session.WriteLine("`cccccc");
            session.WriteLine("`wwwwww");
            session.WriteLine("`KKKKKK");
            session.WriteLine("`RRRRRR");
            session.WriteLine("`GGGGGG");
            session.WriteLine("`YYYYYY");
            session.WriteLine("`BBBBBB");
            session.WriteLine("`MMMMMM");
            session.WriteLine("`CCCCCC");
            session.WriteLine("`WWWWWW");
        }

        private static Dictionary<char, string> codeToColorMap = new Dictionary<char, string>()
        {
            { '0', "\x001B[0m" },
            { 'k', "\x001B[0;30m" },            
            { 'r', "\x001B[0;31m" },
            { 'g', "\x001B[0;32m" },
            { 'y', "\x001B[0;33m" },
            { 'b', "\x001B[0;34m" },
            { 'm', "\x001B[0;35m" },
            { 'c', "\x001B[0;36m" },
            { 'w', "\x001B[0;37m" },
            { 'K', "\x001B[1;30m" },            
            { 'R', "\x001B[1;31m" },
            { 'G', "\x001B[1;32m" },
            { 'Y', "\x001B[1;33m" },
            { 'B', "\x001B[1;34m" },
            { 'M', "\x001B[1;35m" },
            { 'C', "\x001B[1;36m" },
            { 'W', "\x001B[1;37m" },
        };
    }
}
