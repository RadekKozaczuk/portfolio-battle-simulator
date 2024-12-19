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
