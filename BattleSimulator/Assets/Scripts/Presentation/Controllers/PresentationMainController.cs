#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core.Interfaces;
using Core.Models;
using Presentation.Config;
using Presentation.Interfaces;
using Presentation.Jobs;
using Presentation.Views;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;

namespace Presentation.Controllers
{
    /// <summary>
    /// Main controller serves 3 distinct roles:<br/>
    /// 1) It allows you to control signal execution order. For example, instead of reacting on many signals in many different controllers,
    /// you can have one signal, react on it here, and call necessary controllers/systems in the order of your liking.<br/>
    /// 2) Serves as a 'default' controller. When you don't know where to put some logic or the logic is too small for its own controller
    /// you can put it into the main controller.<br/>
    /// 3) Reduces the size of the viewmodel. We could move all (late/fixed)update calls to viewmodel but over time it would lead to viewmodel
    /// being too long to comprehend. We also do not want to react on signals in viewmodels for the exact same reason.<br/>
    /// For better code readability all controllers meant to interact with this controller should implement
    /// <see cref="ICustomLateUpdate" /> interface.<br/>
    /// </summary>
    [UsedImplicitly]
    class PresentationMainController : IInitializable, ICustomUpdate
    {
        static readonly ProjectileConfig _projectileConfig;
        static readonly UnitConfig _unitConfig;

        static bool _loadingFinished;

        readonly ObjectPool<IProjectile> _projectilePool = new(
            () => Object.Instantiate(_projectileConfig.ProjectilePrefab, PresentationSceneReferenceHolder.ProjectileContainer),
            view => view.GameObject.SetActive(true),
            view => view.GameObject.SetActive(false));

        [Preserve]
        PresentationMainController() { }

        public void Initialize() { }

        public void CustomUpdate()
        {
            if (!_loadingFinished)
                return;

            // update units
            var job1 = new UpdateUnitTransformJob
            {
                Positions = CoreData.UnitCurrPos
            };
            JobHandle handle1 = job1.Schedule(PresentationData.UnitTransformAccess);

            // todo: bugged - array not created yet
            // update projectiles
            /*var job2 = new UpdateProjectileTransformJob {Positions = CoreData.ProjectileCurrPos};
            JobHandle handle2 = job2.Schedule(PresentationData.ProjectileTransformAccess);*/

            handle1.Complete();
            //handle2.Complete();
        }

        internal static void InstantiateUnits(List<ArmyModel> armies)
        {
            int totalUnitCount = armies.Sum(army => army.UnitCount);

            PresentationData.Units = new IUnit[totalUnitCount];
            var transforms = new Transform[totalUnitCount];

            int unitId = 0;
            foreach (ArmyModel army in armies)
                for (int unitType = 0; unitType < 2; unitType++)
                    for (int i = 0; i < army.GetUnitCount(unitType); i++)
                    {
                        UnitView prefab = _unitConfig.UnitPrefabs[unitType];
                        UnitView view = Object.Instantiate(prefab, PresentationSceneReferenceHolder.UnitContainer);
                        transforms[i] = view.transform;

                        PresentationData.Units[unitId] = view;
                        unitId++;
                    }

            PresentationData.UnitTransformAccess = new TransformAccessArray(transforms);
        }

        [React]
        static void OnCenterOfArmiesChanged(Vector3 center)
        {
            Camera camera = PresentationSceneReferenceHolder.GameplayCamera;
            Vector3 pos = camera.transform.position;
            Vector3 forwardTarget = (center - pos).normalized;
            camera.transform.forward += (forwardTarget - pos) * 0.1f;
        }
    }
}