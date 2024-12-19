#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace Presentation.Interfaces
{
    internal interface IUnit
    {
        internal Transform Transform { get; }
        internal Renderer Renderer { get; }

        internal void Move(Vector3 currentPos, Vector3 lastPos, float speed);

        internal void Attack();

        internal void Hit();

        internal void Die();
    }
}