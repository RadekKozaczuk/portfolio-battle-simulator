using Core.Models;

namespace GameLogic.Services
{
    static class PersistentStorageService
    {
        internal static void SaveArmy(int id, ArmyModel army)
        {

        }

        internal static ArmyModel LoadArmy(int id)
        {
            return new ArmyModel(1, 1);
        }
    }
}