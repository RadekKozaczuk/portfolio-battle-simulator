#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using UI.Data;
using UnityEngine;

namespace UI.Config
{
    [CreateAssetMenu(fileName = "ArmyConfig", menuName = "Config/UI/ArmyConfig")]
    class ArmyConfig : ScriptableObject
    {
        [SerializeField]
        internal List<ArmyData> Armies;
    }
}