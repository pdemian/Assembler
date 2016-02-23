using System;
using System.Collections.Generic;

namespace Assembler
{
    internal class Instruction
    {
        public string mnemonic;

        public bool floatingpoint;

        public int argCount;
        public int arg1;
        public int arg2;
        public int arg3;

        public Func<Arg, Arg, Arg, List<byte>> assemble;

        public Func<Arg, Arg, Arg, int> numberOfBytes;
    }
}
