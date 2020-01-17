using NodaTime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Sigma
{
    public class Reader
    {
        /*
        The length of the utf8 octet sequence 
        based on the first octet in the sequence.  A length of zero
        indicates an illegal encoding.
        */
        static readonly byte[] UTF8_LENGTH = {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            4, 4, 4, 4, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        /*
            Adjustment values used to 'remove' the tag bits in each utf8
            sequence.  These are constant for any sequence of a given length
            and can be removed with a single subtraction.
         */
        static readonly int[] UTF8_TAG = {
            0x00000000,
            0x00000000,
            0x00003080,
            0x000E2080,
            0x03C82080
        };

        private int c;
        private Stream input;
        private int pos = 0;

        public Reader(Stream input)
        {
            this.input = input;
        }

        public Reader(String s)
        {
            this.input = new MemoryStream(Encoding.UTF8.GetBytes(s));
        }

        public Object Read()
        {
            ReadChar();
            SkipWs();
            if (c == -1)
            {
                return null;
            }
            Object value = ReadValue();
            SkipWs();
            if (c != -1)
            {
                throw Error("end of input expected");
            }
            return value;
        }

        /* 
        separator characters that can occur after values in lists and maps
        and after identifiers in objects
         */
        private bool IsSeparator(int ch)
        {
            return ch == '=' || ch == ',' || ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == -1;
        }

        private bool IsWs(int ch)
        {
            return ch == 0x20 || ch == 0x09 || ch == 0x0a || ch == 0x0d;
        }

        private bool Consume(int ch)
        {
            if (ch == c)
            {
                ReadChar();
                return true;
            }
            return false;
        }

        private int ConsumeDigit()
        {
            int ch = c;
            if (IsDigit())
            {
                ReadChar();
                return ch - '0';
            }
            throw Error("digit expected but '%c' found", ch);
        }

        private bool IsDigit()
        {
            return c >= '0' && c <= '9';
        }

        private void ConsumeOrError(int ch)
        {
            if (ch == c)
            {
                ReadChar();
            }
            else
            {
                throw Error("'{0}' expected but '{1}' found", (char)ch, (char)c);
            }
        }

        private SigmaException Error(String msg, params Object[] p)
        {
            String m = string.Format("Reader Error [{0}]: {1}", pos, msg);
            return new SigmaException(string.Format(m, p));
        }

        private SigmaException Error(Exception cause, String msg, params Object[] p)
        {
            String m = string.Format("Reader Error [{0}]: {1}", pos, msg);
            return new SigmaException(string.Format(m, p), cause);
        }

        private byte[] ReadBase64()
        {
            ReadChar();  // skip *
            String bytes = ReadToken();
            int padding = (4 - bytes.Length % 4) % 4;
            switch (padding)
            {
                case 0:
                    return Convert.FromBase64String(bytes);
                case 1:
                    return Convert.FromBase64String(bytes + "=");
                case 2:
                    return Convert.FromBase64String(bytes + "==");
            }
            return null;
        }

        /*
        Read next character from reader keeping track of the position within the
        file for the purposes of error reporting.
         */
        private void ReadByte()
        {
            c = input.ReadByte();
            ++pos;
        }

        private byte[] ReadBytes()
        {
            ReadChar();
            int len = 0;
            while (c != '|')
            {
                len = (len * 10) + (c - '0');
                ReadChar();
            }
            byte[] b = new byte[len];
            int totalRead = 0;
            while (true)
            {
                int bytesRead = input.Read(b, totalRead, len - totalRead);
                if (bytesRead == -1)
                {
                    throw Error("unexpected end of input in binary data");
                }
                if ((totalRead += bytesRead) == len)
                {
                    break;
                }
            }
            ReadChar();
            return b;
        }

        /*
        Read and decode UTF-8 encoded character from input stream
         */
        private void ReadChar()
        {
            ReadByte();
            if (c == -1)
            {
                return;
            }
            int utf8 = c;
            int len = UTF8_LENGTH[c];
            for (int i = 1; i < len; ++i)
            {
                ReadByte();
                utf8 = (utf8 << 6) + c;
            }
            utf8 -= UTF8_TAG[len];
            c = utf8;
        }

        private object ReadDate()
        {
            int state = 0;
            bool hasDate = false;
            bool hasTime = false;
            bool hasOffset = false;
            bool hasZone = false;
            int year = 0;
            int month = 1;
            int day = 1;
            int hour = 0;
            int minute = 0;
            int second = 0;
            int fraction = 0;
            bool offsetUtc = false;
            bool offsetNeg = false;
            int offsetHour = 0;
            int offsetMin = 0;
            String zone = null;

            ReadChar(); // skip @
            while (true)
            {
                switch (state)
                {
                    case 0:  // yyyy or hh
                        year = ConsumeDigit();
                        year = year * 10 + ConsumeDigit();
                        if (Consume(':'))
                        {
                            hour = year;
                            hasTime = true;
                            state = 1;
                            continue;
                        }
                        hasDate = true;
                        year = year * 10 + ConsumeDigit();
                        year = year * 10 + ConsumeDigit();
                        ConsumeOrError('-');
                        month = ConsumeDigit();
                        month = month * 10 + ConsumeDigit();
                        ConsumeOrError('-');
                        day = ConsumeDigit();
                        day = day * 10 + ConsumeDigit();
                        state = 1;
                        if (Consume('T'))
                        {
                            hasTime = true;
                            hour = ConsumeDigit();
                            hour = hour * 10 + ConsumeDigit();
                            ConsumeOrError(':');
                            state = 1;
                            continue;
                        }
                        state = 9;
                        break;
                    case 1: //  mm:ss.sssssss (hh already read in state 0)
                        minute = ConsumeDigit();
                        minute = minute * 10 + ConsumeDigit();
                        ConsumeOrError(':');
                        second = ConsumeDigit();
                        second = second * 10 + ConsumeDigit();
                        // fractional second
                        if (Consume('.'))
                        {
                            int multiplier = 100000000;
                            fraction = ConsumeDigit() * multiplier;
                            while (IsDigit())
                            {
                                multiplier /= 10;
                                if (multiplier == 0)
                                {
                                    throw Error("invalid fraction of a second");
                                }
                                fraction += ConsumeDigit() * multiplier;
                            }
                        }
                        // offset
                        if (c == '-' || c == '+' || c == 'Z')
                        {
                            state = 2;
                            continue;
                        }
                        state = 9;
                        break;
                    case 2: // offset and zone
                        hasOffset = true;
                        if (!Consume('Z'))
                        {
                            offsetNeg = (c == '-');
                            ReadChar();
                            offsetHour = ConsumeDigit();
                            offsetHour = offsetHour * 10 + ConsumeDigit();
                            ConsumeOrError(':');
                            offsetMin = ConsumeDigit();
                            offsetMin = offsetMin * 10 + ConsumeDigit();
                        }
                        else
                        {
                            offsetUtc = true;
                        }
                        if (c == '[')
                        {
                            hasZone = true;
                            ReadChar();
                            zone = ReadToken();
                            ConsumeOrError(']');
                        }
                        state = 9;
                        break;
                    case 9: // construct date from parsed parts
                        bool valid = (month >= 1 && month <= 12)
                                && (day >= 1 && day <= DateTime.DaysInMonth(year, month))
                                && (hour >= 0 && hour <= 23)
                                && (minute >= 0 && minute <= 59)
                                && (second >= 0 && second <= 59)
                                && ((offsetHour * 100 - offsetMin) >= -1200)
                                && ((offsetHour * 100 + offsetMin) <= 1400)
                                && (offsetMin >= 0 && offsetMin <= 59);
                        if (!valid)
                        {
                            throw Error("invalid time or date");
                        }
                        if (offsetNeg)
                        {
                            offsetHour = -offsetHour;
                            offsetMin = -offsetMin;
                        }
                        if (hasZone)
                        { // ZonedDateTime
                            DateTimeZone zoneId = DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone);
                            if (zoneId == null)
                            {
                                throw Error("invalid time zone");
                            }
                            Offset offset = offsetUtc ? Offset.Zero : Offset.FromHoursAndMinutes(offsetHour, offsetMin);
                            return new ZonedDateTime(new LocalDateTime(year, month, day, hour, minute, second).PlusNanoseconds(fraction), zoneId, offset);
                        }
                        if (hasOffset && hasDate && hasTime)
                        {
                            Offset offset = offsetUtc ? Offset.Zero : Offset.FromHoursAndMinutes(offsetHour, offsetMin);
                            return new OffsetDateTime(new LocalDateTime(year, month, day, hour, minute, second).PlusNanoseconds(fraction), offset);
                        }
                        if (hasOffset && hasTime)
                        {
                            Offset offset = offsetUtc ? Offset.Zero : Offset.FromHoursAndMinutes(offsetHour, offsetMin);
                            return new OffsetTime(new LocalTime(hour, minute, second).PlusNanoseconds(fraction), offset);
                        }
                        if (hasDate && hasTime)
                        {
                            return new LocalDateTime(year, month, day, hour, minute, second).PlusNanoseconds(fraction);
                        }
                        if (hasDate)
                        {
                            return new LocalDate(year, month, day);
                        }
                        return new LocalTime(hour, minute, second).PlusNanoseconds(fraction);
                }
            }
        }

        private int ReadHexDigit()
        {
            int ch = c;
            if (ch >= '0' && ch <= '9')
            {
                ReadChar();
                return ch - '0';
            }
            if (ch >= 'A' && ch <= 'F')
            {
                ReadChar();
                return ch + 10 - 'A';
            }
            if (ch >= 'a' && ch <= 'f')
            {
                ReadChar();
                return ch + 10 - 'a';
            }
            throw Error("invalid hex digit '{0}'", (char)ch);
        }

        private IList ReadList()
        {
            ReadChar(); // skip '['
            IList list = new List<object>();
            SkipWs();
            if (!Consume(']'))
            {
                ReadListElements(list);
                ConsumeOrError(']');
            }
            return list;
        }

        private void ReadListElement(IList list)
        {
            SkipWs();
            Object value = ReadValue();
            SkipWs();
            list.Add(value);
        }

        private void ReadListElements(IList list)
        {
            ReadListElement(list);
            while (Consume(','))
            {
                ReadListElement(list);
            }
        }

        private IDictionary ReadMap()
        {
            ReadChar(); // skip '{'
            IDictionary map = new Dictionary<object, object>();
            SkipWs();
            if (!Consume('}'))
            {
                ReadMapElements(map);
                ConsumeOrError('}');
            }
            return map;
        }

        private void ReadMapElement(IDictionary map)
        {
            SkipWs();
            Object key = ReadValue();
            SkipWs();
            ConsumeOrError('=');
            SkipWs();
            Object value = ReadValue();
            SkipWs();
            map.Add(key, value);
        }

        private void ReadMapElements(IDictionary map)
        {
            ReadMapElement(map);
            while (Consume(','))
            {
                ReadMapElement(map);
            }
        }

        /*
            Unfortunately .NET does not have an abitrary precision BigDecimal data type
            So we read the data into the most precise format we can.  This is a decimal.
            This give use scale to +/-38 which should be sufficient for most purposes.
         */
        private object ReadNumber()
        {
            String num = ReadToken();
            if (Decimal.TryParse(num, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }
            throw Error("invalid number");
        }

        private object ReadObject()
        {
            String identifier = ReadToken();
            SkipWs();
            ConsumeOrError('{');
            Type type = Types.GetType(identifier);
            if (type == null)
            {
                throw Error("type '{0}' is not registered", identifier);
            }
            try
            {
                object obj = Activator.CreateInstance(type);
                ReadObjectFields(obj);
                ConsumeOrError('}');
                return obj;
            }
            catch (Exception ex)
            {
                throw Error(ex, "unable to create instance of type '{0}'", identifier);
            }
        }

        private void ReadObjectField(object obj)
        {
            SkipWs();
            string identifier = ReadToken();
            SkipWs();
            ConsumeOrError('=');
            SkipWs();
            object value = ReadValue();
            SkipWs();

            PropertyInfo p = obj.GetType().GetProperty(identifier);
            if (p != null)
            {
                try
                {
                    // so coerce here --> Type pt = p.PropertyType;
                    p.SetValue(obj, Types.CoerceType(value, p.PropertyType));
                    return;
                }
                catch (Exception ex)
                {
                    throw Error("unable to set property {0}.{1}", obj.GetType().Name, identifier); ;
                }
            }
            throw Error("unable to find property {0}.{1}", obj.GetType().Name, identifier);
        }

        private void ReadObjectFields(object obj)
        {
            try
            {
                ReadObjectField(obj);
                while (Consume(','))
                {
                    ReadObjectField(obj);
                }
            }
            catch (Exception ex)
            {
                throw Error(ex.Message);
            }
        }


        private String ReadString()
        {
            ReadChar(); // skip '"'
            StringBuilder sb = new StringBuilder();
            while (!Consume('"'))
            {
                if (Consume('\\'))
                {
                    switch (c)
                    {
                        case '\\':
                        case '"':
                            break;
                        case 'n':
                            c = '\n';
                            break;
                        case 'r':
                            c = '\r';
                            break;
                        case 't':
                            c = '\t';
                            break;
                        case 'u':
                            ReadChar();
                            int ch = (ReadHexDigit() << 12)
                                    + (ReadHexDigit() << 8)
                                    + (ReadHexDigit() << 4)
                                    + (ReadHexDigit());
                            sb.Append(char.ConvertFromUtf32(ch));
                            continue; // while()
                    }
                }
                sb.Append(char.ConvertFromUtf32(c));
                ReadChar();
            }

            return sb.ToString();
        }

        private String ReadToken()
        {
            StringBuilder sb = new StringBuilder();
            while (!IsSeparator(c) && !IsWs(c))
            {
                Char.ConvertFromUtf32(c);
                sb.Append(Char.ConvertFromUtf32(c));
                ReadChar();
            }
            return sb.ToString();
        }

        private void SkipWs()
        {
            while (IsWs(c))
            {
                ReadChar();
            }
        }

        private Object ReadValue()
        {
            switch (c)
            {
                case '"':
                    return ReadString();
                case '[':
                    return ReadList();
                case '{':
                    return ReadMap();
                case '+':
                case '-':
                    return ReadNumber();
                case '@':
                    return ReadDate();
                case '|':
                    return ReadBytes();
                case '*':
                    return ReadBase64();
                case '&':
                    ReadChar();
                    if (Consume('n'))
                    {
                        return null;
                    }
                    if (Consume('t'))
                    {
                        return true;
                    }
                    if (Consume('f'))
                    {
                        return false;
                    }
                    throw Error("invalid constant value");
                default:
                    if (c >= '0' && c <= '9')
                    {
                        return ReadNumber();
                    }
                    else
                    {
                        return ReadObject();
                    }
            }
        }
    }
}
