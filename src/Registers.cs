namespace Assembler
{
    internal class Registers
    {
        //1 byte registers
        public static Register AL = new Register { size = InstructionArg.REG8, regfield = 0 };
        public static Register CL = new Register { size = InstructionArg.REG8, regfield = 1 };
        public static Register DL = new Register { size = InstructionArg.REG8, regfield = 2 };
        public static Register BL = new Register { size = InstructionArg.REG8, regfield = 3 };
        public static Register AH = new Register { size = InstructionArg.REG8, regfield = 4 };
        public static Register CH = new Register { size = InstructionArg.REG8, regfield = 5 };
        public static Register DH = new Register { size = InstructionArg.REG8, regfield = 6 };
        public static Register BH = new Register { size = InstructionArg.REG8, regfield = 7 };

        //2 byte registers
        public static Register AX = new Register { size = InstructionArg.REG16, regfield = 0 };
        public static Register CX = new Register { size = InstructionArg.REG16, regfield = 1 };
        public static Register DX = new Register { size = InstructionArg.REG16, regfield = 2 };
        public static Register BX = new Register { size = InstructionArg.REG16, regfield = 3 };

        //4 byte registers
        public static Register EAX = new Register { size = InstructionArg.REG32, regfield = 0 };
        public static Register ECX = new Register { size = InstructionArg.REG32, regfield = 1 };
        public static Register EDX = new Register { size = InstructionArg.REG32, regfield = 2 };
        public static Register EBX = new Register { size = InstructionArg.REG32, regfield = 3 };
        public static Register ESP = new Register { size = InstructionArg.REG32, regfield = 4 };
        public static Register EBP = new Register { size = InstructionArg.REG32, regfield = 5 };
        public static Register ESI = new Register { size = InstructionArg.REG32, regfield = 6 };
        public static Register EDI = new Register { size = InstructionArg.REG32, regfield = 7 };

        public static Register ReverseLookup(string name)
        {
            switch (name)
            {
                case "al":
                    return AL;
                case "cl":
                    return CL;
                case "dl":
                    return DL;
                case "bl":
                    return BL;
                case "ah":
                    return AH;
                case "ch":
                    return CH;
                case "dh":
                    return DH;
                case "bh":
                    return BH;
                case "ax":
                    return AX;
                case "cx":
                    return CX;
                case "dx":
                    return DX;
                case "bx":
                    return BX;
                case "eax":
                    return EAX;
                case "ecx":
                    return ECX;
                case "edx":
                    return EDX;
                case "ebx":
                    return EBX;
                case "esp":
                    return ESP;
                case "ebp":
                    return EBP;
                case "esi":
                    return ESI;
                case "edi":
                    return EDI;
                default:
                    return null;
            }
        }
    }
}
