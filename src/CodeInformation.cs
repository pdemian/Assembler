using System.Collections.Generic;

namespace Assembler
{
    internal class CodeInformation
    {
        public byte[] Code;

        public uint EntryPoint;

        public uint FileAlignment;

        public uint SectionAlignment;

        public ushort Subsystem;

        public class SymbolInformation
        {
            public string LibraryName;
            public List<FunctionInformation> Functions;
        }

        public class FunctionInformation
        {
            public int Ordinal;
            public string FunctionName;
            public List<int> Replacements;
        }

        public class StringTableInformation
        {
            public string Text;
            public List<int> Replacements;
        }

        public List<SymbolInformation> SymbolInfo;

        public List<StringTableInformation> StringTable;

        public CodeInformation()
        {
            StringTable = new List<StringTableInformation>();
            SymbolInfo = new List<SymbolInformation>();
        }
    }
}