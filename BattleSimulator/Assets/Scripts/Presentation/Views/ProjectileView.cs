#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.Interfaces;
using UnityEngine;

namespace Presentation.Views
{
    class ProjectileView : MonoBehaviour, IProjectile
    {
        GameObject IProjectile.GameObject => gameObject;
        Transform IProjectile.Transform => transform;
        Renderer IProjectile.Renderer => _renderer;

        [SerializeField]
        Renderer _renderer;
    }
}