using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neko.VrmPhysicsTuner
{
    public sealed class JsonWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Stack<bool> _first = new Stack<bool>();
        private bool _pendingKey;

        private void BeforeValue()
        {
            if (_pendingKey) { _pendingKey = false; return; }
            if (_first.Count == 0) return;
            if (!_first.Peek()) _sb.Append(',');
            _first.Pop();
            _first.Push(false);
        }

        private void Push() { _first.Push(true); }

        public JsonWriter BeginObject() { BeforeValue(); _sb.Append('{'); Push(); return this; }
        public JsonWriter EndObject() { _sb.Append('}'); _first.Pop(); return this; }
        public JsonWriter BeginArray() { BeforeValue(); _sb.Append('['); Push(); return this; }
        public JsonWriter EndArray() { _sb.Append(']'); _first.Pop(); return this; }

        public JsonWriter Key(string name)
        {
            BeforeValue();
            WriteString(name);
            _sb.Append(':');
            _pendingKey = true;
            return this;
        }

        public JsonWriter Value(string v)
        {
            BeforeValue();
            if (v == null) _sb.Append("null"); else WriteString(v);
            return this;
        }

        public JsonWriter Value(bool v)
        {
            BeforeValue();
            _sb.Append(v ? "true" : "false");
            return this;
        }

        public JsonWriter Value(int v)
        {
            BeforeValue();
            _sb.Append(v.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        public JsonWriter Value(float v)
        {
            BeforeValue();
            if (float.IsNaN(v) || float.IsInfinity(v)) _sb.Append('0');
            else _sb.Append(v.ToString("R", CultureInfo.InvariantCulture));
            return this;
        }

        public JsonWriter Null()
        {
            BeforeValue();
            _sb.Append("null");
            return this;
        }

        public JsonWriter Raw(string json)
        {
            BeforeValue();
            _sb.Append(json);
            return this;
        }

        private void WriteString(string s)
        {
            _sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\b': _sb.Append("\\b"); break;
                    case '\f': _sb.Append("\\f"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else _sb.Append(c);
                        break;
                }
            }
            _sb.Append('"');
        }

        public override string ToString() { return _sb.ToString(); }
    }

    public sealed class JsonParser
    {
        private readonly string _s;
        private int _i;

        private JsonParser(string s) { _s = s; _i = 0; }

        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new JsonParser(json);
            p.SkipWs();
            object v = p.ParseValue();
            p.SkipWs();
            if (p._i != p._s.Length) throw new FormatException("Trailing content at " + p._i);
            return v;
        }

        private void SkipWs()
        {
            while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
        }

        private object ParseValue()
        {
            if (_i >= _s.Length) throw new FormatException("Unexpected end of input");
            char c = _s[_i];
            switch (c)
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return ParseString();
                case 't': Expect("true"); return true;
                case 'f': Expect("false"); return false;
                case 'n': Expect("null"); return null;
                default: return ParseNumber();
            }
        }

        private void Expect(string lit)
        {
            if (_i + lit.Length > _s.Length || string.CompareOrdinal(_s, _i, lit, 0, lit.Length) != 0)
                throw new FormatException("Expected " + lit + " at " + _i);
            _i += lit.Length;
        }

        private Dictionary<string, object> ParseObject()
        {
            var map = new Dictionary<string, object>();
            _i++;
            SkipWs();
            if (_i < _s.Length && _s[_i] == '}') { _i++; return map; }
            while (true)
            {
                SkipWs();
                if (_i >= _s.Length || _s[_i] != '"') throw new FormatException("Expected key at " + _i);
                string key = ParseString();
                SkipWs();
                if (_i >= _s.Length || _s[_i] != ':') throw new FormatException("Expected ':' at " + _i);
                _i++;
                SkipWs();
                map[key] = ParseValue();
                SkipWs();
                if (_i >= _s.Length) throw new FormatException("Unterminated object");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == '}') { _i++; break; }
                throw new FormatException("Expected ',' or '}' at " + _i);
            }
            return map;
        }

        private List<object> ParseArray()
        {
            var list = new List<object>();
            _i++;
            SkipWs();
            if (_i < _s.Length && _s[_i] == ']') { _i++; return list; }
            while (true)
            {
                SkipWs();
                list.Add(ParseValue());
                SkipWs();
                if (_i >= _s.Length) throw new FormatException("Unterminated array");
                if (_s[_i] == ',') { _i++; continue; }
                if (_s[_i] == ']') { _i++; break; }
                throw new FormatException("Expected ',' or ']' at " + _i);
            }
            return list;
        }

        private string ParseString()
        {
            _i++;
            var sb = new StringBuilder();
            while (true)
            {
                if (_i >= _s.Length) throw new FormatException("Unterminated string");
                char c = _s[_i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                if (_i >= _s.Length) throw new FormatException("Unterminated escape");
                char e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 > _s.Length) throw new FormatException("Bad unicode escape");
                        sb.Append((char)Convert.ToInt32(_s.Substring(_i, 4), 16));
                        _i += 4;
                        break;
                    default: throw new FormatException("Bad escape \\" + e);
                }
            }
            return sb.ToString();
        }

        private object ParseNumber()
        {
            int start = _i;
            if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
            while (_i < _s.Length && ((_s[_i] >= '0' && _s[_i] <= '9') || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '-' || _s[_i] == '+')) _i++;
            if (start == _i) throw new FormatException("Invalid number at " + start);
            string token = _s.Substring(start, _i - start);
            double d;
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                throw new FormatException("Invalid number: " + token);
            return d;
        }
    }

    public static class JsonValueExtensions
    {
        public static Dictionary<string, object> AsObject(this object o)
        {
            var map = o as Dictionary<string, object>;
            if (map == null) throw new FormatException("Expected object");
            return map;
        }

        public static List<object> AsArray(this object o)
        {
            var list = o as List<object>;
            if (list == null) throw new FormatException("Expected array");
            return list;
        }

        public static string AsString(this object o, string fallback)
        {
            return o as string ?? fallback;
        }

        public static float AsFloat(this object o, float fallback)
        {
            if (o is double) return (float)(double)o;
            if (o is string)
            {
                float f;
                if (float.TryParse((string)o, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
            }
            return fallback;
        }

        public static int AsInt(this object o, int fallback)
        {
            if (o is double) return (int)(double)o;
            return fallback;
        }

        public static object Get(this Dictionary<string, object> map, string key)
        {
            object v;
            return map.TryGetValue(key, out v) ? v : null;
        }

        public static float[] AsFloatArray(this object o)
        {
            var list = o as List<object>;
            if (list == null) return null;
            var result = new float[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = list[i].AsFloat(0f);
            return result;
        }

        public static List<string> AsStringList(this object o)
        {
            var result = new List<string>();
            var list = o as List<object>;
            if (list == null) return result;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i] as string;
                if (s != null) result.Add(s);
            }
            return result;
        }
    }
}