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