using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lua
{
    /// <summary>
    /// lua词法解析器轻量版，专为解析LuaTable设计。
    /// </summary>
    class llex_lite
    {
        const int FIRST_RESERVED = 257;
        const int UCHAR_MAX = 0xff;

        /* ORDER RESERVED */
        static List<string> luaX_tokens = new List<string>(){
            "and", "break", "do", "else", "elseif",
            "end", "false", "for", "function", "goto", "if",
            "in", "local", "nil", "not", "or", "repeat",
            "return", "then", "true", "until", "while",
            "//", "..", "...", "==", ">=", "<=", "~=",
            "<<", ">>", "::", "<eof>",
            "<number>", "<integer>", "<name>", "<string>"
        };

        /*
        * WARNING: if you change the order of this enumeration,
        * grep "ORDER RESERVED"
        */
        enum RESERVED
        {
            /* terminal symbols denoted by reserved words */
            TK_AND = FIRST_RESERVED, TK_BREAK,
            TK_DO, TK_ELSE, TK_ELSEIF, TK_END, TK_FALSE, TK_FOR, TK_FUNCTION,
            TK_GOTO, TK_IF, TK_IN, TK_LOCAL, TK_NIL, TK_NOT, TK_OR, TK_REPEAT,
            TK_RETURN, TK_THEN, TK_TRUE, TK_UNTIL, TK_WHILE,
            /* other terminal symbols */
            TK_IDIV, TK_CONCAT, TK_DOTS, TK_EQ, TK_GE, TK_LE, TK_NE,
            TK_SHL, TK_SHR,
            TK_DBCOLON, TK_EOS,
            TK_FLT, TK_INT, TK_NAME, TK_STRING
        };

        /* number of reserved words */
        const int NUM_RESERVED = (int)RESERVED.TK_WHILE - FIRST_RESERVED + 1;

        static int isreserved(string ts)
        {
            return luaX_tokens.IndexOf(ts);
        }

        static bool currIsNewline(StreamReader sr)
        {
            return sr.Peek() == '\n' || sr.Peek() == '\r';
        }

        static void inclinenumber(StreamReader sr)
        {
            int old = sr.Peek();
            Debug.Assert(currIsNewline(sr));
            sr.Read();
            if (currIsNewline(sr) && sr.Peek() != old)
                sr.Read();
        }

        public static int next(StreamReader sr)
        {
            return sr.Read();
        }

        static List<char> buffer = new List<char>();
        static void resetbuffer()
        {
            buffer.Clear();
        }

        static void save_and_next(StreamReader sr)
        {
            buffer.Add((char)sr.Read());
        }

        static void remove(int n)
        {
            buffer.RemoveRange(buffer.Count - n, n);
        }

        static void save(int n)
        {
            char c = (char)n;
            buffer.Add(c);
        }

        static void save(char c)
        {
            buffer.Add(c);
        }

        public static string buff2str(int startindex = 0, int subcount = 0)
        {
            return new string(buffer.ToArray(), startindex, buffer.Count - subcount);
        }

        static bool lislalnum(int n)
        {
            char c = (char)n;
            return char.IsLetterOrDigit(c) || c == '_';
        }

        static bool lislalnum(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        static bool lislalpha(int n)
        {
            char c = (char)n;
            return char.IsLetter(c) || c == '_';
        }

        static bool lislalpha(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        static bool lisdigit(int n)
        {
            char c = (char)n;
            return char.IsDigit(c);
        }

        static bool lisdigit(char c)
        {
            return char.IsDigit(c);
        }

        static bool lisxdigit(int n)
        {
            char c = (char)n;
            return char.IsDigit(c) || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
        }

        static bool lisxdigit(char c)
        {
            return char.IsDigit(c) || ('a' <= c && c <= 'f') || ('A' <= c && c <= 'F');
        }

        static bool lisspace(int n)
        {
            char c = (char)n;
            return char.IsWhiteSpace(c);
        }

        static bool lisspace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        static bool check_next1(StreamReader sr, char c)
        {
            if ((char)sr.Peek() == c)
            {
                next(sr);
                return true;
            }
            else return false;
        }

        static bool check_next2(StreamReader sr, string s)
        {
            Debug.Assert(s.Length == 2);
            char c = (char)sr.Peek();
            if (c == s[0] || c == s[1])
            {
                save_and_next(sr);
                return true;
            }
            return false;
        }

        /*
        ** skip a sequence '[=*[' or ']=*]'; if sequence is well formed, return
        ** its number of '='s; otherwise, return a negative number (-1 iff there
        ** are no '='s after initial bracket)
        */
        static int skip_sep(StreamReader sr)
        {
            int count = 0;
            int s = sr.Peek();
            Debug.Assert(s == '[' || s == ']');
            save_and_next(sr);
            while ((char)sr.Peek() == '=')
            {
                save_and_next(sr);
                count++;
            }
            return sr.Peek() == s ? count : (-count) - 1;
        }

        static void esccheck(StreamReader sr, bool b, string msg)
        {
            if (!b)
            {
                if (!sr.EndOfStream)
                    save_and_next(sr); /* add current to buffer for error message */
                Console.Error.WriteLine(string.Format("{0} near <string>", msg));
            }
        }

        /* LUA_NUMBER */
        /*
         * this function is quite liberal in what it accepts, as 'lua0_str2num'
         * will reject ill-formed numerals.
         */
        static int read_numeral(StreamReader sr)
        {
            string expo = "Ee";
            int first = sr.Peek();
            Debug.Assert(lisdigit(first));
            save_and_next(sr);
            if (first == '0' && check_next2(sr, "xX")) /* hexadecimal? */
                expo = "Pp";
            for (; ; )
            {
                if (check_next2(sr, expo))/* exponent part? */
                    check_next2(sr, "-+");/* optional exponent sign */
                if (lisxdigit(sr.Peek()))
                    save_and_next(sr);
                else if (sr.Peek() == '.')
                    save_and_next(sr);
                else break;
            }
            //save('\0');
            //string numstr = buff2str();
            //int nret;
            //float fret;
            //if (int.TryParse(numstr, out nret))
            //    return (int)RESERVED.TK_INT;
            //else if (float.TryParse(numstr, out fret))
            //    return (int)RESERVED.TK_FLT;
            //else
            //    Console.Error.WriteLine("malformed number");
            return (int)RESERVED.TK_INT;
        }

        static void read_key(StreamReader sr)
        {
            next(sr);/* skip '[' */
            if (currIsNewline(sr))/* string starts with a newline? */
                inclinenumber(sr);/* skip it */
            while (!sr.EndOfStream)
            {
                switch ((char)sr.Peek())
                {
                    case ']':
                        next(sr);
                        return;
                    case '\"': case '\'':
                        next(sr);
                        break;
                    case '\n': case '\r':
                        inclinenumber(sr);
                        break;
                    default:
                        save_and_next(sr);
                        break;
                }
            }
            if (sr.EndOfStream)
            {
                Console.Error.WriteLine("unfinished long string");
            }
        }

        static int read_decesc(StreamReader sr)
        {
            int i;
            int r = 0; /* result accumulator */
            for (i = 0; i < 3 && lisdigit(sr.Peek()); i++)
            {
                r = 10 * r + sr.Peek() - '0';
                save_and_next(sr);
            }
            esccheck(sr, r <= UCHAR_MAX, "decimal escape too large");
            remove(i);
            return r;
        }

        static void read_table_asstring(StreamReader sr)
        {
            int bracket = 1;
            save_and_next(sr);/* keep delimiter (for error messages) */
            while (bracket > 0)
            {
                switch (sr.Peek())
                {
                    case '\0':
                        Console.Error.WriteLine(@"unfinished string. last char is \0");
                        break;/* to avoid warnings */
                    case '\n': case '\r':
                        Console.Error.WriteLine(@"unfinished string. last char is \n or \r");
                        break;/* to avoid warnings */
                    default:
                        if (sr.Peek() == '{')
                            bracket++;
                        else if (sr.Peek() == '}')
                            bracket--;
                        if (bracket == 0)
                            goto endloop;
                        save_and_next(sr);
                        break;
                }
            }
            endloop: save_and_next(sr); /* skip delimiter */
        }

        static void read_string(StreamReader sr, int del)
        {
            save_and_next(sr);/* keep delimiter (for error messages) */
            while (sr.Peek() != del)
            {
                switch (sr.Peek())
                {
                    case '\0':
                        Console.Error.WriteLine(@"unfinished string. last char is \0");
                        break;/* to avoid warnings */
                    case '\n': case '\r':
                        Console.Error.WriteLine(@"unfinished string. last char is \n or \r");
                        break;/* to avoid warnings */
                    default:
                        save_and_next(sr);
                        break;
                    #region 轻量版不处理转义，直接按照string读取，转义处理后会影响后续处理。
                    //case '\\':/* escape sequences */
                    //    {
                    //        int c = 0;/* final character to be saved */
                    //        save_and_next(sr);/* keep '\\' for error messages */
                    //        switch (sr.Peek())
                    //        {
                    //            case 'a': c = '\a'; goto read_save;
                    //            case 'b': c = '\b'; goto read_save;
                    //            case 'f': c = '\f'; goto read_save;
                    //            case 'n': c = '\n'; goto read_save;
                    //            case 'r': c = '\r'; goto read_save;
                    //            case 't': c = '\t'; goto read_save;
                    //            case 'v': c = '\v'; goto read_save;
                    //            //case 'x': c = read_hexaesc(sr); goto read_save;
                    //            //case 'u': utf8esc(sr); goto no_save;
                    //            case '\n':
                    //            case '\r':
                    //                inclinenumber(sr); c = '\n'; goto only_save;
                    //            case '\\':
                    //            case '\"':
                    //            case '\'':
                    //                c = sr.Peek(); goto read_save;
                    //            case '\0': goto no_save;/* will raise an error next loop */
                    //            case 'z':/* zap following span of sapces */
                    //                {
                    //                    remove(1);/* remove '\\' */
                    //                    next(sr);/* skip the 'z' */
                    //                    while (lisspace(sr.Peek()))
                    //                    {
                    //                        if (currIsNewline(sr)) inclinenumber(sr);
                    //                        else next(sr);
                    //                    }
                    //                    goto no_save;
                    //                }
                    //            default:
                    //                {
                    //                    esccheck(sr, lisdigit(sr.Peek()), "invalid escape sequence");
                    //                    c = read_decesc(sr);/* digital escape '\ddd' */
                    //                    goto only_save;
                    //                }
                    //        }
                    //        read_save:
                    //            next(sr);
                    //            /* go through */
                    //        only_save:
                    //            remove(1);/* remove '\\' */
                    //            save(c);
                    //            /* go through */
                    //        no_save: break;
                    //    }
                    #endregion
                }
            }
            save_and_next(sr); /* skip delimiter */
            //seminfo = buff2str(1, 2);
        }

        public static int llex(StreamReader sr, bool skip = false)
        {
            resetbuffer();
            while(!sr.EndOfStream)
            {
                switch(sr.Peek())
                {
                    case ',':
                        next(sr);
                        break;
                    case '=':
                        next(sr);
                        break;
                    case '\n': case '\r':/* 跳过换行符 */
                        inclinenumber(sr);
                        break;
                    case ' ': case '\f': case '\t': case '\v':/* 跳过空格 */
                        next(sr);
                        break;
                    case '-':/* 跳过注释，并将注释记录下来 */
                        next(sr);/* 跳过第一个-号 */
                        if (sr.Peek() == '-')
                        {
                            //轻量版只检查短注释
                            //不检查--[[...]]以及--[=*[...]=*]此类长注释
                            next(sr);/* 跳过第二个-号 */
                            while (!currIsNewline(sr) && sr.Peek() != '\0')
                                save_and_next(sr);
                            return (int)RESERVED.TK_STRING;
                        }
                        else
                            save('-');
                        break;
                    case '[':
                        read_key(sr);
                        return (int)RESERVED.TK_STRING;
                    case '{':
                        if (skip)/* skip '{' */
                            return next(sr);
                        read_table_asstring(sr);
                        return (int)RESERVED.TK_STRING;
                    case '"': case '\'':
                        read_string(sr, sr.Peek());
                        return (int)RESERVED.TK_STRING;
                    case '0': case '1': case '2': case '3': case '4':
                    case '5': case '6': case '7': case '8': case '9':
                        return read_numeral(sr);/* 读取数字 */
                    default:/* 读取变量名或其他零散字符，比如',' '}' '=' */
                        if (lislalpha(sr.Peek()))/* identifier or reserved word? */
                        {
                            do { save_and_next(sr); } while (lislalnum(sr.Peek()));
                            string seminfo = buff2str();
                            int n = isreserved(seminfo);
                            if (n > -1)
                                return n + FIRST_RESERVED;
                            else
                                return (int)RESERVED.TK_NAME;
                        }
                        return next(sr);
                }
            }
            return (int)RESERVED.TK_STRING;
        }
    }
}
