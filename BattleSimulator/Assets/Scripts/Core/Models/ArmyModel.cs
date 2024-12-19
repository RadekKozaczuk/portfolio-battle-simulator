﻿using Core.Enums;
using UnityEngine;

namespace Core.Models
{
    /// <summary>
    /// ScriptableObject containing the data of an army
    /// for simplicity's sake the use-case of updating the SO manually has been discarded,
    /// and therefore the usage of ReadOnlyAttribute
    /// </summary>
    [CreateAssetMenu(menuName = "Create ArmyModel", fileName = "ArmyModel", order = 0)]
    public class ArmyData : ScriptableObject
    {
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