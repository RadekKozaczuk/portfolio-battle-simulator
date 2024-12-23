#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using UnityEngine;

namespace UI.Data
{
    [CreateAssetMenu(fileName = "ArmyData", menuName = "Data/UI/ArmyData")]
    class ArmyData : ScriptableObject
    {
        internal int TotalUnitCount => _warriors + _archers;

        internal Strategy Strategy
        {
            get => _strategy;
            set => _strategy = value;
        }

        internal int Warriors
        {
            get => _warriors;
            set => _warriors = value;
        }

        internal int Archers
        {
            get => _archers;
            set => _archers = value;
        }

        [SerializeField]
        int _warriors = 100;

        [SerializeField]
        int _archers = 100;

        [SerializeField]
        Strategy _strategy = Strategy.Basic;
    }
}