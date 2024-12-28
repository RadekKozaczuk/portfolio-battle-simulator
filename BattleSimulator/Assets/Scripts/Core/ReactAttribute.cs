#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Core
{
    /// <summary>
    /// Means that this method will be call when we send a corresponding signal.
    /// Methods marked with this attribute should not be public.
    /// Also applies (inherits) <see cref="PreserveAttribute"/> for convenience as these two always go together.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class ReactAttribute : PreserveAttribute { }
}