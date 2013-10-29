using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Text.Transformers
{
    class ColorCodeToAnsiTransformer : TextTransformer
    {
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

        public string Transform(string input)
        {
            var result = new StringBuilder();

            bool foundColorToken = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (!foundColorToken)
                {
                    if (input[i] == '`')
                    {
                        foundColorToken = true;
                        continue;
                    }

                    result.Append(input[i]);
                }
                else
                {
                    if (codeToColorMap.ContainsKey(input[i]))
                    {
                        result.Append(codeToColorMap[input[i]]);
                    }

                    foundColorToken = false;
                }
            }

            result.Append(codeToColorMap['0']);
            return result.ToString();
        }
    }
}
