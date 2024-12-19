#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace Presentation.Interfaces
{
    public interface IUnit
    {
        public Transform Transform { get; }
        public Renderer Renderer { get; }

        public void Move(Vector3 currentPos, Vector3 lastPos, float speed);

        public void Attack();

        public void Hit();

        public void Die();
    }
}