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
        internal static Transform UnitContainer;
        internal static Transform ProjectileContainer;
        internal static BoxCollider LeftSpawn;
        internal static BoxCollider RightSpawn;

        [SerializeField]
        Camera _mainCamera;

        [SerializeField]
        Camera _gameplayCamera;

        [SerializeField]
        Transform _unitContainer;

        [SerializeField]
        Transform _projectileContainer;

        [SerializeField]
        BoxCollider _leftSpawn;

        [SerializeField]
        BoxCollider _rightSpawn;

        void Awake()
        {
            MainMenuCamera = _mainCamera;
            GameplayCamera = _gameplayCamera;
            UnitContainer = _unitContainer;
            ProjectileContainer = _projectileContainer;
            LeftSpawn = _leftSpawn;
            RightSpawn = _rightSpawn;
        }
    }
}