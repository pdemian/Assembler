namespace Assembler
{
    internal class UnprocessedInstruction
    {
        public bool isLabel;
        public string ident;
        public Instruction instruction;
        public Arg arg0;
        public Arg arg1;
        public Arg arg2;
        public int offset;

        public Token main_token;
    }
}
