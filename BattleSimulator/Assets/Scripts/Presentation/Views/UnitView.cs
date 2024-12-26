#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;

namespace Presentation.Views
{
    class UnitView : MonoBehaviour, IUnit
    {
        string IUnit.Name { get => gameObject.name; set => gameObject.name = value; }
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

        void IUnit.Move(float movementSpeed)
        {
            Assert.IsTrue(movementSpeed is >= 0f and <= 1f,
                          "MovementSpeed animation parameter must be a value from 0 to 1 (both inclusive) to match the animation.");
            _animator.SetFloat(_movementSpeed, 1f);
        }

        void IUnit.Attack() => _animator.SetTrigger(_attack);

        void IUnit.Hit() => _animator.SetTrigger(_hit);

        void IUnit.Die() => _animator.SetTrigger(_death);
    }
}