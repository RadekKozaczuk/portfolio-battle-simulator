using Core.Enums;

namespace Core.Interfaces
{
    public interface IArmyModel
    {
        public int GetUnitCount(int unitType);

        public Strategy GetStrategy(int unitType);
    }
}