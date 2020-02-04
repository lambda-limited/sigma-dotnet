using NodaTime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

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

        /**
   * coerce the value to be of type k.
   *
   * This is required when reading numeric values since the exact numeric type
   * cannot be determine by the Reader. The reader therefore reads
   * all numbers as a BigDecimal which can be narrowed for assignments.
   * coerceType will not convert floating point number to integers and will
   * not allow integer conversions that lose precision.
   *
   * coerce will also convert container types (maps and lists) provided the
   * containers implement the same interface.
   *
   * For example, map data can be assigned to a HashMap, TreeMap or
   * ConcurrentHashMap since they all inherit the Map interface.
   *
   * @param value
   * @param k
   * @param onError
   * @return
   */
        public static Object CoerceType(Object value, Type type)
        {
            if (value == null)
            {
                if (type.IsPrimitive)
                {
                    throw new SigmaException(string.Format("unable to coerce null to type '{0}'", type.Name));
                } else
                {
                    return null;
                }
            }
            if (type.IsAssignableFrom(value.GetType()))
            {
                return value;  // assignment compatible already (ie value is a subclass of k)
            }
            if (value is Boolean && typeof(bool).IsAssignableFrom(type)) {
                return value;
            }
            if (value is decimal d)
            {
                if (typeof(decimal) == type)
                {
                    return d;
                }
                if (typeof(int).IsAssignableFrom(type))
                {
                    return Decimal.ToInt32(d);
                }
                if (typeof(long).IsAssignableFrom(type))
                {
                    return Decimal.ToInt64(d);
                }
                if (typeof(double).IsAssignableFrom(type))
                {
                    return Decimal.ToDouble(d);
                }
                if (typeof(float).IsAssignableFrom(type))
                {
                    return (float)(Decimal.ToDouble(d));
                }
                if (typeof(byte).IsAssignableFrom(type))
                {
                    return Decimal.ToByte(d);
                }
                if (typeof(short).IsAssignableFrom(type))
                {
                    return Decimal.ToInt16(d);
                }

            }
            else if (value is IDictionary dict && !type.IsAssignableFrom(value.GetType()))
            {
                if (!typeof(IDictionary).IsAssignableFrom(type))
                {
                    throw new SigmaException(string.Format("unable to coerce map to type '{0}'", type.Name));
                }
                object result = Activator.CreateInstance(type);
                Type keyType = type.GenericTypeArguments[0];
                Type valType = type.GenericTypeArguments[1];
                MethodInfo addMethod = type.GetMethod("Add");
                foreach (DictionaryEntry entry in dict)
                {
                    object coercedKey = CoerceType(entry.Key, keyType);
                    object coercedVal = CoerceType(entry.Value, valType);
                    addMethod.Invoke(result, new object[] { coercedKey, coercedVal });
                }
                return result;
            }
            else if (value is IList list && !type.IsAssignableFrom(value.GetType()))
            {
                if (!typeof(IList).IsAssignableFrom(type))
                {
                    throw new SigmaException(string.Format("unable to coerce list to type '{0}'", type.Name));
                }
                // convert to requested type and copy items from the input list
                // to the result
                object result = Activator.CreateInstance(type);
                Type itemType = type.GetGenericArguments()[0];
                MethodInfo addMethod = type.GetMethod("Add");
                foreach (object item in list)
                {
                    object coercedItem = CoerceType(item, itemType);
                    addMethod.Invoke(result, new object[] { coercedItem });
                }
                return result;
            }
            throw new SigmaException(string.Format("unable to coerce type {0} to type {1}", value.GetType(), type.Name));
        }
    }
}
