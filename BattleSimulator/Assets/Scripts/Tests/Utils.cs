using System;
using System.Reflection;

namespace Tests
{
    static class Utils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="memory">List of memories</param>
        /// <param name="id">ID of the element we want to retrieve</param>
        /// <returns></returns>
        internal static dynamic GetMemoryFromMemoryList(dynamic memory, int id)
        {
            Type type = memory.GetType();
            MethodInfo toArrayMethod = type.GetMethod("ToArray");
            dynamic array = toArrayMethod!.Invoke(memory, null); // this should be a list now
            return (array as Array)!.GetValue(id);
        }

        internal static dynamic MemoryListToMemoryArray(dynamic memory)
        {
            Type type = memory.GetType();
            MethodInfo toArrayMethod = type.GetMethod("ToArray");
            dynamic array = toArrayMethod!.Invoke(memory, null);
            return (Array)array;
        }

        internal static dynamic MemoryToArray(dynamic memory)
        {
            Type type = memory.GetType();
            MethodInfo toArrayMethod = type.GetMethod("ToArray");
            return toArrayMethod!.Invoke(memory, null);
        }

        internal static dynamic GetElementValue(dynamic array, int index, string fieldName)
        {
            dynamic value = array.GetValue(index);
            FieldInfo fInfo = value.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return fInfo.GetValue(value);
        }
    }
}