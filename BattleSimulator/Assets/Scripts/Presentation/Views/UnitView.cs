using Presentation.Interfaces;
using UnityEngine;

namespace Presentation.Views
{
    class UnitView : MonoBehaviour, IUnit
    {
        Transform IUnit.Transform => transform;
        Renderer IUnit.Renderer => _renderer;

        [SerializeField]
        Renderer _renderer;

        [SerializeField]
        Animator _animator;

        static readonly int _hit = Animator.StringToHash("Hit");
        static readonly int _death = Animator.StringToHash("Death");
        static readonly int _movementSpeed = Animator.StringToHash("MovementSpeed");
        static readonly int _attack = Animator.StringToHash("Attack");

        public void OnDeathAnimFinished() => Destroy(gameObject);

        void IUnit.Move(Vector3 currentPos, Vector3 lastPos, float speed) =>
            _animator.SetFloat(_movementSpeed, (currentPos - lastPos).magnitude / speed);

        void IUnit.Attack() => _animator.SetTrigger(_attack);

        void IUnit.Hit() => _animator.SetTrigger(_hit);

        void IUnit.Die() => _animator.SetTrigger(_death);
    }
}