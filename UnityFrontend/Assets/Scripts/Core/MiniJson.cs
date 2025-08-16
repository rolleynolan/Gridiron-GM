using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Gridiron.Core
{
    // Minimal JSON parser based on MiniJSON (https://gist.github.com/darktable/1411710)
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            return Parser.Parse(json);
        }

        private sealed class Parser : IDisposable
        {
            private readonly string json;
            private int index;

            private Parser(string json)
            {
                this.json = json;
            }

            public static object Parse(string json)
            {
                using (var parser = new Parser(json))
                {
                    return parser.ParseValue();
                }
            }

            public void Dispose()
            {
            }

            private char PeekChar()
            {
                return json[index];
            }

            private char NextChar()
            {
                return json[index++];
            }

            private string NextWord()
            {
                var sb = new StringBuilder();
                while (index < json.Length && !char.IsWhiteSpace(PeekChar()) && "{}[],:\"".IndexOf(PeekChar()) == -1)
                {
                    sb.Append(NextChar());
                }
                return sb.ToString();
            }

            private void EatWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(PeekChar()))
                {
                    index++;
                }
            }

            private object ParseValue()
            {
                EatWhitespace();
                if (index == json.Length) return null;
                char c = PeekChar();
                switch (c)
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return ParseString();
                    default:
                        return ParseNumberOrWord();
                }
            }

            private object ParseNumberOrWord()
            {
                string word = NextWord();
                if (word == "true") return true;
                if (word == "false") return false;
                if (word == "null") return null;
                if (long.TryParse(word, out var l)) return l;
                if (double.TryParse(word, out var d)) return d;
                return word;
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                NextChar(); // skip opening quote
                while (index < json.Length)
                {
                    char c = NextChar();
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        if (index == json.Length) break;
                        c = NextChar();
                        switch (c)
                        {
                            case '\"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                var hex = new char[4];
                                for (int i = 0; i < 4; i++) hex[i] = NextChar();
                                sb.Append((char)Convert.ToInt32(new string(hex), 16));
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }

            private IDictionary ParseObject()
            {
                var dict = new Dictionary<string, object>();
                NextChar(); // skip '{'
                while (true)
                {
                    EatWhitespace();
                    if (index < json.Length && PeekChar() == '}')
                    {
                        NextChar();
                        break;
                    }
                    string key = ParseString();
                    EatWhitespace();
                    NextChar(); // skip ':'
                    object value = ParseValue();
                    dict[key] = value;
                    EatWhitespace();
                    if (index < json.Length && PeekChar() == ',')
                    {
                        NextChar();
                    }
                }
                return dict;
            }

            private IList ParseArray()
            {
                var list = new List<object>();
                NextChar(); // skip '['
                bool done = false;
                while (!done && index < json.Length)
                {
                    EatWhitespace();
                    if (index < json.Length && PeekChar() == ']')
                    {
                        NextChar();
                        break;
                    }
                    list.Add(ParseValue());
                    EatWhitespace();
                    if (index < json.Length && PeekChar() == ',')
                    {
                        NextChar();
                    }
                }
                return list;
            }
        }
    }
}
