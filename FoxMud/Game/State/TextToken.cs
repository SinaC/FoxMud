using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxMud.Game.State
{
    public enum TextTokenType
    {
        Eof,
        Newline,
        Word,
        Sep
    }

    public class TextToken
    {
        public TextTokenType Type { get; set; }
        public string Text { get; set; }        
    }
}
