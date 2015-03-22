using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Assembler
{
    class Assembler
    {
        public List<Token> tokens;
        public List<string> warnings;

        private string filename;
        private string shortname;

        private List<byte> code;
        private List<UnprocessedInstruction> unprocessed_code;
        private Dictionary<string, int> labels;
        private Dictionary<string, List<int>> stringTable;
        private List<string> imports;

        private Dictionary<string, CodeInformation.FunctionInformation> unresolvedReferences;

        private int index;
        private int offset;

        public CodeInformation ci;

        internal class UnprocessedInstruction
        {
            public bool isLabel;
            public string ident;
            public x86InstructionSet.Instruction instruction;
            public x86InstructionSet.Arg arg0;
            public x86InstructionSet.Arg arg1;
            public x86InstructionSet.Arg arg2;
            public int offset;

            public Token main_token;
        }


        public Assembler(string filename)
        {
            warnings = new List<string>();
            this.filename = filename;
            code = new List<byte>();
            labels = new Dictionary<string, int>();
            stringTable = new Dictionary<string, List<int>>();
            shortname = Path.GetFileName(filename);
            imports = new List<string>();
            index = 0;
            offset = 0;
            unresolvedReferences = new Dictionary<string, CodeInformation.FunctionInformation>();
            ci = new CodeInformation();
            unprocessed_code = new List<UnprocessedInstruction>();
        }


        public void Assemble()
        {
            Scanner sc = null;
            try
            {
                sc = new Scanner(filename);
                sc.Scan();
                tokens = sc.tokens;
                warnings.AddRange(sc.warnings);
            }
            catch(Exception)
            {
                if (sc != null)
                    warnings.AddRange(sc.warnings);
                throw;
            }

            Parse();

            AssembleCode();

            //set ci accordingly
            ci.Code = code.ToArray();
            ci.FileAlignment = 0;
            ci.SectionAlignment = 0;
            ci.StringTable = new List<CodeInformation.StringTableInformation>();
            foreach(var str in stringTable)
            {
                ci.StringTable.Add(new CodeInformation.StringTableInformation { Text = str.Key, Replacements = str.Value });
            }
        }

        private void Parse()
        {
            string lang;
            string filen;
            bool parsedText = false;
            bool parsedData = false;
            Token textStartPosition = null;
            Token dataStartPosition = null;


            try
            {
                for (; index < tokens.Count; index++)
                {
                    //expect a directive marker '.'
                    if (!tokens[index].Equals("."))
                    {
                        warnings.Add(FormatWarning("Expected a directive. Ignoring line.", tokens[index].lineNum, tokens[index].linePos));
                        int this_line = tokens[index].lineNum;
                        while (tokens.Count + 1 > index && tokens[index + 1].lineNum <= this_line)
                            index++;
                        continue;
                    }
                    index++;



                    switch (tokens[index].str_value)
                    {
                        case "lang":
                            //At the moment only x86 is available
                            if (tokens.Count + 1 <= index)
                                throw new Exception(FormatError("Unexpected EOF.", tokens[index].lineNum, tokens[index].linePos));
                            if (!tokens[index + 1].Equals("x86"))
                                throw new Exception(String.Format("{0}: Fatal Error: Language \"{1}\" is unsupported. Currently only \"x86\" is supported.", shortname, tokens[index + 1]));

                            lang = tokens[++index];
                            break;
                        case "subsystem":
                            Expect(typeof(StringBuilder), "Expected a valid subsystem");
                            if (!tokens[index].Equals("GUI") && !tokens[index].Equals("CUI"))
                                warnings.Add("Invalid subsystem. Assuming CUI.");
                            ci.Subsystem = (ushort)(tokens[index].Equals("GUI") ? 2 : 3);
                            break;
                        case "file":
                            Expect(typeof(StringBuilder), "Expected a valid filename", fatal: false);
                            filen = tokens[index];
                            break;
                        case "include":
                            Expect(typeof(StringBuilder), "Expected a valid library.");
                            imports.Add(tokens[index]);
                            break;
                        case "text":
                            if (parsedText)
                                throw new Exception(FormatError(String.Format("Expected only a single text section. Previous text section started at: {0}:{1}", textStartPosition.lineNum, textStartPosition.linePos), tokens[index - 1].lineNum, tokens[index - 1].linePos));
                            parsedText = true;
                            textStartPosition = tokens[index - 1];

                            ParseCode();

                            break;
                        case "data":
                            if (parsedData)
                                throw new Exception(FormatError(String.Format("Expected only a single data section. Previous data section started at: {0}:{1}", dataStartPosition.lineNum, dataStartPosition.linePos), tokens[index - 1].lineNum, tokens[index - 1].linePos));
                            parsedData = true;
                            dataStartPosition = tokens[index - 1];

                            ParseData();

                            break;
                        default:
                            warnings.Add(FormatWarning("Expected a valid directive. Ignoring line.", tokens[index - 1].lineNum, tokens[index - 1].linePos));
                            int this_line = tokens[index].lineNum;
                            while (tokens.Count + 1 > index && tokens[index + 1].lineNum <= this_line)
                                index++;
                            break;
                    }
                }
            }
            catch(Exception ex)
            {
                throw;
            }

        }

        private void ParseCode()
        {
            //Expect { instruction } [string] (arg1 (, arg2 (, arg3 )))
            //Expect { label }       [string]:

            for (; index + 1 < tokens.Count && !tokens[index + 1].Equals("."); )
            {
                Expect(typeof(string), "Expected a label or instruction");
                if (index + 1 < tokens.Count && tokens[index + 1].Equals(":"))
                {
                    unprocessed_code.Add(new UnprocessedInstruction { ident = tokens[index], isLabel = true, main_token = tokens[index]});

                    index++;
                    //a label does not need to be on its own line
                    continue;
                }
                else
                {
                    UnprocessedInstruction inst = new UnprocessedInstruction { ident = tokens[index], isLabel = false, main_token = tokens[index]};


                    if(index < tokens.Count && !nextTokenIsNextLine())
                    {
                        index++;
                        inst.arg0 = ParseArg();
                        
                        if (index < tokens.Count && !nextTokenIsNextLine())
                        {
                            index++;
                            if(!tokens[index].Equals(","))
                                throw new Exception(FormatError("Expected a delimiter between arguments.", tokens[index].lineNum, tokens[index].linePos));

                            index++;
                            inst.arg1 = ParseArg();
                            if (index < tokens.Count && !nextTokenIsNextLine())
                            {
                                index++;
                                if (!tokens[index].Equals(","))
                                    throw new Exception(FormatError("Expected a delimiter between arguments.", tokens[index].lineNum, tokens[index].linePos));

                                index++;
                                inst.arg2 = ParseArg();
                                if (!nextTokenIsNextLine())
                                {
                                    throw new Exception(FormatError("Unexpected arg count. No instruction has more than 3 arguments.", tokens[index].lineNum, tokens[index].linePos));
                                }
                            }
                        }
                    }

                    unprocessed_code.Add(inst);

                }
            }
        }
        private x86InstructionSet.Arg ParseArg()
        {
            var arg = new x86InstructionSet.Arg();

            if (tokens[index].Equals("["))
            {
                arg.offsetRegister = -1;
                arg.offsetValue = -1;
                arg.shiftValue = 0;

                //Expect [x] or [x+imm] or [x+y] or [x+y+imm] or [x+y*z+imm]
                Expect(typeof(string), "Expected a valid identifier.");
                string firstValue = tokens[index];
                string secondValue = null;
                int imm = 0;
                int shift = 0;

                index++;
                if (tokens[index].Equals("+") || tokens[index].Equals("-"))
                {
                    bool inverse = tokens[index].Equals("-");

                    index++;
                    //[x+y(...)]
                    if(tokens[index].type == typeof(string))
                    {
                        if (inverse)
                            throw new Exception(FormatError("Operation not supported.", tokens[index].lineNum, tokens[index].linePos));

                        secondValue = tokens[index];
                        index++;

                        if(tokens[index].Equals("*"))
                        {
                            Expect(typeof(uint), "Syntax error");
                            shift = (int)(tokens[index].obj as uint?);
                            if (shift != 1 && shift != 2 && shift != 4 && shift != 8)
                                throw new Exception(FormatError("Invalid shift. Expected 1,2,4,8", tokens[index].lineNum, tokens[index].linePos));

                            shift = shift == 2 ? 1 : shift == 4 ? 2 : shift == 8 ? 3 : 0;

                            index++;
                        }

                        if(tokens[index].Equals("+"))
                        {
                            Expect(typeof(uint), "Syntax error");
                            imm = (int)(tokens[index].obj as uint?);
                            index++;
                        }

                        Expect("]", "Syntax error", false);

                    }
                    else if(tokens[index].type == typeof(uint))
                    {
                        //immediate
                        imm = (int)(tokens[index].obj as uint?);
                        imm *= (inverse ? -1 : 1);

                        Expect("]", "Syntax error");
                    }
                    else
                    {
                        throw new Exception(FormatError("Unexpected long,float,or character in expression.", tokens[index].lineNum, tokens[index].linePos));
                    }
                }
                else if (!tokens[index].Equals("]"))
                    throw new Exception(FormatError("Syntax error.", tokens[index].lineNum, tokens[index].linePos));

                if (shift != 0 && secondValue == null)
                    throw new Exception(FormatError("Unsupported offset", tokens[index].lineNum, tokens[index].linePos));

                if (secondValue != null && secondValue.Equals("esp"))
                    throw new Exception(FormatError("Invalid operand", tokens[index].lineNum, tokens[index].linePos));

                arg.type = x86InstructionSet.InstructionArg.OFFSET;
                var regist = x86InstructionSet.Registers.ReverseLookup(firstValue);
                if (regist == null)
                    throw new Exception(FormatError("Expected valid register", tokens[index].lineNum, tokens[index].linePos));
                if (regist.size != x86InstructionSet.InstructionArg.REG32)
                    throw new Exception(FormatError("Expected a 32 bit register.", tokens[index].lineNum, tokens[index].linePos));

                arg.value = regist.regfield;
                if(secondValue != null)
                {
                    regist = x86InstructionSet.Registers.ReverseLookup(secondValue);
                    if (regist == null)
                        throw new Exception(FormatError("Expected valid register", tokens[index].lineNum, tokens[index].linePos));
                    if (regist.size != x86InstructionSet.InstructionArg.REG32)
                        throw new Exception(FormatError("Expected a 32 bit register.", tokens[index].lineNum, tokens[index].linePos));
                    arg.offsetRegister = regist.regfield;
                }

                arg.offsetValue = imm;
                arg.shiftValue = shift;
                
            }
            else if(tokens[index].type == typeof(uint) || tokens[index].type == typeof(ulong))
            {
                if (tokens[index].type == typeof(ulong))
                    throw new Exception(FormatError("Unexpected 64 bit integer", tokens[index].lineNum, tokens[index].linePos));

                arg.value = (int)(tokens[index].obj as uint?);

                if (arg.value <= 255)
                    arg.type = x86InstructionSet.InstructionArg.IMM8;
                else
                    arg.type = x86InstructionSet.InstructionArg.IMM32;
            }
            else if(tokens[index].type == typeof(int) || tokens[index].type == typeof(long))
            {
                if (tokens[index].type == typeof(long))
                    throw new Exception(FormatError("Unexpected 64 bit integer", tokens[index].lineNum, tokens[index].linePos));

                arg.value = (int)(tokens[index].obj as int?);

                if (arg.value <= 255)
                    arg.type = x86InstructionSet.InstructionArg.IMM8;
                else
                    arg.type = x86InstructionSet.InstructionArg.IMM32;
            }
            else if(tokens[index].type == typeof(float) || tokens[index].type == typeof(double))
            {
                arg.type = x86InstructionSet.InstructionArg.IMM32;
                arg.value = BitConverter.ToInt32(BitConverter.GetBytes((float)(tokens[index].obj as float?)), 0);
            }
            else
            {
                //Expect either a register or label
                string ident = tokens[index];
                var reg = x86InstructionSet.Registers.ReverseLookup(ident);
                if(reg == null)
                {
                    //either a label, string, function
                    //really hacky at the moment
                    if(unresolvedReferences.ContainsKey(ident))
                    {
                        arg.type = x86InstructionSet.InstructionArg.LABEL;
                        arg.ident = ident;
                    }
                    else
                    {
                        arg.type = x86InstructionSet.InstructionArg.LABEL;
                        arg.ident = ident;
                        unresolvedReferences.Add(ident, null);
                    }
                }
                else
                {
                    arg.type = reg.size;
                    arg.value = reg.regfield;
                }
            }
            return arg;
        }

        private void ParseData()
        {
            //Expect [string] '=' [StringBuilder] NEWLINE
            //eg, HelloWorld = "Hello World!\n" 

            for (; index + 1 < tokens.Count && !tokens[index + 1].Equals("."); )
            {
                Expect(typeof(string), "Expected an identifier");
                string ident = tokens[index];
                Expect("=", "Expected '= [value]'");
                Expect(typeof(StringBuilder), "Expected '= [value]'");
                if (!nextTokenIsNextLine())
                    throw new Exception(FormatError("Unexpected characters on line after declaration.", tokens[index].lineNum, tokens[index].linePos));

                if (this.stringTable.ContainsKey(ident))
                {
                    warnings.Add("Redefinition of string");
                    stringTable[ident] = new List<int>();
                }
                else
                {
                    stringTable.Add(ident, new List<int>());
                }
            }
        }
        
        private void AssembleCode()
        {
            //resolve external dependencies
            try
            {
                foreach(string s in imports)
                {
                    CodeInformation.SymbolInformation si = new CodeInformation.SymbolInformation { LibraryName = s + ".dll", Functions = new List<CodeInformation.FunctionInformation>() };

                    //import values
                    string[] lines = null;


                    string temp = "libraries/" + s + ".export";



                    //.exe is in root directory
                    if (File.Exists(temp))
                        lines = File.ReadAllLines(temp);
                    //.exe is in a /bin/ directory
                    else if (File.Exists(temp = "../" + temp))
                        lines = File.ReadAllLines(temp);
                    //.exe is in Visual Studio's /bin/debug or /bin/release
                    else if (File.Exists(temp = "../" + temp))
                        lines = File.ReadAllLines(temp);
                    else
                        throw new FileNotFoundException("Fatal Error: Cannot find file '" + s + ".export'");


                    foreach(var l in lines)
                    {
                        var split = l.Split(new char[] {' '});

                        if(unresolvedReferences.ContainsKey(split[2]))
                        {
                            var func = new CodeInformation.FunctionInformation { FunctionName = split[2], Ordinal = int.Parse(split[0]), Replacements = new List<int>()};
                            unresolvedReferences[split[2]] = func;
                            si.Functions.Add(func);
                        }
                    }

                    if(si.Functions.Count > 0)
                        ci.SymbolInfo.Add(si);
                }
            }
            catch(FileNotFoundException)
            {
                throw;
            }
            catch(Exception ex)
            {
                //should never happen if installed correctly
                throw new Exception(FormatError("Unable to read import files: " + ex.Message));
            }



            bool found_main = false;
            //first calculate byte offsets for everything
            offset = 0;
            for (int i = 0; i < unprocessed_code.Count; i++)
            {
                if (unprocessed_code[i].isLabel)
                {
                    if (unprocessed_code[i].ident.Equals("main"))
                    {
                        ci.EntryPoint = (uint)offset;
                        found_main = true;
                    }

                    labels.Add(unprocessed_code[i].ident, offset);
                }
                else
                {
                    for (int j = 0; j < x86InstructionSet.Instructions.x86Instructions.Length; j++)
                    {
                        if (unprocessed_code[i].ident.Equals(x86InstructionSet.Instructions.x86Instructions[j].mnemonic))
                        {
                            unprocessed_code[i].instruction = x86InstructionSet.Instructions.x86Instructions[j];
                            break;
                        }
                    }

                    if (unprocessed_code[i].instruction == null)
                        throw new Exception(FormatError("Invalid opcode.", unprocessed_code[i].main_token.lineNum, unprocessed_code[i].main_token.linePos));

                    if (!isValidArgument(unprocessed_code[i].arg0, unprocessed_code[i].instruction.arg1)
                        || !isValidArgument(unprocessed_code[i].arg1, unprocessed_code[i].instruction.arg2)
                        || !isValidArgument(unprocessed_code[i].arg2, unprocessed_code[i].instruction.arg3))
                    {
                        throw new Exception(FormatError("Invalid operand", unprocessed_code[i].main_token.lineNum, unprocessed_code[i].main_token.linePos));
                    }

                    //I swear officer, this hack isn't mine. Please don't arrest me.
                    //need to replace for external calls
                    if(unprocessed_code[i].ident.Equals("call"))
                    {
                        if(unprocessed_code[i].arg0.ident != null)
                        {
                            if(unresolvedReferences[unprocessed_code[i].arg0.ident] != null)
                            {
                                //external code
                                unprocessed_code[i].arg0.offsetValue = 1;

                                unresolvedReferences[unprocessed_code[i].arg0.ident].Replacements.Add(offset + 2);
                            }
                        }
                    }
                    else if(unprocessed_code[i].ident.Equals("mov"))
                    {
                        if (unprocessed_code[i].arg1.ident != null)
                        {
                            //can be either a string, label, or external function
                            if (unresolvedReferences[unprocessed_code[i].arg1.ident] != null)
                            {
                                unprocessed_code[i].arg1.offsetValue = 1;
                                //external function
                                unresolvedReferences[unprocessed_code[i].arg1.ident].Replacements.Add(offset + 2);
                            }
                            else if (stringTable.ContainsKey(unprocessed_code[i].arg1.ident))
                            {
                                unprocessed_code[i].arg1.offsetValue = 1;
                                //string table
                                stringTable[unprocessed_code[i].arg1.ident].Add(offset + 2);
                            }
                            //labels handled elsewhere
                        }
                    }
                    //replace string table calls
                    else if(unprocessed_code[i].ident.Equals("push"))
                    {
                        if(unprocessed_code[i].arg0.ident != null)
                        {
                            if(stringTable.ContainsKey(unprocessed_code[i].arg0.ident))
                            {
                                //string table
                                unprocessed_code[i].arg0.offsetValue = 1;

                                stringTable[unprocessed_code[i].arg0.ident].Add(offset + 1);
                            }
                        }
                    }

                    unprocessed_code[i].offset = offset;
                    offset += unprocessed_code[i].instruction.numberOfBytes(unprocessed_code[i].arg0, unprocessed_code[i].arg1, unprocessed_code[i].arg2);
                }
            }

            if (!found_main)
                throw new Exception(FormatError("Could not find entrypoint. Expected 'main' label."));


            //remove all resolved external references/string references/label references
            foreach(var key in unresolvedReferences.Keys.ToArray())
            {
                if (unresolvedReferences[key] != null)
                    unresolvedReferences.Remove(key);
                else if (stringTable.ContainsKey(key))
                    unresolvedReferences.Remove(key);
                else if (labels.ContainsKey(key))
                    unresolvedReferences.Remove(key);

            }
            if (unresolvedReferences.Count > 0)
            {
                foreach (var v in unresolvedReferences)
                {
                    warnings.Add(FormatWarning("Unresolved reference to '" + v.Key + "'"));
                }
                throw new Exception(FormatError("Cannot continue compilation."));
            }


            //"The magnitude of this hack compares favorably with that of the national debt." - A Microsoft Programmer

            //resolve references for internal calls and jumps
            foreach(var unpr in unprocessed_code)
            {
                if(unpr.ident.Equals("push") && unpr.arg0.type == x86InstructionSet.InstructionArg.LABEL)
                {
                    if (unpr.arg0.offsetValue != 1)
                    {
                        if (labels.ContainsKey(unpr.arg0.ident))
                        {
                            unpr.arg0.value = labels[unpr.arg0.ident] - (unpr.offset + unpr.instruction.numberOfBytes(unpr.arg0, unpr.arg1, unpr.arg2));
                            unpr.arg0.type = x86InstructionSet.InstructionArg.IMM32;
                        }
                        else
                        {
                            throw new Exception(FormatError("Unresolved reference '" + unpr.arg0.ident + "'", unpr.main_token.lineNum, unpr.main_token.linePos));
                        }
                    }
                }
                else if(unpr.ident.Equals("mov"))
                {
                    //if not external
                    if (unpr.arg1.ident != null && unpr.arg1.offsetValue != 1)
                    {
                        if (labels.ContainsKey(unpr.arg1.ident))
                        {
                            unpr.arg1.value = labels[unpr.arg1.ident] - (unpr.offset + unpr.instruction.numberOfBytes(unpr.arg0, unpr.arg1, unpr.arg2));
                        }
                        else
                        {
                            throw new Exception(FormatError("Unresolved reference '" + unpr.arg1.ident + "'", unpr.main_token.lineNum, unpr.main_token.linePos));
                        }
                    }
                }
                else if(unpr.ident.Equals("call") || unpr.ident.Equals("jmp") || unpr.ident.Equals("jg") 
                    || unpr.ident.Equals("je") || unpr.ident.Equals("jge") || unpr.ident.Equals("jl")
                    || unpr.ident.Equals("jle") || unpr.ident.Equals("jne") || unpr.ident.Equals("loop")
                    || unpr.ident.Equals("loope") || unpr.ident.Equals("loopne"))
                {
                    //if not external
                    if (unpr.arg0.offsetValue != 1)
                    {
                        if (labels.ContainsKey(unpr.arg0.ident))
                        {
                            unpr.arg0.value = labels[unpr.arg0.ident] - (unpr.offset + unpr.instruction.numberOfBytes(unpr.arg0, unpr.arg1, unpr.arg2));
                        }
                        else
                        {
                            throw new Exception(FormatError("Unresolved reference '" + unpr.arg0.ident + "'", unpr.main_token.lineNum, unpr.main_token.linePos));
                        }
                    }
                }
            }

            UnprocessedInstruction last_inst = null;
            try
            {
                //assemble everything
                foreach (var unpr in unprocessed_code)
                {
                    last_inst = unpr;
                    if(!unpr.isLabel)
                        code.AddRange(unpr.instruction.assemble(unpr.arg0, unpr.arg1, unpr.arg2));
                }
            }
            catch(Exception ex)
            {
                throw new Exception(FormatError(ex.Message, last_inst.main_token.lineNum, last_inst.main_token.linePos), ex);
            }
            return;
        }


        private void Expect(string value, string error, bool increment = true, bool fatal = true)
        {
            if (increment)
            {
                if (++index >= tokens.Count)
                {
                    throw new Exception(
                        FormatError("Unexpected end of file",
                        tokens[index - 1].lineNum,
                        tokens[index - 1].linePos));
                }
            }

            if (!tokens[index].Equals(value))
            {

                if (fatal)
                {
                    throw new Exception(
                        FormatError(error,
                        tokens[index].lineNum,
                        tokens[index].linePos));
                }
                else
                {
                    warnings.Add(
                        FormatWarning(error,
                        tokens[index].lineNum,
                        tokens[index].linePos));
                }
            }
        }
        private void Expect(Type type, string error, bool increment = true, bool fatal = true)
        {
            if (increment)
            {
                if (++index >= tokens.Count)
                {
                    throw new Exception(
                        FormatError("Unexpected end of file",
                        tokens[index - 1].lineNum,
                        tokens[index - 1].linePos));
                }
            }
            if (tokens[index].type != type)
            {
                if (fatal)
                {
                    throw new Exception(
                        FormatError(error,
                        tokens[index].lineNum,
                        tokens[index].linePos));
                }
                else
                {
                    warnings.Add(
                        FormatWarning(error,
                        tokens[index].lineNum,
                        tokens[index].linePos));
                }
            }
        }

        //little hack to deal with my all-in-one Scanner class
        //I don't have any way to know if there's a new line other than just get the difference
        private bool nextTokenIsNextLine()
        {
            //no more tokens = it doesn't matter
            if (index + 1 >= tokens.Count)
                return true;

            return tokens[index].lineNum < tokens[index + 1].lineNum;
        }

        private bool isValidArgument(x86InstructionSet.Arg arg, int value)
        {
            return !( 
                //no arg, but an arg is required
                ((arg == null && value != x86InstructionSet.InstructionArg.NONE)
                //an arg, but it doesn't match
                || (arg != null && (arg.type & value) == 0))
                );
        }

        private string FormatWarning(string warning, int lineCounter, int linePosition)
        {
            return String.Format("{0}:{1}:{2}: Warning: {3}", shortname, lineCounter, linePosition, warning);
        }
        private string FormatError(string error, int lineCounter, int linePosition)
        {
            return String.Format("{0}:{1}:{2} Parse Error: {3}", shortname, lineCounter, linePosition, error);
        }
        private string FormatError(string error)
        {
            return String.Format("{0}: Parse Error: {1}", shortname, error);
        }
        private string FormatWarning(string warning)
        {
            return String.Format("{0}: Warning: {1}", shortname, warning);
        }
    }
}
