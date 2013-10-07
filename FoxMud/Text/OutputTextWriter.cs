using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxMud.Text
{
    interface OutputTextWriter
    {
        void Write(string value);
        void Write(object value);
        void Write(string format, params object[] args);

        void WriteLine(string value);
        void WriteLine(object value);
        void WriteLine(string format, params object[] args);
    }
}
