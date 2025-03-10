#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Enums;
using JetBrains.Annotations;
using UnityEngine.Scripting;
using Core.Interfaces;
using Core.Models;
using Presentation.Config;
using Presentation.Interfaces;
using Presentation.Jobs;
using Presentation.Views;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Jobs;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

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
    /// </summary>
    [UsedImplicitly]
    class PresentationMainController : IInitializable, ICustomUpdate
    {
        static readonly ProjectileConfig _projectileConfig;
        static readonly UnitConfig _unitConfig;

        static bool _loadingFinished;

        static readonly ObjectPool<IProjectile> _projectilePool = new(
            () => Object.Instantiate(_projectileConfig.ProjectilePrefab, PresentationSceneReferenceHolder.ProjectileContainer),
            view => view.GameObject.SetActive(true),
            view =>
            {
                view.Id = int.MinValue;
                view.GameObject.SetActive(false);
            });

        static readonly List<IProjectile> _projectiles = new();

        /// <summary>
        /// Null when dead.
        /// </summary>
        static IUnit?[] _units;

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
                Positions = CoreData.UnitCurrPos,
                DifferenceArray = PresentationData.MovementSpeedArray,
                Speed = 10f // todo: take from Shared
            };
            JobHandle handle1 = job1.Schedule(PresentationData.UnitTransformAccess);

            handle1.Complete();

            // movement animation
            for (int i = 0; i < _units.Length; i++)
            {
                IUnit? view = _units[i];
                view?.Move(PresentationData.MovementSpeedArray[i]);
            }
        }

        internal static void InstantiateUnits(List<ArmyModel> armies)
        {
            int totalUnitCount = armies.Sum(army => army.UnitCount);

            _units = new IUnit[totalUnitCount];
            var transforms = new Transform[totalUnitCount];

            int unitId = 0;
            foreach (ArmyModel army in armies)
                for (int unitType = 0; unitType < 2; unitType++) // todo: iterate over UnitType enum
                {
                    UnitView prefab = _unitConfig.UnitPrefabs[unitType];
                    for (int i = 0; i < army.GetUnitCount((UnitType)unitType); i++)
                    {
                        IUnit view = Object.Instantiate(prefab, PresentationSceneReferenceHolder.UnitContainer);
                        view.Renderer.material.color = army.Color;

                        // todo: match unit type name dynamically
                        view.Name = $"{(unitType == 0 ? "Warrior" : "Archer")}_{unitId}";

                        transforms[unitId] = view.Transform;
                        _units[unitId] = view;
                        unitId++;
                    }
                }

            PresentationData.UnitTransformAccess = new TransformAccessArray(transforms);
            PresentationData.MovementSpeedArray = new NativeArray<float>(totalUnitCount, Allocator.Persistent);
            _loadingFinished = true;
        }

        [React]
        static void OnCenterOfArmiesChanged(Vector3 center)
        {
            Camera camera = PresentationSceneReferenceHolder.GameplayCamera;
            Vector3 pos = camera.transform.position;
            Vector3 forwardTarget = (center - pos).normalized;
            camera.transform.forward += forwardTarget * 0.1f;
        }

        [React]
        static void OnProjectileCreated(int id, int armyId, Vector3 position, Vector3 direction)
        {
            int index = _projectiles.FindIndex(p => p.Id == id);
            Assert.IsTrue(index == -1, "Id cannot overlap");

            IProjectile projectile = _projectilePool.Get();
            projectile.Id = id;
            projectile.Transform.position = position;
            projectile.Transform.forward = direction;
            projectile.Renderer.material.color = new Color(1, 0, 0); // todo: should be the same as the army  
            _projectiles.Add(projectile);
        }

        [React]
        static void OnProjectileDestroyed(int id)
        {
            int index = _projectiles.FindIndex(p => p.Id == id);

            IProjectile view = _projectiles[index];
            _projectilePool.Release(view);
            _projectiles.RemoveAt(index);
        }

        [React]
        static void OnProjectilePositionChanged(int id, Vector3 position)
        {
            int index = _projectiles.FindIndex(p => p.Id == id);
            IProjectile view = _projectiles[index];
            view.Transform.position = position;
        }

        [React]
        static void OnUnitAttacked(int unitId)
        {
            _units[unitId]!.Attack();
        }

        [React]
        static void OnUnitDied(int unitId)
        {
            _units[unitId]!.Die();
            _units[unitId] = null;
        }

        [React]
        static void OnUnitHit(int unitId, Vector3 attackDir)
        {
            // todo: race condition
            if (_units[unitId] != null)
                _units[unitId]!.Hit(attackDir);
        }
    }
}