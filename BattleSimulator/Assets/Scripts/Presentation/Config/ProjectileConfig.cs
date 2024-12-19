#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.Views;
using UnityEngine;

namespace Presentation.Config
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Config/Presentation/ProjectileConfig")]
    class ProjectileConfig : ScriptableObject
    {
        [SerializeField]
        internal ProjectileView ProjectilePrefab;
    }
}