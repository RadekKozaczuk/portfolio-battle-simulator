// ReSharper disable UnusedMemberInSuper.Global

using UnityEngine;

namespace Core
{
    public interface ISignal
    {
        void BattleModelCreated();

        void CenterOfArmiesChanged(Vector3 center);
    }
}