namespace Nya.Serializer
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Custom object marshal
    /// </summary>
    public static class CustomMarshal
    {
        /// <summary>
        /// Marshal object as bytes
        /// </summary>
        /// <param name="object">Source object</param>
        /// <returns>Byte array</returns>
        public static byte[]? MarshalAsBytes(object @object, IEnumerable<Attribute>? fieldAttributes = null, IEnumerable<Tuple<object?, IEnumerable<MemberInfo>>>? serializedFields = null)
        {
            Type sourceType = @object.GetType();

            if (CustomMarshal.IsValue(sourceType))
            {
                return CustomMarshal.MarshalValuesAsBytes(@object);
            }
            else if (sourceType.IsEnum)
            {
                Type enumType = Enum.GetUnderlyingType(sourceType);
                object value = Convert.ChangeType(@object, Enum.GetUnderlyingType(sourceType));
                return CustomMarshal.MarshalValuesAsBytes(value);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(sourceType))
            {
                ArraySizeAttribute? arraySize = fieldAttributes?.OfType<ArraySizeAttribute>().FirstOrDefault();

                if (arraySize is ArraySizeStaticAttribute staticSize)
                {
                    List<byte> result = new List<byte>();
                    IEnumerator enumerator = ((IEnumerable)@object).GetEnumerator();

                    if (staticSize.Size < 0)
                    {
                        throw new IndexOutOfRangeException("Size cannot be negative!");
                    }

                    for (int i = 0; i < staticSize.Size; i++)
                    {
                        if (!enumerator.MoveNext())
                        {
                            throw new IndexOutOfRangeException("Array size missmatch!");
                        }

                        byte[]? recurseResult = CustomMarshal.MarshalAsBytes(enumerator.Current);

                        if (recurseResult != null)
                        {
                            result.AddRange(recurseResult);
                        }
                    }

                    return result.ToArray();
                }
                else if (arraySize is ArraySizeDynamicAttribute dynamicSize && serializedFields != null)
                {
                    int size = CustomMarshal.FindRelatedFieldValue<int>(serializedFields, dynamicSize.PropertyName);

                    if (size < 0)
                    {
                        throw new FieldAccessException("Could not access related field for the array size!");
                    }

                    List<byte> result = new List<byte>();
                    IEnumerator enumerator = ((IEnumerable)@object).GetEnumerator();

                    for (int i = 0; i < size; i++)
                    {
                        if (!enumerator.MoveNext())
                        {
                            throw new IndexOutOfRangeException("Array size missmatch!");
                        }

                        byte[]? recurseResult = CustomMarshal.MarshalAsBytes(enumerator.Current);

                        if (recurseResult != null)
                        {
                            result.AddRange(recurseResult);
                        }
                    }

                    return result.ToArray();
                }
            }
            else
            {
                List<MemberInfo> fields =
                    sourceType.GetFields(BindingFlags.Public | BindingFlags.Instance).ToList().Concat<MemberInfo>(sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    .Select(field => new Tuple<FieldOrderAttribute?, MemberInfo>(field.GetCustomAttribute<FieldOrderAttribute>(), field))
                    .OrderBy(field => field.Item1 != null ? field.Item1.Order : int.MaxValue)
                    .Select(field => field.Item2)
                    .ToList();

                List<Tuple<object?, IEnumerable<MemberInfo>>> members = new List<Tuple<object?, IEnumerable<MemberInfo>>>();

                if (serializedFields != null)
                {
                    members.AddRange(serializedFields);
                }

                members.Add(new Tuple<object?, IEnumerable<MemberInfo>>(@object, fields));
                List<byte> result = new List<byte>();

                foreach (MemberInfo member in fields.Where(field => field.GetCustomAttribute<FieldOrderAttribute>() != null))
                {
                    object? value;

                    if (member is PropertyInfo property)
                    {
                        value = property.GetValue(@object);
                    }
                    else if (member is FieldInfo field)
                    {
                        value = field.GetValue(@object);
                    }
                    else
                    {
                        throw new NotSupportedException("Not a field or property type");
                    }

                    if (value != null)
                    {
                        byte[]? recurseResult = CustomMarshal.MarshalAsBytes(value, member.GetCustomAttributes(), members);
                        
                        if (recurseResult != null)
                        {
                            result.AddRange(recurseResult);
                        }
                    }
                }

                return result.ToArray();
            }

            return null;
        }

        /// <summary>
        /// Marshal byte stream as object
        /// </summary>
        /// <param name="data">Data stream</param>
        /// <param name="targetType">Desired type</param>
        /// <param name="fieldAttributes">Desired field attributes</param>
        /// <param name="deserializedFields">Already deserialized fields</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException">Size of requested data did not match</exception>
        /// <exception cref="FieldAccessException">Field does not exist</exception>
        /// <exception cref="NotSupportedException">Type is not supported</exception>
        public static object? MarshalAsObject(
            Stream data,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType,
            IEnumerable<Attribute>? fieldAttributes = null,
            IEnumerable<Tuple<object?, IEnumerable<MemberInfo>>>? deserializedFields = null)
        {
            if (CustomMarshal.IsValue(targetType))
            {
                return CustomMarshal.MarshalBackInternal(data, targetType);
            }
            else if (targetType.IsEnum)
            {
                Type enumType = Enum.GetUnderlyingType(targetType);
                TypeConverter converter = new TypeConverter();
                return CustomMarshal.MarshalBackInternal(data, enumType);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                ArraySizeAttribute? arraySize = fieldAttributes?.OfType<ArraySizeAttribute>().FirstOrDefault();
                Type? objectType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments().Single();

                if (objectType == null)
                {
                    return null;
                }

                List<object> results = new List<object>();
                int size = 0;

                if (arraySize is ArraySizeStaticAttribute staticSize)
                {
                    if (staticSize.Size < 0)
                    {
                        throw new IndexOutOfRangeException("Size cannot be negative!");
                    }

                    size = staticSize.Size;
                }
                else if (arraySize is ArraySizeDynamicAttribute dynamicSize && deserializedFields != null)
                {
                    size = CustomMarshal.FindRelatedFieldValue<int>(deserializedFields, dynamicSize.PropertyName);

                    if (size < 0)
                    {
                        throw new FieldAccessException("Could not access related field for the array size!");
                    }
                }

                for (int i = 0; i < size; i++)
                {
                    object? recurseResult = CustomMarshal.MarshalAsObject(data, objectType, null, deserializedFields);

                    if (recurseResult != null)
                    {
                        results.Add(recurseResult);
                    }
                }

                if (targetType.IsArray)
                {
                    Array result = Array.CreateInstance(objectType, results.Count);

                    for (int i = 0; i < results.Count; i++)
                    {
                        result.SetValue(results[i], i);
                    }

                    return result;
                }
                else if (targetType != null)
                {
                    object? target = Activator.CreateInstance(targetType);
                    MethodInfo? add = targetType?.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

                    if (add != null)
                    {
                        foreach (object item in results)
                        {
                            add.Invoke(target, new object[] { item });
                        }
                    }

                    return target;
                }
            }
            else
            {
                List<MemberInfo> fields =
                    targetType.GetFields(BindingFlags.Public | BindingFlags.Instance).ToList().Concat<MemberInfo>(targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    .Select(field => new Tuple<FieldOrderAttribute?, MemberInfo>(field.GetCustomAttribute<FieldOrderAttribute>(), field))
                    .OrderBy(field => field.Item1 != null ? field.Item1.Order : int.MaxValue)
                    .Select(field => field.Item2)
                    .ToList();

                List<Tuple<object?, IEnumerable<MemberInfo>>> members = new List<Tuple<object?, IEnumerable<MemberInfo>>>();

                if (deserializedFields != null)
                {
                    members.AddRange(deserializedFields);
                }

                object? result = Activator.CreateInstance(targetType);
                members.Add(new Tuple<object?, IEnumerable<MemberInfo>>(result, fields));

                foreach (MemberInfo member in fields.Where(field => field.GetCustomAttribute<FieldOrderAttribute>() != null))
                {
                    if (member is FieldInfo field)
                    {
                        field.SetValue(result, CustomMarshal.MarshalAsObject(data, field.FieldType, field.GetCustomAttributes(), members));
                    }
                    else if (member is PropertyInfo property)
                    {
                        property.SetValue(result, CustomMarshal.MarshalAsObject(data, property.PropertyType, property.GetCustomAttributes(), members));
                    }
                }

                return result;
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// Find value for the related field
        /// </summary>
        /// <typeparam name="FieldValueType">Field type</typeparam>
        /// <param name="serializedFields">Serialized fields</param>
        /// <param name="relatedFieldName">Related field name</param>
        /// <returns>Value of related field</returns>
        /// <exception cref="NotSupportedException">Thrown when field is not a field or property</exception>
        /// <exception cref="FieldAccessException">Thrown when field is not found</exception>
        private static FieldValueType FindRelatedFieldValue<FieldValueType>(IEnumerable<Tuple<object?, IEnumerable<MemberInfo>>> serializedFields, string relatedFieldName)
        {
            Tuple<object?, IEnumerable<MemberInfo>>? fields = serializedFields?.LastOrDefault();

            if (fields != null)
            {
                MemberInfo? member = fields.Item2.FirstOrDefault(field => field.Name == relatedFieldName);

                if (member != null)
                {
                    object? value;

                    if (member is PropertyInfo property)
                    {
                        value = property.GetValue(fields.Item1);
                    }
                    else if (member is FieldInfo field)
                    {
                        value = field.GetValue(fields.Item1);
                    }
                    else
                    {
                        throw new NotSupportedException("Not a field or property type");
                    }

                    if (typeof(FieldValueType).IsAssignableFrom(value?.GetType()))
                    {
                        return (FieldValueType)value;
                    }
                }
                else
                {
                    int size = serializedFields?.Count() - 1 ?? 0;

                    if (size > 0 && serializedFields != null)
                    {
                        return CustomMarshal.FindRelatedFieldValue<FieldValueType>(serializedFields.Take(size), relatedFieldName);
                    }
                }
            }

            throw new FieldAccessException("Related field for array size not found");
        }

        /// <summary>
        /// Is simple value
        /// </summary>
        /// <param name="type">Type to check</param>
        /// <returns>True if is simple value</returns>
        private static bool IsValue(Type type)
        {
            return new[] {
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(UInt16),
                typeof(UInt32),
                typeof(UInt64),
                typeof(bool),
                typeof(char),
                typeof(SByte)
            }.Contains(type);
        }

        /// <summary>
        /// Convert bytes to object
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <returns>Converted object</returns>
        private static object? MarshalBackInternal(Stream bytes, Type targetType)
        {
            int length = Marshal.SizeOf(targetType);
            byte[] buffer = new byte[length];
            bytes.Read(buffer, 0, length);
            buffer = buffer.Reverse<byte>().ToArray();

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            object? theStructure = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), targetType);
            handle.Free();
            return theStructure;
        }

        /// <summary>
        /// Converts object to bytes in reverse order
        /// </summary>
        /// <param name="object">Simple object</param>
        /// <returns>Bytes in reversed order</returns>
        private static byte[] MarshalInternal(object @object)
        {
            int length = Marshal.SizeOf(@object);
            IntPtr ptr = Marshal.AllocHGlobal(length);
            byte[] myBuffer = new byte[length];

            Marshal.StructureToPtr(@object, ptr, true);
            Marshal.Copy(ptr, myBuffer, 0, length);
            Marshal.FreeHGlobal(ptr);
            return myBuffer.Reverse<byte>().ToArray();
        }

        /// <summary>
        /// Marshal value object as bytes
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        private static byte[] MarshalValuesAsBytes(object @object)
        {
            if (@object is byte)
            {
                return new[] { (byte)@object };
            }
            else
            {
                return CustomMarshal.MarshalInternal(@object);
            }
        }
    }
}