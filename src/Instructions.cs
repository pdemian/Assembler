using System;
using System.Collections.Generic;
using System.Linq;

namespace Assembler
{
    internal class x86InstructionSet
    {
        public static Instruction ADD = new Instruction
        {
            mnemonic = "add",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.IMMANY | InstructionArg.REGANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    return 2 + (a.type == InstructionArg.REG16 ? 1 : 0);
                }
                return 3 + (a.type == InstructionArg.REG16 ? 2 : a.type == InstructionArg.REG32 ? 3 : 0);
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                //deal with registers
                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    if (a.type == b.type)
                    {
                        if (a.type == InstructionArg.REG8)
                        {
                            x.Add(0x02);
                        }
                        else if (a.type == InstructionArg.REG16)
                        {
                            x.Add(0x66);
                            x.Add(0x03);
                        }
                        else
                        {
                            x.Add(0x03);
                        }

                        x.Add(ModRm(0xC0, a, b));
                    }
                    else
                    {
                        throw new Exception("Operand size conflict.");
                    }
                }
                //deal with registers/immediate
                else
                {
                    int size = 1;

                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x80);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            size = 2;
                            x.Add(0x66);
                        }
                        else
                        {
                            size = 4;
                        }

                        x.Add(0x81);
                    }

                    x.Add((byte) (0xC0 + (byte) a.value));

                    x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                }

                return x;
            }
        };

        public static Instruction AND = new Instruction
        {
            mnemonic = "and",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.REGANY | InstructionArg.IMMANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                return 2 +
                        ((b.type == InstructionArg.REG16)
                            ? 1
                            : ((b.type == InstructionArg.IMM8)
                                ? 1
                                : ((b.type == InstructionArg.IMM16)
                                    ? 3
                                    : ((b.type == InstructionArg.IMM32) ? 4 : 0))));
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    if (a.type != b.type)
                    {
                        throw new Exception("Size conflict.");
                    }

                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x22);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            x.Add(0x66);
                        }
                        x.Add(0x23);
                    }
                    x.Add(ModRm(0xC0, a, b));
                }
                else
                {
                    int size = 1;
                    //reg,imm
                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x80);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            size = 2;
                            x.Add(0x66);
                        }
                        else
                        {
                            size = 4;
                        }
                        x.Add(0x81);
                    }
                    x.Add((byte) (0xE0 + a.value));
                    x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                }

                return x;
            }
        };

        public static Instruction BSWAP = new Instruction
        {
            mnemonic = "cpuid",
            argCount = 0,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) => { return new List<byte> {0x0F, (byte) (0xC8 + a.value)}; }
        };

        public static Instruction CALL = new Instruction
        {
            mnemonic = "call",
            argCount = 1,
            arg1 = InstructionArg.REG32 | InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return a.type == InstructionArg.REG32 ? 2 : a.offsetValue > 0 ? 6 : 5; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (a.type == InstructionArg.REG32)
                {
                    x.Add(0xFF);
                    x.Add((byte) (0xD0 + a.value));
                }
                else
                {
                    //hack: deal with the fact that we don't have the address
                    //for a library function, a.
                    if (a.offsetValue > 0)
                    {
                        x.Add(0xFF);
                        x.Add(0x15);
                        x.Add(0x00);
                        x.Add(0x00);
                        x.Add(0x00);
                        x.Add(0x00);
                    }
                    else
                    {
                        x.Add(0xE8);
                        x.AddRange(BitConverter.GetBytes(a.value));
                    }
                }


                return x;
            }
        };

        public static Instruction CMP = new Instruction
        {
            mnemonic = "cmp",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.REG32 | InstructionArg.IMMANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return b.type == InstructionArg.REG32 ? 2 : 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (b.type != InstructionArg.REG32)
                {
                    x.Add(0x81);
                    x.Add((byte) (0xF8 + a.value));
                    x.AddRange(BitConverter.GetBytes(b.value));
                }
                else
                {
                    x.Add(0x3B);
                    x.Add(ModRm(0xC0, a, b));
                }


                return x;
            }
        };

        public static Instruction CPUID = new Instruction
        {
            mnemonic = "cpuid",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) => { return new List<byte> {0x0F, 0xA2}; }
        };

        public static Instruction CDQ = new Instruction
        {
            mnemonic = "cdq",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x099}; }
        };

        public static Instruction DEC = new Instruction
        {
            mnemonic = "dec",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (a.type == InstructionArg.REG32)
                {
                    return 1;
                }
                return 2;
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();


                if (a.type == InstructionArg.REG32)
                {
                    x.Add((byte) (0x48 + (byte) a.value));
                }
                else if (a.type == InstructionArg.REG16)
                {
                    x.Add(0x66);
                    x.Add((byte) (0xC8 + (byte) a.value));
                }
                else
                {
                    x.Add(0xFE);
                    x.Add((byte) (0x48 + (byte) a.value));
                }


                return x;
            }
        };

        public static Instruction DIV = new Instruction
        {
            mnemonic = "div",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                if (a.type == InstructionArg.REG8)
                {
                    x.Add(0xF6);
                }
                else
                {
                    if (a.type == InstructionArg.REG16)
                    {
                        x.Add(0x66);
                    }
                    x.Add(0xF7);
                }
                x.Add((byte) (0xF0 + a.value));

                return x;
            }
        };

        public static Instruction IDIV = new Instruction
        {
            mnemonic = "idiv",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                if (a.type == InstructionArg.REG8)
                {
                    x.Add(0xF6);
                }
                else
                {
                    if (a.type == InstructionArg.REG16)
                    {
                        x.Add(0x66);
                    }
                    x.Add(0xF7);
                }
                x.Add((byte) (0xF8 + a.value));

                return x;
            }
        };

        public static Instruction IMUL = new Instruction
        {
            mnemonic = "imul",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.REGANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 3 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                if (a.type != InstructionArg.REG32)
                {
                    x.Add(0x66);
                }
                x.Add(0x0F);
                x.Add(0xAF);
                x.Add(ModRm(0xC0,a,b));

                return x;
            }
        };

        public static Instruction INC = new Instruction
        {
            mnemonic = "inc",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (a.type == InstructionArg.REG32)
                {
                    return 1;
                }
                return 2;
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();


                if (a.type == InstructionArg.REG32)
                {
                    x.Add((byte) (0x40 + (byte) a.value));
                }
                else if (a.type == InstructionArg.REG16)
                {
                    x.Add(0x66);
                    x.Add((byte) (0xC0 + (byte) a.value));
                }
                else
                {
                    x.Add(0xFE);
                    x.Add((byte) (0x40 + (byte) a.value));
                }


                return x;
            }
        };

        public static Instruction INT = new Instruction
        {
            mnemonic = "int",
            argCount = 1,
            arg1 = InstructionArg.IMM8,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (((byte) a.value) == 3)
                {
                    return 1;
                }
                return 2;
            },
            assemble = (a, b, c) =>
            {
                if (((byte) a.value) == 3)
                {
                    return new List<byte> {0xCC};
                }
                return new List<byte> {0xCD, (byte) a.value};
            }
        };

        public static Instruction JMP = new Instruction
        {
            mnemonic = "jmp",
            argCount = 1,
            arg1 = InstructionArg.LABEL | InstructionArg.REG32,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (a.type == InstructionArg.REG32)
                {
                    return 1;
                }
                return 5;
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (a.type == InstructionArg.REG32)
                {
                    x.Add(0xFF);
                    x.Add((byte) (0xE0 + a.value));
                }
                else
                {
                    x.Add(0xE9);
                    x.AddRange(BitConverter.GetBytes(a.value + 5));
                }

                return x;
            }
        };

        public static Instruction JE = new Instruction
        {
            mnemonic = "je",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x84);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction JG = new Instruction
        {
            mnemonic = "jg",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x8F);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction JGE = new Instruction
        {
            mnemonic = "jge",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x8D);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction JL = new Instruction
        {
            mnemonic = "jl",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x8C);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction JLE = new Instruction
        {
            mnemonic = "jle",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x8E);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction JNE = new Instruction
        {
            mnemonic = "jne",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                x.Add(0x0F);
                x.Add(0x85);
                x.AddRange(BitConverter.GetBytes(a.value + 6));

                return x;
            }
        };

        public static Instruction LEA = new Instruction
        {
            mnemonic = "lea",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.OFFSET,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                try
                {
                    //too complicated to count easily/don't care enough
                    return LEA.assemble(a, b, c).Count();
                }
                catch (Exception)
                {
                    //going to fail anyways, why bother putting a proper value
                    return 1;
                }
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                x.Add(0x8D);


                //everything that's not general is special.
                //why? beats me
                if ((b.value == Registers.ESP.regfield || b.value == Registers.ESI.regfield ||
                        b.value == Registers.EDI.regfield || b.value == Registers.EBP.regfield)
                    && b.offsetRegister < 0)
                {
                    x.Add(ModRm(0x80 + b.value, a.value));

                    //esp is super special
                    if (b.value == Registers.ESP.regfield)
                    {
                        x.Add(0x24);
                    }

                    x.AddRange(BitConverter.GetBytes(b.offsetValue));
                }
                else
                {
                    if (b.offsetValue > 0)
                    {
                        //Encode for 'lea v, [w + x*y + z]
                        x.Add(ModRm(0x84, a.value));
                        x.Add(ModRm(b));
                        x.AddRange(BitConverter.GetBytes(b.offsetValue));
                    }
                    else if (b.offsetRegister > -1)
                    {
                        //Encode for 'lea v, [w+x]'
                        x.Add(ModRm(0x04, a.value));
                        x.Add(ModRm(b));
                    }
                    else
                    {
                        //Encode for 'lea v, [w]'
                        x.Add(ModRm(0, a, b));
                    }
                }
                return x;
            }
        };

        public static Instruction LEAVE = new Instruction
        {
            mnemonic = "leave",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0xC9}; }
        };

        public static Instruction LOOP = new Instruction
        {
            mnemonic = "loop",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) =>
            {
                if (a.value >= 129 || a.value <= -130)
                {
                    throw new Exception("Loop argument out of range.");
                }
                return new List<byte> {0xE2, (byte) (a.value + 2)};
            }
        };

        public static Instruction LOOPE = new Instruction
        {
            mnemonic = "loope",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) =>
            {
                if (a.value >= 129 || a.value <= -130)
                {
                    throw new Exception("Loop argument out of range.");
                }
                return new List<byte> {0xE1, (byte) (a.value + 2)};
            }
        };

        public static Instruction LOOPNE = new Instruction
        {
            mnemonic = "loopne",
            argCount = 1,
            arg1 = InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) =>
            {
                if (a.value >= 129 || a.value <= -130)
                {
                    throw new Exception("Loop argument out of range.");
                }
                return new List<byte> {0xE0, (byte) (a.value + 2)};
            }
        };

        public static Instruction MOV = new Instruction
        {
            mnemonic = "mov",
            argCount = 2,
            arg1 = InstructionArg.REGANY | InstructionArg.OFFSET,
            arg2 = InstructionArg.REGANY | InstructionArg.OFFSET | InstructionArg.IMMANY | InstructionArg.LABEL,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                //The code to properly figure this out would be too complex and I'm lazy
                try
                {
                    return MOV.assemble(a, b, c).Count;
                }
                catch (Exception)
                {
                    //it's going to fail when we build anyways, so there is really no need to return a proper value
                    return 1;
                }
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                //register, imm/register/offset
                if ((a.type & InstructionArg.REGANY) != 0)
                {
                    //register, label
                    if (b.type == InstructionArg.LABEL)
                    {
                        x.Add(0x8B);
                        x.Add(ModRm(0x05, a.value));
                        x.Add(0x00);
                        x.Add(0x00);
                        x.Add(0x00);
                        x.Add(0x00);
                    }
                    //register, imm
                    else if ((b.type & InstructionArg.IMMANY) != 0)
                    {
                        int size = 1;
                        if (a.type == InstructionArg.REG8)
                        {
                            x.Add((byte) (0xB0 + a.value));
                        }
                        else
                        {
                            if (a.type == InstructionArg.REG16)
                            {
                                size = 2;
                                x.Add(0x66);
                            }
                            else
                            {
                                size = 4;
                            }
                            x.Add((byte) (0xB8 + a.value));
                        }
                        x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                    }
                    //register, register
                    else if ((b.type & InstructionArg.REGANY) != 0)
                    {
                        if (b.type != a.type)
                        {
                            throw new Exception("Operand size conflict.");
                        }

                        if (a.type == InstructionArg.REG8)
                        {
                            x.Add(0x8A);
                        }
                        else
                        {
                            if (a.type == InstructionArg.REG16)
                            {
                                x.Add(0x66);
                            }
                            x.Add(0x8B);
                        }

                        x.Add(ModRm(0xC0, a, b));
                    }
                    //register,offset
                    else
                    {
                        if (a.type != InstructionArg.REG32)
                        {
                            throw new Exception("Invalid operand size.");
                        }

                        x.Add(0x8B);

                        //mov v, [w+x*imm+imm] or mov v, [w+x+imm]
                        if (b.offsetRegister > -1 && b.offsetValue > 0)
                        {
                            x.Add(ModRm(0x84, a.value));
                            x.Add(ModRm(b));
                            //0x00 + offset, value
                            x.Add(ModRm(0xC0, b.offsetRegister, b.value));
                            x.AddRange(BitConverter.GetBytes(b.offsetValue));
                        }
                        //mov v, [w+x]
                        else if (b.offsetRegister > -1)
                        {
                            x.Add(ModRm(0x04, a.value));

                            if (b.value == Registers.ESP.regfield &&
                                b.offsetRegister == Registers.ESP.regfield)
                            {
                                throw new Exception("Invalid operands.");
                            }

                            x.Add(ModRm(b));
                        }
                        //mov v, [w+imm] or mov v, [w]
                        else
                        {
                            x.Add(ModRm(0x80, a, b));
                            if (b.value == Registers.ESP.regfield)
                            {
                                x.Add(0x24);
                            }
                            x.AddRange(BitConverter.GetBytes(b.offsetValue));
                        }
                    }
                }
                //offset, imm/register/offset
                else if (a.type == InstructionArg.OFFSET)
                {
                    //offset, register
                    if ((b.type & InstructionArg.REGANY) != 0)
                    {
                        if (b.type != InstructionArg.REG32)
                        {
                            throw new Exception("Operand size conflict.");
                        }

                        x.Add(0x89);

                        if (a.value == Registers.ESP.regfield &&
                            a.offsetRegister == Registers.ESP.regfield)
                        {
                            throw new Exception("Invalid operands.");
                        }

                        //mov [w+x*imm+imm], z or mov [w+x+imm], z
                        if (a.offsetRegister > -1 && a.offsetValue > 0)
                        {
                            x.Add(ModRm(0x84, b.value));
                            x.Add(ModRm(a));
                            x.AddRange(BitConverter.GetBytes(a.offsetValue));
                        }
                        //mov [w+x], z
                        else if (a.offsetRegister > -1)
                        {
                            x.Add(ModRm(0x04, b.value));
                            x.Add(ModRm(a));
                        }
                        //mov [w+imm], z or mov [w],z
                        else
                        {
                            x.Add(ModRm(0x80, b, a));
                            if (a.value == Registers.ESP.regfield)
                            {
                                x.Add(0x24);
                            }
                            x.AddRange(BitConverter.GetBytes(a.offsetValue));
                        }
                    }
                    //offset, imm
                    else if ((b.type & InstructionArg.IMMANY) != 0)
                    {
                        x.Add(0xC6);
                        //mov [w+x*imm+imm], imm or mov [w+x+imm], imm
                        if (a.offsetRegister > -1 && a.offsetValue > 0)
                        {
                            x.Add(0x44);
                            x.Add(ModRm(a));
                            x.Add((byte) (a.offsetValue));
                            x.Add((byte) b.value);
                        }
                        //mov [w+x], imm
                        else if (a.offsetRegister > -1)
                        {
                            x.Add(0x04);
                            x.Add(ModRm(a));
                            x.Add((byte) b.value);
                        }
                        //mov [w+imm], imm or mov [w],imm
                        else
                        {
                            x.Add((byte) (0x80 + a.value));
                            if (a.value == Registers.ESP.regfield)
                            {
                                x.Add(0x24);
                            }
                            x.AddRange(BitConverter.GetBytes(a.offsetValue));
                            x.Add((byte) b.value);
                        }
                    }
                    //offset, offset or offset,label
                    else
                    {
                        throw new Exception("Improper operand types.");
                    }
                }

                return x;
            }
        };

        public static Instruction MUL = new Instruction
        {
            mnemonic = "mul",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();
                if (a.type == InstructionArg.REG8)
                {
                    x.Add(0xF6);
                }
                else
                {
                    if (a.type == InstructionArg.REG16)
                    {
                        x.Add(0x66);
                    }
                    x.Add(0xF7);
                }
                x.Add((byte) (0xE0 + a.value));

                return x;
            }
        };

        public static Instruction NEG = new Instruction
        {
            mnemonic = "neg",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (a.type == InstructionArg.REG8)
                {
                    x.Add(0xF6);
                }
                else if (a.type == InstructionArg.REG16)
                {
                    x.Add(0x66);
                    x.Add(0xF7);
                }
                else
                {
                    x.Add(0xF7);
                }

                x.Add((byte) (0xD0 + a.value));

                return x;
            }
        };

        public static Instruction NOP = new Instruction
        {
            mnemonic = "nop",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x90}; }
        };

        public static Instruction NOT = new Instruction
        {
            mnemonic = "not",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2 + (a.type == InstructionArg.REG16 ? 1 : 0); },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (a.type == InstructionArg.REG8)
                {
                    x.Add(0xF6);
                }
                else
                {
                    if (a.type == InstructionArg.REG16)
                    {
                        x.Add(0x66);
                    }

                    x.Add(0xF7);
                }
                x.Add((byte) (0xD0 + a.value));

                return x;
            }
        };

        public static Instruction OR = new Instruction
        {
            mnemonic = "or",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.REGANY | InstructionArg.IMMANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                return 2 +
                        ((b.type == InstructionArg.REG16)
                            ? 1
                            : ((b.type == InstructionArg.IMM8)
                                ? 1
                                : ((b.type == InstructionArg.IMM16)
                                    ? 3
                                    : ((b.type == InstructionArg.IMM32) ? 4 : 0))));
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    if (a.type != b.type)
                    {
                        throw new Exception("Size conflict.");
                    }

                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x0A);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            x.Add(0x66);
                        }
                        x.Add(0x0B);
                    }
                    x.Add(ModRm(0xC0, a, b));
                }
                else
                {
                    int size = 1;
                    //reg,imm
                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x80);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            size = 2;
                            x.Add(0x66);
                        }
                        else
                        {
                            size = 4;
                        }
                        x.Add(0x81);
                    }
                    x.Add((byte) (0xC8 + a.value));
                    x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                }

                return x;
            }
        };

        public static Instruction POP = new Instruction
        {
            mnemonic = "pop",
            argCount = 1,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (a.type == InstructionArg.REG32)
                {
                    return 1;
                }
                return 2;
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                //because there is no 8bit instruction, ah == ax, ch == cx
                if (a.type == InstructionArg.REG8 && a.value >= 4)
                {
                    a.value -= 4;
                }

                if (a.type == InstructionArg.REG32)
                {
                    x.Add((byte) (0x58 + (byte) a.value));
                }
                else
                {
                    x.Add(0x66);
                    x.Add((byte) (0x58 + (byte) a.value));
                }

                return x;
            }
        };

        public static Instruction POPAD = new Instruction
        {
            mnemonic = "popad",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x61}; }
        };

        public static Instruction POPFD = new Instruction
        {
            mnemonic = "popfd",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x9D}; }
        };

        public static Instruction PUSH = new Instruction
        {
            mnemonic = "push",
            argCount = 1,
            arg1 = InstructionArg.REGANY | InstructionArg.IMMANY | InstructionArg.LABEL,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if (a.type == InstructionArg.REG32)
                {
                    return 1;
                }
                if (a.type == InstructionArg.REG16 || a.type == InstructionArg.REG8)
                {
                    return 2;
                }
                if (a.type == InstructionArg.LABEL)
                {
                    return 5;
                }
                return 1 + (a.type == InstructionArg.IMM8 ? 1 : 4);
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                //ah == al, ch == cl
                if (a.type == InstructionArg.REG8 && a.value >= 4)
                {
                    a.value -= 4;
                }


                if (a.type == InstructionArg.REG32)
                {
                    x.Add((byte) (0x50 + (byte) a.value));
                }
                else if (a.type == InstructionArg.REG16 || a.type == InstructionArg.REG8)
                {
                    x.Add(0x66);
                    x.Add((byte) (0x50 + (byte) a.value));
                }
                else if (a.type == InstructionArg.LABEL)
                {
                    x.Add(0x68);
                    x.Add(0x00);
                    x.Add(0x00);
                    x.Add(0x00);
                    x.Add(0x00);
                }
                else
                {
                    int size = 1;
                    if (a.type == InstructionArg.IMM8)
                    {
                        x.Add(0x6A);
                    }
                    else
                    {
                        size = 4;
                        x.Add(0x68);
                    }

                    x.AddRange(BitConverter.GetBytes(a.value).Take(size));
                }


                return x;
            }
        };

        public static Instruction PUSHAD = new Instruction
        {
            mnemonic = "pushad",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x60}; }
        };

        public static Instruction PUSHFD = new Instruction
        {
            mnemonic = "pushfd",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0x9C}; }
        };

        public static Instruction RET = new Instruction
        {
            mnemonic = "ret",
            argCount = 0,
            arg1 = InstructionArg.NONE,
            arg2 = InstructionArg.NONE,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 1; },
            assemble = (a, b, c) => { return new List<byte> {0xC3}; }
        };

        public static Instruction SAL = new Instruction
        {
            mnemonic = "sal",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.IMM8,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 3; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                x.Add(0xC1);
                x.Add((byte) (0xE0 + a.value));
                x.Add((byte) b.value);

                return x;
            }
        };

        public static Instruction SAR = new Instruction
        {
            mnemonic = "sar",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.IMM8,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 3; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                x.Add(0xC1);
                x.Add((byte) (0xF8 + a.value));
                x.Add((byte) b.value);

                return x;
            }
        };

        public static Instruction SUB = new Instruction
        {
            mnemonic = "sub",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.IMMANY | InstructionArg.REGANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    return 2 + (a.type == InstructionArg.REG16 ? 1 : 0);
                }
                return 3 + (a.type == InstructionArg.REG16 ? 2 : a.type == InstructionArg.REG32 ? 3 : 0);
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                //deal with registers
                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    if (a.type == b.type)
                    {
                        if (a.type == InstructionArg.REG8)
                        {
                            x.Add(0x2A);
                        }
                        else if (a.type == InstructionArg.REG16)
                        {
                            x.Add(0x66);
                            x.Add(0x2B);
                        }
                        else
                        {
                            x.Add(0x2B);
                        }
                        x.Add(ModRm(0xC0, a, b));
                    }
                    else
                    {
                        throw new Exception("Operand size conflict.");
                    }
                }
                //deal with registers/immediate
                else
                {
                    int size = 1;

                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x80);
                    }
                    else
                    {
                        if (a.type == InstructionArg.REG16)
                        {
                            size = 2;
                            x.Add(0x66);
                        }
                        else
                        {
                            size = 4;
                        }

                        x.Add(0x81);
                    }

                    x.Add((byte) (0xE8 + (byte) a.value));

                    x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                }

                return x;
            }
        };

        public static Instruction TEST = new Instruction
        {
            mnemonic = "test",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.REG32 | InstructionArg.IMMANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return b.type == InstructionArg.REG32 ? 2 : 6; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if (b.type != InstructionArg.REG32)
                {
                    x.Add(0xF7);
                    x.Add((byte) (0xC0 + a.value));
                    x.AddRange(BitConverter.GetBytes(b.value));
                }
                else
                {
                    x.Add(0x85);
                    x.Add(ModRm(0xC0, a, b));
                }


                return x;
            }
        };

        public static Instruction XCHG = new Instruction
        {
            mnemonic = "xchg",
            argCount = 2,
            arg1 = InstructionArg.REG32,
            arg2 = InstructionArg.REG32,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) => { return 2; },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                x.Add(0x87);
                x.Add(ModRm(0xC0, a, b));

                return x;
            }
        };

        public static Instruction XOR = new Instruction
        {
            mnemonic = "xor",
            argCount = 2,
            arg1 = InstructionArg.REGANY,
            arg2 = InstructionArg.REGANY | InstructionArg.IMMANY,
            arg3 = InstructionArg.NONE,
            floatingpoint = false,
            numberOfBytes = (a, b, c) =>
            {
                return ((b.type & InstructionArg.REGANY) != 0
                    ? 2 + (a.type == InstructionArg.REG16 || b.type == InstructionArg.REG16 ? 1 : 0)
                    : 3 + ((a.type & InstructionArg.REG16) != 0 ? 2 : ((a.type & InstructionArg.REG32) != 0) ? 3 : 0));
            },
            assemble = (a, b, c) =>
            {
                List<byte> x = new List<byte>();

                if ((b.type & InstructionArg.REGANY) != 0)
                {
                    if (a.type != b.type)
                    {
                        throw new Exception("Incompatible operand sizes.");
                    }

                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x32);
                    }
                    else if (a.type == InstructionArg.REG16)
                    {
                        x.Add(0x66);
                        x.Add(0x33);
                    }
                    else
                    {
                        x.Add(0x33);
                    }
                    x.Add(ModRm(0xC0, a, b));
                }
                else
                {
                    int size = 1;
                    if (a.type == InstructionArg.REG8)
                    {
                        x.Add(0x80);
                    }
                    else if (a.type == InstructionArg.REG16)
                    {
                        size = 2;
                        x.Add(0x66);
                        x.Add(0x81);
                    }
                    else
                    {
                        size = 4;
                        x.Add(0x81);
                    }

                    x.Add((byte) (0xF0 + a.value));
                    x.AddRange(BitConverter.GetBytes(b.value).Take(size));
                }

                return x;
            }
        };

        #region x87

        //not yet implemented.
        public static Instruction FABS = new Instruction();
        public static Instruction FADD = new Instruction();
        public static Instruction FCHS = new Instruction();
        public static Instruction FCOMP = new Instruction();
        public static Instruction FCOS = new Instruction();
        public static Instruction FDECSTP = new Instruction();
        public static Instruction FDIV = new Instruction();
        public static Instruction FICOMP = new Instruction();
        public static Instruction FILD = new Instruction();
        public static Instruction FINCSTP = new Instruction();
        public static Instruction FIST = new Instruction();
        public static Instruction FLD = new Instruction();
        public static Instruction FLD1 = new Instruction();
        public static Instruction FLDZ = new Instruction();
        public static Instruction FMUL = new Instruction();
        public static Instruction FNOP = new Instruction();
        public static Instruction FRNDINT = new Instruction();
        public static Instruction FRSTOR = new Instruction();
        public static Instruction FSAVE = new Instruction();
        public static Instruction FSCALE = new Instruction();
        public static Instruction FSIN = new Instruction();
        public static Instruction FSINCOS = new Instruction();
        public static Instruction FSQRT = new Instruction();
        public static Instruction FST = new Instruction();
        public static Instruction FSUB = new Instruction();
        public static Instruction FTST = new Instruction();
        public static Instruction FXCH = new Instruction();
        public static Instruction FXRSTOR = new Instruction();
        public static Instruction FXSAVE = new Instruction();

        #endregion

        //Why is ModRm so complicated you might ask?
        //Clearly that's because 
        private static byte ModRm(Arg argument)
        {
            //The magic values have a meaning
            //I've yet to figure out what, but they do
            return
                (byte)
                    ((argument.shiftValue << 6) +
                        (argument.offsetRegister > -1 ? (argument.offsetRegister << 3) : 0) + argument.value);
        }

        private static byte ModRm(int mod, int arg1, int arg2)
        {
            return (byte) (mod + (arg1 << 3) + arg2);
        }

        private static byte ModRm(int mod, Arg argument1, Arg argument2)
        {
            return (byte) (mod + (argument1.value << 3) + argument2.value);
        }

        private static byte ModRm(int mod, int value)
        {
            return (byte) (mod + value << 3);
        }


        public static Instruction[] x86Instructions =
        {
            ADD, AND, BSWAP, CALL, CMP, CPUID, DEC, DIV,
            /*FABS,FADD,FCHS,FCOMP,FCOS,FDECSTP,FDIV,FICOMP,
            FILD,FINCSTP,FIST,FLD,FLD1,FLDZ,FMUL,FNOP,FRNDINT,
            FRSTOR,FSAVE,FSCALE,FSIN,FSINCOS,FSQRT,FST,FSUB,
            FTST,FXCH,FXRSTOR,FXSAVE,
            */
            IDIV, IMUL, INC, INT, JMP, CDQ,
            JE, JG, JGE, JL, JLE, JNE, LEA, LEAVE, LOOP, LOOPE,
            LOOPNE, MOV, MUL, NEG, NOP, NOT, OR, POP, POPAD, POPFD, PUSH,
            PUSHAD, PUSHFD, RET, SAL, SAR, SUB, TEST, XCHG, XOR
        };
    }
}