using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        public override bool Equals(object obj)
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


    internal class Scanner
    {
        private string filename;
        private string shortname;
        public List<string> warnings;
        public List<Token> tokens;

        public Scanner(string filename)
        {
            this.filename = filename;
            shortname = Path.GetFileName(filename);
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

                        for (;;)
                        {
                            if (reader.Peek() < 0)
                            {
                                warnings.Add(FormatWarning("EOF before end of block comment.", lineCounter, linePosition));
                                break;
                            }
                            if (next_char == '\n')
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
                        for (next_char = reader.Read();
                            reader.Peek() > -1 && next_char != '\n';
                            next_char = reader.Read()) ;

                        lineCounter++;
                        linePosition = 0;
                    }
                    else if (char.IsLetter((char) next_char) || next_char == '_' || next_char == '$')
                    {
                        temp_token = new Token {lineNum = lineCounter, linePos = linePosition, source = shortname};
                        StringBuilder str = new StringBuilder();
                        str.Append((char) next_char);

                        for (;;)
                        {
                            next_char = reader.Peek();
                            if (char.IsLetter((char) next_char) || next_char == '_' || next_char == '$' ||
                                char.IsDigit((char) next_char))
                            {
                                str.Append((char) reader.Read());
                                linePosition++;
                            }
                            else
                            {
                                break;
                            }
                        }

                        temp_token.obj = str.ToString();
                        temp_token.str_value = str.ToString();

                        tokens.Add(temp_token);
                    }
                    else if (char.IsDigit((char) next_char))
                    {
                        temp_token = new Token {lineNum = lineCounter, linePos = linePosition, source = shortname};

                        temp_token.obj = ParseDigits(reader, ref linePosition, ref lineCounter, (char) next_char);
                        temp_token.str_value = temp_token.obj.ToString();

                        tokens.Add(temp_token);
                    }
                    else if (next_char == '"')
                    {
                        temp_token = new Token {lineNum = lineCounter, linePos = linePosition, source = shortname};
                        StringBuilder str = new StringBuilder();

                        for (;;)
                        {
                            next_char = reader.Read();

                            linePosition++;
                            if (next_char == -1)
                            {
                                throw new Exception(FormatError("Unexpected EOF in string literal.", lineCounter,
                                    linePosition));
                            }
                            if (next_char == '"')
                            {
                                break;
                            }
                            if (next_char == '\r')
                            {
                                continue;
                            }
                            if (next_char == '\n')
                            {
                                throw new Exception(FormatError("Unexpected newline in a string literal.", lineCounter,
                                    linePosition));
                            }
                            if (next_char == '\\')
                            {
                                next_char = reader.Read();
                                linePosition++;

                                if (next_char == 'x')
                                {
                                    int val = ReadHexadecimal(reader, ref linePosition, ref lineCounter, false);
                                    str.Append((char)val);
                                }
                                else if (next_char == 'u')
                                {
                                    int val = ReadHexadecimal(reader, ref linePosition, ref lineCounter, true);
                                    str.Append((char)val);
                                }
                                else if (next_char == '\'' || next_char == '"' || next_char == '\\')
                                {
                                    str.Append((char) next_char);
                                }
                                else
                                {
                                    char ch_code = MapCharCode(next_char);

                                    if (ch_code == (char) next_char)
                                    {
                                        warnings.Add(FormatWarning(
                                            string.Format("Unknown control code '\\{0}'. Replacing with '{0}'.", ch_code),
                                            lineCounter,
                                            linePosition));
                                    }
                                    str.Append(ch_code);
                                }
                            }
                            else
                            {
                                str.Append((char) next_char);
                            }
                        }


                        temp_token.obj = str;
                        temp_token.str_value = str.ToString();
                        tokens.Add(temp_token);
                    }
                    else if (IsValidSymbol(next_char))
                    {
                        tokens.Add(new Token
                        {
                            obj = new[] {(char) next_char},
                            str_value = ((char) next_char).ToString(),
                            source = shortname,
                            linePos = linePosition,
                            lineNum = lineCounter
                        });
                    }
                    else
                    {
                        warnings.Add(FormatWarning(
                            string.Format("Ignoring invalid character \'{0}\' (0x{1}).", (char) next_char,
                                next_char.ToString("X2")),
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
                    reader.Read();
                    linePosition++;
                    return ParseBoolean(reader, ref linePosition, ref lineCounter);
                }
                if (reader.Peek() == 'x')
                {
                    reader.Read();
                    linePosition++;
                    return ParseHexadecimal(reader, ref linePosition, ref lineCounter);
                }
                return ParseOctal(reader, ref linePosition, ref lineCounter);
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(first_char);
            bool has_decimal = false;

            while (reader.Peek() != -1)
            {
                if (reader.Peek() == '.')
                {
                    if (has_decimal)
                    {
                        break;
                    }
                    has_decimal = true;
                    sb.Append('.');
                    reader.Read();
                    linePosition++;
                }
                else if (char.IsDigit((char) reader.Peek()))
                {
                    sb.Append((char) reader.Read());
                    linePosition++;
                }
                else
                {
                    break;
                }
            }
            int type = 0;
            try
            {
                if (reader.Peek() == 'd' || reader.Peek() == 'D')
                {
                    type = 3;
                    return double.Parse(sb.ToString());
                }
                if (reader.Peek() == 'f' || reader.Peek() == 'F' || has_decimal)
                {
                    type = 2;
                    return float.Parse(sb.ToString());
                }
                if (reader.Peek() == 'l' || reader.Peek() == 'L')
                {
                    type = 1;
                    return ulong.Parse(sb.ToString());
                }
                return uint.Parse(sb.ToString());
            }
            catch (OverflowException)
            {
                throw new Exception(FormatError(
                    string.Format("The number '{0}' is too large to be stored in a {1}", sb,
                        (type == 0 ? "int" : (type == 1 ? "long" : (type == 2 ? "float" : "double")))),
                    lineCounter,
                    linePosition));
            }
            catch (Exception ex)
            {
                throw new Exception(FormatError(string.Format("{0} occured while parsing a number.", ex.Message),
                    lineCounter, linePosition));
            }
        }

        private object ParseHexadecimal(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char) reader.Peek();
                if ((ch - '0' >= 0 && ch - '0' <= 9) ||
                    (ch - 'a' >= 0 && ch - 'a' <= 5) ||
                    (ch - 'A' >= 0 && ch - 'A' <= 5))
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else
                {
                    break;
                }
            }

            if (sb.Length == 0)
            {
                throw new Exception(FormatError("Invalid hexadecimal constant. Found \'0x\' but no value after.",
                    lineCounter, linePosition));
            }
            try
            {
                return Convert.ToUInt32(sb.ToString(), 16);
            }
            catch (OverflowException)
            {
                throw new Exception(FormatError(
                    string.Format("The number '{0}' is too large to be stored in an integer.", sb),
                    lineCounter,
                    linePosition));
            }
        }

        private object ParseOctal(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char) reader.Peek();
                if (ch - '0' >= 0 && ch - '0' <= 7)
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else
                {
                    break;
                }
            }

            if (sb.Length == 0)
            {
                return 0;
            }
            try
            {
                return Convert.ToUInt32(sb.ToString(), 8);
            }
            catch (OverflowException)
            {
                throw new Exception(FormatError(
                    string.Format("The number '0{0}' is too large to be stored in an integer.", sb),
                    lineCounter,
                    linePosition));
            }
        }

        private object ParseBoolean(StreamReader reader, ref int linePosition, ref int lineCounter)
        {
            StringBuilder sb = new StringBuilder();

            while (reader.Peek() != -1)
            {
                char ch = (char) reader.Peek();
                if (ch == '0' || ch == '1')
                {
                    sb.Append(ch);
                    reader.Read();
                    linePosition++;
                }
                else
                {
                    break;
                }
            }

            if (sb.Length == 0)
            {
                throw new Exception(FormatError("Invalid binary constant. Found \'0b\' but no value after.", lineCounter,
                    linePosition));
            }
            try
            {
                return Convert.ToUInt32(sb.ToString(), 2);
            }
            catch (OverflowException)
            {
                throw new Exception(FormatError(
                    string.Format("The number '0b{0}' is too large to be stored in an integer.", sb),
                    lineCounter,
                    linePosition));
            }
        }

        private int ReadHexadecimal(StreamReader reader, ref int linePosition, ref int lineCounter, bool wide_char)
        {
            int char_size = (wide_char ? 4 : 2);

            StringBuilder sb = new StringBuilder();
            int j;
            int c;

            //attempt to get the next characters
            for (j = 0; j < char_size; j++)
            {
                c = reader.Peek();
                if (c < 0)
                {
                    throw new Exception(FormatError("Unexpected EOF", lineCounter, linePosition));
                }
                if (!((c - '0' >= 0 && c - '0' <= 9) ||
                      (c - 'a' >= 0 && c - 'a' <= 5) ||
                      (c - 'A' >= 0 && c - 'A' <= 5)))
                {
                    break;
                }
                if (c == '"')
                {
                    break;
                }

                linePosition++;

                sb.Append((char) reader.Read());
            }
            if (j == 0)
            {
                warnings.Add(FormatWarning(
                    "Expected hexadecimal constant after " + (wide_char ? "'\\u'": "'\\x'") + ". Assuming 0.",
                    lineCounter,
                    linePosition));
                return 0;
            }

            return Convert.ToInt32(sb.ToString(), 16);
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
            return string.Format("{0}:{1}:{2}: Warning: {3}", shortname, lineCounter, linePosition, warning);
        }

        private string FormatError(string error, int lineCounter, int linePosition)
        {
            return string.Format("{0}:{1}:{2} Syntax Error: {3}", shortname, lineCounter, linePosition, error);
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
                    return (char) c;
            }
        }
    }
}