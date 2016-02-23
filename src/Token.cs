using System;

namespace Assembler
{
    internal class Token
    {
        public string str_value;
        public object obj;

        public int lineNum;
        public int linePos;
        public string source;

        public Type type
        {
            get { return obj.GetType(); }
        }

        public static implicit operator string(Token t)
        {
            return t.str_value;
        }

        public override bool Equals(object o)
        {
            return str_value.Equals(o);
        }

        public override int GetHashCode()
        {
            return str_value.GetHashCode();
        }

        public override string ToString()
        {
            return str_value;
        }
    }
}
