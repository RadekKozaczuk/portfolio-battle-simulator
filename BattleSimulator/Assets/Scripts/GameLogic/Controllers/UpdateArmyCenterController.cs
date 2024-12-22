using Core;
using Core.Interfaces;
using GameLogic.Models;
using Unity.Mathematics;
using UnityEngine;

namespace GameLogic.Controllers
{
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

        void Initialize(BattleModel model)
        {
            /*_centerOfArmies = new float2(float.MinValue, float.MinValue);
            _armyCenters = new float2[_armyCount];

            for (int i = 0; i < _armyCount; i++)
                _armyCenters[i] = new float2(float.MinValue, float.MinValue);*/
        }

        public void CustomUpdate()
        {

        }

        internal float2 GetArmyCenter(int armyId) => _armyCenters[armyId];
    }
}