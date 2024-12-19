#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace Presentation.Interfaces
{
    internal interface IProjectile
    {
        internal GameObject GameObject { get; }
        internal Transform Transform { get; }
        internal Renderer Renderer { get; }
    }
}
