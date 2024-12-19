#if UNITY_EDITOR || DEVELOPMENT_BUILD
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "Config/Core/DebugConfig")]
    public class DebugConfig : ScriptableObject
    {
        /// <summary>
        /// Logs all state change requests.
        /// </summary>
        [InfoBox("Logs GameState change request and what scenes are requested to be loaded and unloaded.", InfoMessageType.None)]
        public bool LogRequestedStateChange;

        /// <summary>
        /// Logs all sent Signals.
        /// </summary>
        public bool LogSentSignals;
    }
}
#endif