#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using JetBrains.Annotations;

namespace Core
{
    /// <summary>
    /// Indicates that this field will be populated by <see cref="Core.Services.DependencyInjectionService{TScriptableObject}"/>.
    /// The field should be always private, static, and readonly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectAttribute : Attribute { }
}