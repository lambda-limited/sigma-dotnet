using NodaTime;
using System;
using System.Collections.Concurrent;

namespace Sigma
{
    public class Types
    {
        private static readonly ConcurrentDictionary<Type, string> TYPE_TO_TYPENAME = new ConcurrentDictionary<Type, string>();
        private static readonly ConcurrentDictionary<string, Type> TYPENAME_TO_TYPE = new ConcurrentDictionary<string, Type>();

        public static void Register(Type type, string typeName)
        {
            TYPE_TO_TYPENAME[type] = typeName;
            TYPENAME_TO_TYPE[typeName] = type;
        }

        public static Type GetType(string typeName)
        {
            return TYPENAME_TO_TYPE.ContainsKey(typeName)
                ? TYPENAME_TO_TYPE[typeName]
                : null;
        }

        public static string GetTypeName(Type klass)
        {
            return TYPE_TO_TYPENAME.ContainsKey(klass)
                ? TYPE_TO_TYPENAME[klass]
                : null;
        }

        public static void UnregisterAll()
        {
            TYPE_TO_TYPENAME.Clear();
            TYPENAME_TO_TYPE.Clear();
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
