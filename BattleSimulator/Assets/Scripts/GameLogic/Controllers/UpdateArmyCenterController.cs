#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core;
using Core.Interfaces;
using Core.Models;
using GameLogic.Interfaces;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

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

        readonly float2[] _armyCenters;
        IBattleModel _model;

        // todo: should be manually called
        void Initialize(IBattleModel model)
        {
            _model = model;
        }

        public void CustomUpdate()
        {
            float2 sum = float2.zero;
            float2 partialSum = float2.zero;
            for (int armyId = 0; armyId < _armyCenters.Length; armyId++)
            {
                Span<UnitModel> units = _model.GetUnits(armyId);

                for (int i = 0; i < units.Length; i++)
                    partialSum += CoreData.UnitCurrPos[units[i].Id];

                _armyCenters[armyId] = partialSum;
                sum += partialSum;
            }

            CenterOfArmies = sum;
        }

        internal float2 GetArmyCenter(int armyId) => _armyCenters[armyId];
    }
}