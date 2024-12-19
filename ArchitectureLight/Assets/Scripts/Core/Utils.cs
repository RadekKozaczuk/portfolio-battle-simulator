#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;

namespace Core
{
    public static class Utils
    {
        public static bool HasDuplicates(int[] array)
        {
            var set = new HashSet<int>();
            // ReSharper disable once ForCanBeConvertedToForeach
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < array.Length; i++)
                if (!set.Add(array[i]))
                    return true;

            return false;
        }
    }
}