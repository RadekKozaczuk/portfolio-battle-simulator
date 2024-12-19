#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Presentation.Views;
using UnityEngine;

namespace Presentation.Config
{
    [CreateAssetMenu(fileName = "UnitConfig", menuName = "Config/Presentation/UnitConfig")]
    class UnitConfig : ScriptableObject
    {
        // todo: add info that order must match the enum
        [SerializeField]
        internal UnitView[] UnitPrefabs;
    }
}