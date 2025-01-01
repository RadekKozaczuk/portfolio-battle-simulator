#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using Core;
using Core.Interfaces;
using Core.Models;
using JetBrains.Annotations;
using Presentation.Config;
using Presentation.Controllers;
using UnityEditor;
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

        public void Initialize() => EditorApplication.quitting += Dispose;

        public static void CustomUpdate() => _presentationMainController.CustomUpdate();

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

        public static Bounds[] GetSpawnBounds() => new[]
        {
            PresentationSceneReferenceHolder.LeftSpawn.bounds,
            PresentationSceneReferenceHolder.RightSpawn.bounds
        };

        /// <summary>
        /// This only spawns object.
        /// Positions are not yet set at this moment,
        /// </summary>
        public static void InstantiateUnits(List<ArmyModel> armies) => PresentationMainController.InstantiateUnits(armies);

        public static void Dispose()
        {
            PresentationData.UnitTransformAccess.Dispose();
            PresentationData.MovementSpeedArray.Dispose();
        }
    }
}
