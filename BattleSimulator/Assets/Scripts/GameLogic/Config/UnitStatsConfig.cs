#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using GameLogic.Data;
using UnityEngine;

namespace GameLogic.Config
{
    [CreateAssetMenu(fileName = "UnitStatsConfig", menuName = "Config/GameLogic/UnitStatsConfig")]
    class UnitStatsConfig : ScriptableObject
    {
        [SerializeField]
        internal UnitData[] UnitData;
    }
}