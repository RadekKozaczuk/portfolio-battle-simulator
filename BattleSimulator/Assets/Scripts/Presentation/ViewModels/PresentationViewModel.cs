#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using Core;
using Core.Interfaces;
using JetBrains.Annotations;
using Presentation.Config;
using Presentation.Controllers;
using UnityEngine;
using UnityEngine.Scripting;

namespace Presentation.ViewModels
{
    [UsedImplicitly]
    public class PresentationViewModel : IInitializable
    {
        static readonly UnitConfig _playerConfig;

        [Inject]
        static readonly PresentationMainController _presentationMainController;

        [Preserve]
        PresentationViewModel() { }

        public void Initialize() { }

        public static void CustomUpdate() => _presentationMainController.CustomUpdate();

        public static void CustomFixedUpdate() => _presentationMainController.CustomFixedUpdate();

        public static void CustomLateUpdate() => _presentationMainController.CustomLateUpdate();

        public static void BootingOnExit() { }

        public static void MainMenuOnEntry()
        {
            PresentationSceneReferenceHolder.GameplayCamera.gameObject.SetActive(false);
            PresentationSceneReferenceHolder.MainMenuCamera.gameObject.SetActive(true);
        }

        public static void MainMenuOnExit() { }

        public static void GameplayOnEntry()
        {
            PresentationSceneReferenceHolder.GameplayCamera.gameObject.SetActive(true);
            PresentationSceneReferenceHolder.MainMenuCamera.gameObject.SetActive(false);
        }

        public static void GameplayOnExit() { }

        public static void GetSpawnBounds(out Bounds left, out Bounds right)
        {
            left = PresentationSceneReferenceHolder.LeftSpawn.bounds;
            right = PresentationSceneReferenceHolder.RightSpawn.bounds;
        }
    }
}
