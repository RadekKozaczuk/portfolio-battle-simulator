#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Reflection;

namespace Core
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns true if this type is declared with static keyword.
        /// </summary>
        // ReSharper disable once MergeIntoPattern
        public static bool IsStatic(this Type type) => type.IsAbstract && type.IsSealed;

        // IsLiteral determines if its value is written at compile time and not changeable.
        // IsInitOnly determines if the field can be set in the body of the constructor.
        // In C# a field which is readonly keyword would have both true.
        // But a const field would have only IsLiteral equal to true.
        /// <summary>
        /// Returns true if this field is declared with const keyword.
        /// </summary>
        // ReSharper disable once MergeIntoPattern
        public static bool IsConst(this FieldInfo field) => field.IsLiteral && !field.IsInitOnly;
    }
}