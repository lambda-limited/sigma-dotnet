using NodaTime;
using NodaTime.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sigma
{
    public class Writer
    {

        private static readonly char[] HEX_CHARS = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private readonly Stream output;
        private bool allowBytes;

        public Writer(Stream output)
        {
            this.output = output;
        }


        public void Write(object obj)
        {
            Write(obj, true);
        }

        public void Write(object obj, bool allowBytes)
        {
            this.allowBytes = allowBytes;
            WriteValue(obj);
        }

        private void WriteBase64(byte[] b)
        {
            WriteChar('*');
            if (b.Length > 0)
            {
                String bytes = Convert.ToBase64String(b);
                WriteChars(bytes);
            }
        }

        private void WriteByte(int b)
        {
            try
            {
                output.WriteByte((byte)b);
            }
            catch (IOException ex)
            {
                throw new SigmaException("Unable to write byte", ex);
            }
        }

        private void WriteBytes(byte[] b)
        {
            WriteChar('|');
            WriteChars(b.Length.ToString());
            WriteChar('|');
            if (b.Length > 0)
            {
                try
                {
                    output.Write(b, 0, b.Length);
                }
                catch (IOException ex)
                {
                    throw new SigmaException("Unable to write bytes", ex);
                }
            }
        }


        /*
        Encode character as UTF-8 byte sequence.  
         */
        private void WriteChar(int c)
        {
            if (c <= 0x7F)
            {
                WriteByte(c);
            }
            else if (c <= 0x7FF)
            {
                WriteByte(0xC0 | (c >> 6));
                WriteByte(0x80 | (c & 0x3F));
            }
            else if (c <= 0xFFFF)
            {
                WriteByte(0xE0 | (c >> 12));
                WriteByte(0x80 | ((c >> 6) & 0x3F));
                WriteByte(0x80 | (c & 0x3F));
            }
            else if (c <= 0x10FFFF)
            {
                WriteByte(0xF0 | (c >> 18));
                WriteByte(0x80 | ((c >> 12) & 0x3F));
                WriteByte(0x80 | ((c >> 6) & 0x3F));
                WriteByte(0x80 | (c & 0x3F));
            }
            else
            {
                throw new SigmaException("Character output of range for UTF-8 encoding");
            }
        }

        private void WriteChars(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            output.Write(bytes, 0, bytes.Length);
        }

        private void WriteList(ICollection list)
        {
            WriteChar('[');
            string separator = "";
            foreach (object value in list)
            {
                WriteChars(separator);
                WriteValue(value);
                separator = ",";
            }
            WriteChar(']');
        }

        private void WriteMap(IDictionary map)
        {
            string separator = "";
            WriteChar('{');
            foreach (DictionaryEntry kvp in map)
            {
                WriteChars(separator);
                WriteValue(kvp.Key);
                WriteChar('=');
                WriteValue(kvp.Value);
                separator = ",";
            }
            WriteChar('}');
        }


        private void WriteNumber(object obj)
        {
            WriteChars(obj.ToString());
        }

        private void WriteObject(object obj)
        {
            try
            {
                string typeName = Types.GetTypeName(obj.GetType());
                if (typeName == null)
                {
                    throw new SigmaException("No object type registered for class " + obj.GetType().Name);
                }
                string separator = "";
                WriteChars(typeName);
                WriteChar('{');
                PropertyInfo[] propInfo = obj.GetType().GetProperties();
                foreach (PropertyInfo p in propInfo)
                {
                    WriteChars(separator);
                    WriteChars(p.Name);
                    WriteChar('=');
                    object value = p.GetValue(obj, null);
                    WriteValue(value);
                    separator = ",";
                }
                WriteChar('}');
            }
            catch (IOException ex)
            {
                throw new SigmaException(ex.Message, ex);
            }
        }

        private List<int> Codepoints(string s)
        {
            List<int> cp = new List<int>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                cp.Add(Char.ConvertToUtf32(s, i));
                if (Char.IsHighSurrogate(s[i])) { ++i; }
            }
            return cp;
        }

        private void WriteString(string s)
        {
            WriteChar('"');
            foreach (int c in Codepoints(s))
            {
                switch (c)
                {
                    case '"':
                        WriteChars("\\\"");
                        break;
                    case '\\':
                        WriteChars("\\\\");
                        break;
                    case '\n':
                        WriteChars("\\n");
                        break;
                    case '\r':
                        WriteChars("\\r");
                        break;
                    case '\t':
                        WriteChars("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            WriteChars("\\u00");
                            WriteChar(HEX_CHARS[(c >> 4) & 0xF]);
                            WriteChar(HEX_CHARS[c & 0xF]);
                        }
                        else
                        {
                            WriteChar(c);
                        }
                        break;
                }
            }
            WriteChar('"');
        }

        /*
         * .NET date support is weak. 
         */
        private void WriteTemporal(object obj)
        {
            WriteChar('@');
            if (obj is DateTimeOffset dto)
            {
                OffsetDateTime odt = OffsetDateTime.FromDateTimeOffset(dto);
                WriteChars(odt.ToString(OffsetDateTimePattern.Rfc3339.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is DateTime dt)
            {
                LocalDateTime ldt = LocalDateTime.FromDateTime(dt);
                WriteChars(ldt.ToString(LocalDateTimePattern.ExtendedIso.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is ZonedDateTime zdt)
            {
                WriteChars(zdt.ToString("uuuu'-'MM'-'dd'T'HH':'mm':'ss;FFFFFFFFFo<Z+HH:mm>'['z']'", CultureInfo.InvariantCulture));
            }
            else if (obj is OffsetDateTime odt)
            {
                WriteChars(odt.ToString(OffsetDateTimePattern.Rfc3339.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is LocalDateTime ldt)
            {
                WriteChars(ldt.ToString(LocalDateTimePattern.ExtendedIso.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is LocalDate ld)
            {
                WriteChars(ld.ToString(LocalDatePattern.Iso.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is LocalTime lt)
            {
                WriteChars(lt.ToString(LocalTimePattern.ExtendedIso.PatternText, CultureInfo.InvariantCulture));
            }
            else if (obj is OffsetTime ot)
            {
                WriteChars(ot.ToString(OffsetTimePattern.Rfc3339.PatternText, CultureInfo.InvariantCulture));
            }
        }

        private void WriteValue(object value)
        {
            if (value == null)
            {
                WriteChars("&n");
            }
            else if (value is Boolean)
            {
                WriteChars((Boolean)value ? "&t" : "&f");
            }
            else if (value is string)
            {
                WriteString(value.ToString());
            }
            else if (Types.IsNumber(value))
            {
                WriteNumber(value);
            }
            else if (Types.IsTemporal(value))
            {
                WriteTemporal(value);
            }
            // must check dictionary before collection because 
            // IDictionary inherits ICollection
            else if (value is IDictionary map)  
            {                                   
                WriteMap(map);
            }
            // same for byte[]
            else if (value is byte[])
            {
                if (allowBytes)
                {
                    WriteBytes((byte[])value);
                }
                else
                {
                    WriteBase64((byte[])value);
                }
            }
            else if (value is ICollection list)
            {
                WriteList(list);
            }

            else
            {
                WriteObject(value);
            }
        }

    }
}
