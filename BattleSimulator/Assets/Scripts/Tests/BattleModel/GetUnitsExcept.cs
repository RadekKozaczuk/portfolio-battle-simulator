using System;
using System.Collections.Generic;
using Core.Models;
using NUnit.Framework;

namespace Tests.BattleModel
{
    class GetUnitsExcept
    {
        [Test]
        public void OneWarriorArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 0);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            var battle = new GameLogic.Models.BattleModel(armies);
            Memory<UnitModel>[] units = battle.GetUnitsExcept(0, 0);
            Memory<UnitModel>[] warriors = battle.GetUnitsExcept(0, 0, 0);
            Memory<UnitModel>[] archers = battle.GetUnitsExcept(0, 1, 0);

            Span<UnitModel> unitsSpan = units[0].Span;
            Span<UnitModel> warriorsSpan = warriors[0].Span;
            Span<UnitModel> archersSpan = archers[0].Span;

            // 3. Assert
            Assert.That(units.Length == 1);
            Assert.That(warriors.Length == 1);
            Assert.That(archers.Length == 1);

            Assert.That(unitsSpan.Length == 0);
            Assert.That(warriorsSpan.Length == 0);
            Assert.That(archersSpan.Length == 0);
        }

        [Test]
        public void OneWarriorOneArcherArmy_ExceptFirst()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            var battle = new GameLogic.Models.BattleModel(armies);
            Memory<UnitModel>[] units = battle.GetUnitsExcept(0, 0);
            Memory<UnitModel>[] warriors = battle.GetUnitsExcept(0, 0, 0);
            Memory<UnitModel>[] archers = battle.GetUnitsExcept(0, 1, 0);
            Span<UnitModel> unitsSpan = units[0].Span;
            Span<UnitModel> warriorsSpan = warriors[0].Span;
            Span<UnitModel> archersSpan = archers[0].Span;

            // 3. Assert
            Assert.That(units.Length == 1);
            Assert.That(warriors.Length == 1);
            Assert.That(archers.Length == 1);

            Assert.That(unitsSpan.Length == 1);
            Assert.That(warriorsSpan.Length == 0);
            Assert.That(archersSpan.Length == 1);
        }

        [Test]
        public void FiftyArchersArmy()
        {
            // 1. Arrange
            /*var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            var battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> allies = battle.GetAllies(0);
            Span<UnitModel> warriors = battle.GetAllies(0, 0);
            Span<UnitModel> archers = battle.GetAllies(0, 1);

            // 3. Assert
            Assert.That(allies.Length == 50);
            Assert.That(warriors.Length == 0);
            Assert.That(archers.Length == 50);*/
        }

        [Test]
        public void TwoArmies_OneWarrior_OneArcher()
        {
            /*// 1. Arrange
            var army1 = new ArmyModel(1, 0);
            var army2 = new ArmyModel(0, 1);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            var battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> allies1 = battle.GetAllies(0);
            Span<UnitModel> warriors1 = battle.GetAllies(0, 0);
            Span<UnitModel> archers1 = battle.GetAllies(0, 1);
            Span<UnitModel> allies2 = battle.GetAllies(1);
            Span<UnitModel> warriors2 = battle.GetAllies(1, 0);
            Span<UnitModel> archers2 = battle.GetAllies(1, 1);

            // 3. Assert
            Assert.That(allies1.Length == 1);
            Assert.That(warriors1.Length == 1);
            Assert.That(archers1.Length == 0);
            Assert.That(allies2.Length == 1);
            Assert.That(warriors2.Length == 0);
            Assert.That(archers2.Length == 1);*/
        }
    }
}