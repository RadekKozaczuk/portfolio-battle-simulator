#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace Presentation.Interfaces
{
    public interface IProjectile
    {
        public GameObject GameObject { get; }
        public Transform Transform { get; }
        public Renderer Renderer { get; }
    }
}
