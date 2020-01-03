using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Sigma
{
    public class Writer
    {

        private static readonly char[] HEX_CHARS = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private Stream output;
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

        private bool IsWs(int c)
        {
            return c == 0x20 || c == 0x09 || c == 0x0a || c == 0x0d;
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

        private void WriteList(ICollection<object> list)
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

        private void WriteMap(IDictionary<string, object> map)
        {
            string separator = "";
            WriteChar('{');
            foreach (KeyValuePair<string, object> kvp in map)
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
                    throw new SigmaException("No obj type registered for class " + obj.GetType().Name);
                }
                string separator = "";
                WriteChars(typeName);
                WriteChar('{');
                //BeanInfo beanInfo = Introspector.getBeanInfo(obj.getClass());
                //PropertyDescriptor[] propertyDescriptors = beanInfo.getPropertyDescriptors();
                //for (PropertyDescriptor prop : propertyDescriptors)
                //{
                //    if (!prop.getName().equals("class"))
                //    {
                //        WriteChars(separator);
                //        WriteName(prop.getName());
                //        WriteChar('=');
                //        Method getter = prop.getReadMethod();
                //        object value = getter.invoke(obj);
                //        WriteValue(value);
                //        separator = ",";
                //    }
                //}
                WriteChar('}');
            }
            catch (IOException ex)
            {
                throw new SigmaException(ex.Message, ex);
            }
        }

        private List<int> codepoints(string s)
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
            foreach (int c in codepoints(s))
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


        private void WriteTemporal(object obj)
        {
            if (obj is DateTimeOffset)
            {
                WriteChars("@z");
            }
            else if (obj is DateTime)
            {
                WriteChars("@D");
            }
            else if (obj is LocalDate)
            {

            }
            else if (obj is LocalDateTime)
            {
                WriteChars("@d");
            }
            else if (obj is LocalTime)
            {
                WriteChars("@t");
            }
            WriteChars(obj.ToString());
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
        //   }
        //    else if (typeof(IDictionary).IsAssignableFrom(typeof(value))
        //    {

        //        WriteMap(Map.class.cast(value));
        //} else if (value is ICollection) {
        //    WriteList(Collection.class.cast(value));
        } else if (value is byte[]) {
            if (allowBytes) {
                WriteBytes((byte[]) value);
            } else {
                WriteBase64((byte[]) value);
            }
        } else {
            WriteObject(value);
        }
    }

    }
}
