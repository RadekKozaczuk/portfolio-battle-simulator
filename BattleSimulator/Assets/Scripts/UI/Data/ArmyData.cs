#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core.Enums;
using UnityEngine;

namespace UI.Data
{
    /// <summary>
    /// ScriptableObject containing the data of an army
    /// for simplicity's sake the use-case of updating the SO manually has been discarded,
    /// and therefore the usage of ReadOnlyAttribute
    /// </summary>
    [CreateAssetMenu(menuName = "Create ArmyModel", fileName = "ArmyModel", order = 0)]
    public class ArmyData : ScriptableObject
    {
        public int TotalUnitCount => _warriors + _archers;

        public Strategy Strategy
        {
            get => _strategy;
            set => _strategy = value;
        }

        public int Warriors
        {
            get => _warriors;
            set => _warriors = value;
        }

        public int Archers
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