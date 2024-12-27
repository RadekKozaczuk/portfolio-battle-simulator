// ReSharper disable UnusedMemberInSuper.Global

using UnityEngine;

namespace Core
{
    public interface ISignal
    {
        void CenterOfArmiesChanged(Vector3 center);

        void ProjectileCreated(int armyId, Vector3 position, Vector3 direction);

        void UnitAttacked(int unitId);

        void UnitDied(int unitId);

        void UnitHit(int unitId, Vector3 attackDir);
    }
}