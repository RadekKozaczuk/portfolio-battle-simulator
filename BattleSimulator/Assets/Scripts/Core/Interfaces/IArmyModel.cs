using Core.Enums;

namespace Core.Interfaces
{
    public interface IArmyModel
    {
        public int GetUnitCount(UnitType unitType);

        public Strategy GetStrategy(UnitType unitType);
    }
}