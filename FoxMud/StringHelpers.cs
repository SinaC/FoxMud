using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxMud.Game;
using FoxMud.Game.Item;

namespace FoxMud
{
    static class StringHelpers
    {
        public static string ReadCommandLinePart(this string input, out string part)
        {
            bool startFound = false;
            bool isReading = false;
            char sep = ' ';
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (!startFound)
                {
                    if (char.IsWhiteSpace(input[i]))
                        continue;
                    else
                    {
                        startFound = true;
                        i--;
                        continue;
                    }
                }

                if (!isReading)
                {
                    isReading = true;

                    if (input[i] == '"' || input[i] == '\'')
                    {
                        sep = input[i];
                        continue;
                    }

                    i--;
                    continue;
                }

                if (input[i] != sep)
                {
                    result.Append(input[i]);
                }
                else
                {
                    i++;
                    part = result.ToString();
                    if (i == input.Length)
                        return string.Empty;

                    return input.Substring(i);
                }
            }

            part = result.ToString();
            return string.Empty;
        }

        private static string SelectOnGender(Player observer, Player player, string male, string female, string unknown, string ob)
        {
            if (observer == player)
                return ob;

            switch (player.Gender)
            {
                case PlayerGender.Male:
                    return male;
                case PlayerGender.Female:
                    return female;
                default:
                    return unknown;
            }
        }

        private static void ReplaceToken(StringBuilder result, char c, Player observer, Player subject, Player target)
        {
            switch (c)
            {
                case 'o':                    
                    result.Append(SelectOnGender(observer, subject, "his", "her", "it's", "your"));
                    break;
                case 'O':
                    result.Append(SelectOnGender(observer, target, "his", "her", "it's", "your"));
                    break;
                case 'p':
                    result.Append(SelectOnGender(observer, subject, "him", "her", "it", "you"));
                    break;
                case 'P':
                    result.Append(SelectOnGender(observer, target, "him", "her", "it", "you"));
                    break;
                case 'n':
                    result.Append(SelectOnGender(observer, subject, "he", "she", "it", "you"));
                    break;
                case 'N':
                    result.Append(SelectOnGender(observer, target, "he", "she", "it", "you"));
                    break;
                case 'd':
                    result.Append(observer.GetOtherPlayerDescription(subject));
                    break;
                case 'D':
                    result.Append(observer.GetOtherPlayerDescription(target));
                    break;
                case 'y':
                    if (subject == observer)
                        result.Append("you");
                    else
                        result.Append(observer.GetOtherPlayerDescription(subject));
                    break;
                case 'Y':
                    if (target == observer)
                        result.Append("you");
                    else
                        result.Append(observer.GetOtherPlayerDescription(target));
                    break;
                    
            }
        }

        public static string BuildString(string format, Player observer, Player subject)
        {
            return BuildString(format, observer, subject, null);
        }

        public static string BuildString(string format, Player observer, Player subject, Player target)
        {
            // his, her, it's, your     %o
            // him, her, it, you        %p
            // he, she, it, you         %n
            // description / name       %d

            // lower case means subject, upper case means target
            //"%d opens %o bag and hands %D a melon"

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < format.Length; i++)
            {
                if (format[i] == '%')
                {
                    i++;                    
                    ReplaceToken(result, format[i], observer, subject, target);
                    continue;
                }

                result.Append(format[i]);
            }

            return result.ToString();
        }

        public static string[] GetKeywords(string value)
        {
            return value.Split(new char[] { ' ', '!', '?', ',', '.', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string GetWearLocation(Wearlocation loc)
        {
            switch (loc)
            {
                case Wearlocation.Arms:
                    return "Arms";
                case Wearlocation.BothHands:
                    return "Both Hands";
                case Wearlocation.Container:
                    return "Container";
                case Wearlocation.Feet:
                    return "Feet";
                case Wearlocation.Head:
                    return "Head";
                case Wearlocation.Key:
                    return "Key";
                case Wearlocation.LeftHand:
                    return "Left Hand";
                case Wearlocation.Legs:
                    return "Legs";
                case Wearlocation.None:
                    return "None";
                case Wearlocation.RightHand:
                    return "Right Hand";
                case Wearlocation.Shoulders:
                    return "Shoulders";
                case Wearlocation.Torso:
                    return "Torso";
                case Wearlocation.Waist:
                    return "Waist";
                default:
                    return string.Empty;
            }
        }

        public static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text.Substring(0, 1).ToUpper() + text.Substring(1);
        }

        public static string AutoTruncate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // get substring <= 80, remove from text, .trim() + "\r\n"
            if (!string.IsNullOrEmpty(text) && text.Length < 80)
                return text;

            var line = text.Substring(0, 80);
            line = line.Substring(0, line.LastIndexOf(' ')).Trim();
            // remove from text
            text = text.Remove(0, line.Length).Trim();

            return line + "\r\n" + AutoTruncate(text);
        }
    }
}
