using NodaTime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Sigma
{
    public class Types
    {
        private static readonly ConcurrentDictionary<Type, string> CLASS_TO_TYPENAME = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<string, Type> TYPENAME_TO_CLASS = new ConcurrentDictionary<string, Type>();

        public static void Register(Type klass, string typeName)
        {
            CLASS_TO_TYPENAME[klass] = typeName;
            TYPENAME_TO_CLASS[typeName] = klass;
        }

        public static Type GetClass(string typeName)
        {
            return TYPENAME_TO_CLASS[typeName];
        }

        public static string GetTypeName(Type klass)
        {
            return CLASS_TO_TYPENAME[klass];
        }

        public static void UnregisterAll()
        {
            CLASS_TO_TYPENAME.Clear();
            TYPENAME_TO_CLASS.Clear();
        }


        public static bool IsNumber(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsUnsignedNumber(object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTemporal(object o)
        {
            return
                o is DateTime ||
                o is DateTimeOffset ||
                o is LocalDate ||
                o is LocalTime ||
                o is LocalDateTime ||
                o is OffsetTime ||
                o is OffsetDateTime ||
                o is ZonedDateTime;
        }
    }
}
