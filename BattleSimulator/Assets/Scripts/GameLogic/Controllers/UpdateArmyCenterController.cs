#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Interfaces;
using Core.Models;
using GameLogic.Interfaces;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace GameLogic.Controllers
{
    [UsedImplicitly]
    class UpdateArmyCenterController : ICustomUpdate
    {
        internal float2 CenterOfArmies
        {
            get => _centerOfArmies;
            private set
            {
                // ReSharper disable once InvertIf
                if (math.any(_centerOfArmies - value))
                {
                    _centerOfArmies = value;
                    Signals.CenterOfArmiesChanged(new Vector3(_centerOfArmies.x, 0, _centerOfArmies.y));
                }
            }
        }
        float2 _centerOfArmies;

        float2[] _armyCenters;
        IBattleModel _model;

        [Preserve]
        UpdateArmyCenterController() { }

        public void CustomUpdate()
        {
            float2 sum = float2.zero;
            float2 partialSum = float2.zero;
            for (int armyId = 0; armyId < _armyCenters.Length; armyId++)
            {
                Span<UnitModel> units = _model.GetUnits(armyId);

                for (int i = 0; i < units.Length; i++)
                {
                    ref UnitModel unit = ref units[i];

                    if (unit.Health > 0)
                        partialSum += CoreData.UnitCurrPos[unit.Id];
                }

                _armyCenters[armyId] = partialSum;
                sum += partialSum;
            }

            CenterOfArmies = sum;
        }

        // todo: should be manually called
        internal void Initialize(IBattleModel model)
        {
            Assert.IsNull(_model);

            _model = model;
            _armyCenters = new float2[_model.ArmyCount];
        }

        internal float2 GetArmyCenter(int armyId) => _armyCenters[armyId];
    }
}