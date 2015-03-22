using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Assembler
{
    class Token
    {
        public string str_value;
        public object obj;

        public int lineNum;
        public int linePos;
        public string source;

        public Type type
        {
            get
            {
                return obj.GetType();
            }
        }

        public static implicit operator string(Token t)
        {
            return t.str_value;
        }
        public override bool Equals(Object obj)
        {
            return str_value.Equals(obj);
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


    class Scanner
    {
        private string filename;
        private string shortname;
        public List<string> warnings;
        public List<Token> tokens;

        public Scanner(string filename)
        {
            this.filename = filename;
            this.shortname = Path.GetFileName(filename);
            warnings = new List<string>();
        }
        public List<Token> Scan()
        {
            tokens = new List<Token>();

            Token temp_token;

            using (StreamReader reader = new StreamReader(filename))
            {
                int next_char = -1;

                int lineCounter = 1; //every newline
                int linePosition = 0; //position on the line

                while ((next_char = reader.Read()) > -1)
                {
                    linePosition++;
                    if (next_char == ' ' || next_char == '\r' || next_char == '\t')
                    {
                        continue;
                    }
                    else if (next_char == '\n')
                    {
                        lineCounter++;
                        linePosition = 0;
                    }
                    else if (next_char == '/' && reader.Peek() == '*')
                    {
                        reader.Read();
                        next_char = reader.Read();
                        linePosition += 2;

                        for (; ; )
                        {
                            if (reader.Peek() < 0)
                            {
                                warnings.Add(FormatWarning("EOF before end of block comment.", lineCounter, linePosition));
                                break;
                            }
                            else if (next_char == '\n')
                            {
                                lineCounter++;
                                linePosition = 0;
                            }
                            else if (next_char == '*' && reader.Peek() == '/')
                            {
                                reader.Read();
                                linePosition++;
                                break;
                            }
                            next_char = reader.Read();
                            linePosition++;
                        }
                    }
                    else if ((next_char == '/' && reader.Peek() == '/') || next_char == ';')
                    {
                        for (next_char = reader.Read(); reader.Peek() > -1 && next_char != '\n'; next_char = reader.Read()) ;
                        lineCounter++;
                        linePosition = 0;
                    }
                    else if (char.IsLetter((char)next_char) || next_char == '_' || next_char == '$')
                    {
                        temp_token = new Token { lineNum = lineCounter, linePos = linePosition, source = shortname };
                        StringBuilder str = new StringBuilder();
                        str.Append((char)next_char);

                        for (; ; )
                        {
                            next_char = reader.Peek();
                            if (Char.IsLetter((char)next_char) || next_char == '_' || next_char == '$' || Char.IsDigit((char)next_char))
                            {
                                str.Append((char)reader.Read());
                                linePosition++;
                            }
                            else
                                break;
                        }

                        temp_token.obj = str.ToString();
                        temp_token.str_value = str.ToString();

                        tokens.Add(temp_token);
                    }
                    else if (char.IsDigit((char)next_char))
                    {
                        temp_token = new Token { lineNum = lineCounter, linePos = linePosition, source = shortname };

                        temp_token.obj = ParseDigits(reader, ref linePosition, ref lineCounter, (char)next_char);
                        temp_token.str_value = temp_token.obj.ToString();

                        tokens.Add(temp_token);
                    }
                    else if (next_char == '"')
                    {
                        temp_token = new Token { lineNum = lineCounter, linePos = linePosition, source = shortname };
                        StringBuilder str = new StringBuilder();

                        StringBuilder raw_str = new StringBuilder();

                        int start_line = lineCounter;
                        int start_position = linePosition;

                        for (; ; )
                        {

                            next_char = reader.Read();
                            linePosition++;
                            if (next_char == -1)
                                throw new Exception(FormatError("Unexpected EOF in string literal.", lineCounter, linePosition));
                            else if (next_char == '"')
                                break;
                            else if (next_char == '\r')
                                continue;
                            else if (next_char == '\n')
                            {
                                lineCounter++;
                                linePosition = 0;
                            }
                            else if (next_char == '\\')
                            {
                                next_char = reader.Read(); linePosition++;

                                if (next_char == 'x')
                                {
                                    int val = ReadHexadecimal(reader, ref linePosition, ref lineCounter, '"');
                                    if (val < 0)
                                    {
                                        str.Append((char)0);
                                        raw_str.Append("\\x");
                                    }
                                    else
                                    {
                                        str.Append((char)val);
                                        raw_str.AppendFormat("\\x{0:X}", val);
                                    }
                                }
                                else if (next_char == '\'' || next_char == '"' || next_char == '\\')
                                {
                                    str.Append((char)next_char);
                                    raw_str.AppendFormat("\\{0}", (char)next_char);
                                }
                                else
                                {
                                    char ch_code = MapCharCode(next_char);

                                    if (ch_code == (char)next_char)
                                    {
                                        warnings.Add(FormatWarning(
                                            String.Format("Unknown control code '\\{0}'. Replacing with '{0}'.", ch_code),
                                            lineCounter,
                                            linePosition));
                                    }
                                    str.Append(ch_code);
                                    raw_str.AppendFormat("\\{0}", (char)next_char);
                                }
                            }
                            else
                            {
                                str.Append((char)next_char);
                                raw_str.Append((char)next_char);
                            }
                        }

                        if (lineCounter > start_line)
                        {
                            warnings.Add(FormatWarning(
                                String.Format("Detected runaway multi-line string literal. String starts on line: {0}:{1}.", start_line, start_position),
                                lineCounter,
                                linePosition));
                        }

                        temp_token.obj = str;
                        temp_token.str_value = raw_str.ToString();
                        tokens.Add(temp_token);
                    }
                    else if (next_char == '\'')
                    {
                        temp_token = new Token { lineNum = lineCounter, linePos = linePosition, source = shortname };

                        if (reader.Peek() < 0) throw new Exception(FormatError("Unexpected EOF while parsing a character literal.", lineCounter, linePosition));
                        char c = (char)reader.Read();

                        if (c == '\\')
                        {
                            if (reader.Peek() == 'x')
                            {
                                reader.Read();
                                int val = ReadHexadecimal(reader, ref linePosition, ref lineCounter, '\'');
                                if (val >= 0)
                                {
                                    c = (char)val;
                                }
                                else
                                {
                                    c = '\0';
                                }

                                temp_token.str_value = String.Format("'\\x{0:X}'", val);
                            }
                            else if (reader.Peek() == '\'' || reader.Peek() == '"' || reader.Peek() == '\\')
                            {
                                c = (char)reader.Read();
                                linePosition++;
                                temp_token.str_value = "'\\" + c + "'";
                            }
                            else
                            {
                                char ch_code = MapCharCode(reader.Peek());
                                c = (char)reader.Read(); linePosition++;

                                if (ch_code == (char)next_char)
                                {
                                    warnings.Add(FormatWarning(
                                        String.Format("Unknown control code '\\{0}'. Replacing with '{0}'.", ch_code),
                                        lineCounter,
                                        linePosition));
                                }
                                temp_token.str_value = "'\\" + c + "'";
                                c = ch_code;
                            }
                        }
                        else
                        {
                            temp_token.str_value = "'" + c + "'";
                        }
                        if (reader.Peek() != (int)'\'')
                            throw new Exception(FormatError("Missing end quote for char literal.", lineCounter, linePosition));

                        reader.Read(); linePosition++;

                        temp_token.obj = (char)c;

                        tokens.Add(temp_token);

                    }
                    else if (IsValidSymbol(next_char))
                    {
                        tokens.Add(new Token { obj = new char[] { (char)next_char }, str_value = ((char)next_char).ToString(), source = shortname, linePos = linePosition, lineNum = lineCounter });
                    }
                    else
                    {
                        warnings.Add(FormatWarning(
                            String.Format("Ignoring invalid character \'{0}\' (0x{1}).", (char)next_char, next_char.ToString("X2")),
                            lineCounter,
                            linePosition));
                    }
                }
            }
            return tokens;
        }

        private object ParseDigits(StreamReader reader, ref int linePosition, ref int lineCounter, char first_char)
        {
            if (first_char == '0')
            {
                if (reader.Peek() == 'b')
                {
                    reader.Read(); linePosition++;
                    return ParseBoolean(reader, ref linePosition, ref lineCounter);
                }
                else if (reader.Peek() == 'x')
                {
                    reader.Read(); linePosition++;
                    return ParseHexadecimal(reader, ref linePosition, ref lineCounter);
                }
                else
                {
                    return ParseOctal(reader, ref linePosition, ref lineCounter);
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(first_char);
                bool has_decimal = false;

                while (reader.Peek() != -1)
                {
                    if (reader.Peek() == '.')
                    {
                        if (has_decimal) break;
                        has_decimal = true;
                        sb.Append('.');
                        reader.Read(); linePosition++;
                    }
                    else if (char.IsDigit((char)reader.Peek()))
                    {
                        sb.Append((char)reader.Read()); linePosition++;
                    }
                    else break;
                }
                int type = 0;
                try
                {
                    if (reader.Peek() == 'd' || reader.Peek() == 'D')
                    {
                        type = 3;
                        return double.Parse(sb.ToString());
                    }
                    else if (reader.Peek() == 'f' || reader.Peek() == 'F' || has_decimal)
                    {
                        type = 2;
                        return float.Parse(sb.ToString());
                    }
                    else if (reader.Peek() == 'l' || reader.Peek() == 'L')
                    {
                        type = 1;
                        return ulong.Parse(sb.ToString());
                    }
                    else
                    {
                        return uint.Parse(sb.ToString());
                    }
                }
                catch (OverflowException)
                {
                    throw new Exception(FormatError(
                        String.Format("The number '{0}' is too large to be stored in a {1}", sb.ToString(), (type == 0 ? "int" : (type == 1 ? "long" : (type == 2 ? "float" : "double")))),
                        lineCounter,
                        linePosition));
                }
                catch (Exception ex)
                {
                    throw new Exception(FormatError(String.Format("{0} occured while parsing a number.", ex.Message), lineCounter, linePosition));
                }
            }
        }

        private object ParseHexadecimal(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char)reader.Peek();
                if ((ch - '0' >= 0 && ch - '0' <= 9) ||
                    (ch - 'a' >= 0 && ch - 'a' <= 5) ||
                    (ch - 'A' >= 0 && ch - 'A' <= 5))
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else break;
            }

            if (sb.Length == 0)
                throw new Exception(FormatError("Invalid hexadecimal constant. Found \'0x\' but no value after.", lineCounter, linePosition));
            else
            {
                try
                {
                    return Convert.ToUInt32(sb.ToString(), 16);
                }
                catch (OverflowException)
                {
                    throw new Exception(FormatError(
                        String.Format("The number '{0}' is too large to be stored in an integer.", sb.ToString()),
                        lineCounter,
                        linePosition));
                }
            }
        }

        private object ParseOctal(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char)reader.Peek();
                if (ch - '0' >= 0 && ch - '0' <= 7)
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else break;
            }

            if (sb.Length == 0)
                return 0;
            else
            {
                try
                {
                    return Convert.ToUInt32(sb.ToString(), 8);
                }
                catch (OverflowException)
                {
                    throw new Exception(FormatError(
                        String.Format("The number '0{0}' is too large to be stored in an integer.", sb.ToString()),
                        lineCounter,
                        linePosition));
                }
            }
        }

        private object ParseBoolean(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char)reader.Peek();
                if (ch == '0' || ch == '1')
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else break;
            }

            if (sb.Length == 0)
                throw new Exception(FormatError("Invalid binary constant. Found \'0b\' but no value after.", lineCounter, linePosition));
            else
            {
                try
                {
                    return Convert.ToUInt32(sb.ToString(), 2);
                }
                catch (OverflowException)
                {
                    throw new Exception(FormatError(
                        String.Format("The number '0b{0}' is too large to be stored in an integer.", sb.ToString()),
                        lineCounter,
                        linePosition));
                }
            }
        }

        private int ReadHexadecimal(StreamReader reader, ref int linePosition, ref int lineCounter, char endChar)
        {
            int char_size = 2; //4 for wide chars (not yet supported)

            string s = "";
            int j;
            int c;

            //attempt to get the next characters
            for (j = 0; j < char_size; j++)
            {
                c = reader.Peek();
                if (c < 0 || c == endChar)
                {
                    break;
                }

                linePosition++;

                s += (char)reader.Read();
            }
            if (j == 0)
            {
                warnings.Add(FormatWarning(
                    String.Format("Expected hexadecimal constant after '\\x'. Assuming 0."),
                    lineCounter,
                    linePosition));
                return -1;
            }

            return Convert.ToInt32(s, 16);
        }

        private int GetHexValue(byte p)
        {
            switch ((char)p)
            {
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return (int)p - '0';
                case 'A':
                case 'a':
                    return 10;
                case 'b':
                case 'B':
                    return 11;
                case 'c':
                case 'C':
                    return 12;
                case 'd':
                case 'D':
                    return 13;
                case 'e':
                case 'E':
                    return 14;
                case 'f':
                case 'F':
                    return 15;
                default:
                    return 0;
            }
        }

        private bool IsValidSymbol(int c)
        {
            switch (c)
            {
                case '+':
                case '[':
                case ':':
                case ',':
                case '*':
                case ']':
                case '.':
                case '-':
                case '=':
                    return true;
                default:
                    return false;
            }
        }

        private string FormatWarning(string warning, int lineCounter, int linePosition)
        {
            return String.Format("{0}:{1}:{2}: Warning: {3}", shortname, lineCounter, linePosition, warning);
        }
        private string FormatError(string error, int lineCounter, int linePosition)
        {
            return String.Format("{0}:{1}:{2} Syntax Error: {3}", shortname, lineCounter, linePosition, error);
        }
        private char MapCharCode(int c)
        {
            switch (c)
            {
                case '\'':
                    return '\'';
                case '\\':
                    return '\\';
                case 'n':
                    return '\n';
                case 'r':
                    return '\r';
                case 'b':
                    return '\b';
                case 't':
                    return '\t';
                case 'f':
                    return '\f';
                case 'a':
                    return '\a';
                case 'v':
                    return '\v';
                default:
                    return (char)c;
            }

        }
    }
}