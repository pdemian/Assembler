namespace Assembler
{
    internal class Arg
    {
        public int type;
        public int value;
        public int offsetRegister;
        public int offsetValue;
        public int shiftValue;

        //When seeing this: '[eax + ecx*4 + 8]'
        //value = eax
        //offsetRegister = ecx
        //offsetValue = 8
        //shiftValue = 4

        //When seeing this: '[eax]' or 'eax'
        //value = eax

        public string ident;
    }
}
