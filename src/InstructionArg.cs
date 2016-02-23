namespace Assembler
{
    public class InstructionArg
    {
        //Take 0 arguments
        public static int NONE = 0x0000;
        //Take an immediate (constant) 8 bit value
        public static int IMM8 = 0x1000;
        //Take an immediate (constant) 16 bit value
        public static int IMM16 = 0x2000;
        //Take an immediate (constant) 32 bit value
        public static int IMM32 = 0x4000;
        //Take an 8 bit register
        public static int REG8 = 0x10000;
        //Take a 16 bit register
        public static int REG16 = 0x20000;
        //Take a 32 bit register
        public static int REG32 = 0x40000;

        //Take any immediate (8 bit,16 bit, or 32 bit)
        public static int IMMANY = IMM8 | IMM16 | IMM32;
        //Take any register (8 bit, 16 bit, or 32 bit)
        public static int REGANY = REG8 | REG16 | REG32;

        //Take a label (for gotos/jumps)
        public static int LABEL = 0x80000;
        //Take a combination of values (see Arg class)
        public static int OFFSET = 0x100000;
    }
}
