// ReSharper disable UnusedMemberInSuper.Global

using UnityEngine;

namespace Core
{
    public interface ISignal
    {
        void CenterOfArmiesChanged(Vector3 center);

        void ProjectileCreated(int id, int armyId, Vector3 position, Vector3 direction);

        void ProjectileDestroyed(int id);

        void ProjectilePositionChanged(int id, Vector3 position);

        void UnitAttacked(int unitId);

        void UnitDied(int unitId);

        void UnitHit(int unitId, Vector3 attackDir);

        void Victory(int armiesLeft);
    }
}