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