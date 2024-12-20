using Core.Enums;

namespace Core.Models
{
    public class ArmyModel
    {
        public int TotalUnitCount => Warriors + Archers;

        public readonly int Warriors;
        public readonly int Archers;
        public readonly Strategy StrategyWarrior;
        public readonly Strategy StrategyArcher;

        public ArmyModel(int warriors, int archers, Strategy strategyWarrior, Strategy strategyArcher)
        {
            Warriors = warriors;
            Archers = archers;
            StrategyWarrior = strategyWarrior;
            StrategyArcher = strategyArcher;
        }
    }
}