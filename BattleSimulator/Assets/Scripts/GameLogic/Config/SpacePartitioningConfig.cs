#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace GameLogic.Config
{
    [CreateAssetMenu(fileName = "SpacePartitioningConfig", menuName = "Config/GameLogic/SpacePartitioningConfig")]
    class SpacePartitioningConfig : ScriptableObject
    {
        internal Bounds Bounds => new(Vector3.zero, new Vector3(AreaSize.x, 1, AreaSize.y));

        [SerializeField]
        internal int QuadrantCount = 8;

        [SerializeField]
        internal Vector2 AreaSize = new(100, 100);
    }
}