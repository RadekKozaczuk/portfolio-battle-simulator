#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using UnityEngine;

namespace Presentation
{
    class LevelSceneReferenceHolder : MonoBehaviour
    {
        [SerializeField]
        internal List<Collider2D> Colliders;

        [SerializeField]
        internal Transform LightContainer;

        [SerializeField]
        internal Transform EnemySpawnPointContainer;

        [SerializeField]
        internal Transform EnemyContainer;
    }
}