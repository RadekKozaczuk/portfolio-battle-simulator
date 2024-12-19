using Presentation.Interfaces;
using UnityEngine;

namespace Presentation.Views
{
    public class UnitView : MonoBehaviour, IUnit
    {
        public Transform Transform => transform;
        public Renderer Renderer => _renderer;

        [SerializeField]
        Renderer _renderer;

        [SerializeField]
        Animator _animator;

        static readonly int _hit = Animator.StringToHash("Hit");
        static readonly int _death = Animator.StringToHash("Death");
        static readonly int _movementSpeed = Animator.StringToHash("MovementSpeed");
        static readonly int _attack = Animator.StringToHash("Attack");

        public void Move(Vector3 currentPos, Vector3 lastPos, float speed) =>
            _animator.SetFloat(_movementSpeed, (currentPos - lastPos).magnitude / speed);

        public void Attack() => _animator.SetTrigger(_attack);

        public void Hit() => _animator.SetTrigger(_hit);

        public void Die() => _animator.SetTrigger(_death);

        public void OnDeathAnimFinished() => Destroy(gameObject);
    }
}