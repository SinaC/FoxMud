using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Text
{
    interface TextTransformer
    {
        string Transform(string input);
    }
}
