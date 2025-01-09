using System;
using System.Reflection;

namespace Tests
{
    public static class Utils
    {
        public static dynamic MemoryToArray(dynamic memory)
        {
            Type type = memory.GetType();
            MethodInfo toArrayMethod = type.GetMethod("ToArray");
            return toArrayMethod!.Invoke(memory, null);
        }

        public static dynamic GetElementValue(dynamic array, int index, string fieldName)
        {
            dynamic value = array.GetValue(index);
            FieldInfo fInfo = value.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return fInfo.GetValue(value);
        }
    }
}