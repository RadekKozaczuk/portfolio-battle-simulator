#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace UI.Data
{
    [CreateAssetMenu(fileName = "ArmyData", menuName = "Data/UI/ArmyData")]
    class ArmyData : ScriptableObject
    {
        [SerializeField]
        internal string Name;

        [SerializeField]
        internal Color Color;

        [SerializeField]
        internal UnitData[] Units;
    }
}