using Core.Enums;

namespace Core.Models
{
    /// <summary>
    /// Representation of the army. The amount and the strategy of every unit type.
    /// </summary>
    public class ArmyModel
    {
        public int UnitCount => Warriors + Archers;

        public int UnitTypeCount => _units.Length;

        public int WarriorCount => _units[0];

        public int ArcherCount => _units[1];

        public readonly int Warriors;
        public readonly int Archers;
        public readonly Strategy StrategyWarrior;
        public readonly Strategy StrategyArcher;

        readonly int[] _units;

        public ArmyModel(int warriors, int archers)
        {
            Warriors = warriors;
            Archers = archers;
            _units = new [] {warriors, archers};

            StrategyWarrior = Strategy.Basic;
            StrategyArcher = Strategy.Basic;
        }

        public ArmyModel(int warriors, int archers, Strategy strategyWarrior, Strategy strategyArcher)
        {
            Warriors = warriors;
            Archers = archers;
            _units = new [] {warriors, archers};

            StrategyWarrior = strategyWarrior;
            StrategyArcher = strategyArcher;
        }

        public int GetUnitCount(int unitType) => _units[unitType];
    }
}