#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using UnityEngine;

namespace Presentation
{
    /// <summary>
    /// Keep in mind references stored here are only accessible AFTER Core scene is fully loaded up.
    /// Use, for example, <see cref="Controllers.PresentationMainController._coreSceneLoaded" /> to control the execution.
    /// </summary>
    class PresentationSceneReferenceHolder : MonoBehaviour
    {
        internal static Camera MainMenuCamera;
        internal static Camera GameplayCamera;
        internal static Transform ProjectileContainer;
        internal static Transform UnitContainer;

        [SerializeField]
        Camera _mainCamera;

        [SerializeField]
        Camera _gameplayCamera;

        [SerializeField]
        Transform _projectileContainer;

        [SerializeField]
        Transform _unitContainer;

        void Awake()
        {
            MainMenuCamera = _mainCamera;
            GameplayCamera = _gameplayCamera;
            ProjectileContainer = _projectileContainer;
            UnitContainer = _unitContainer;
        }
    }
}