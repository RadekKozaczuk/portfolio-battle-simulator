using System;
using System.Collections.Generic;
using Core.Models;
using GameLogic.Interfaces;
using NUnit.Framework;

namespace Tests.BattleModel
{
    class GetUnits
    {
        [Test]
        public void OneWarriorArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 0);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> units = battle.GetUnits(0);
            Span<UnitModel> warriors = battle.GetUnits(0, 0);
            Span<UnitModel> archers = battle.GetUnits(0, 1);

            // 3. Assert
            Assert.That(units.Length == 1);
            Assert.That(warriors.Length == 1);
            Assert.That(archers.Length == 0);
        }

        [Test]
        public void OneWarriorOneArcherArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> units = battle.GetUnits(0);
            Span<UnitModel> warriors = battle.GetUnits(0, 0);
            Span<UnitModel> archers = battle.GetUnits(0, 1);

            // 3. Assert
            Assert.That(units.Length == 2);
            Assert.That(warriors.Length == 1);
            Assert.That(archers.Length == 1);
        }

        [Test]
        public void FiftyArchersArmy()
        {
            // 1. Arrange
            var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> units = battle.GetUnits(0);
            Span<UnitModel> warriors = battle.GetUnits(0, 0);
            Span<UnitModel> archers = battle.GetUnits(0, 1);

            // 3. Assert
            Assert.That(units.Length == 50);
            Assert.That(warriors.Length == 0);
            Assert.That(archers.Length == 50);
        }

        [Test]
        public void TwoArmies_OneWarrior_OneArcher()
        {
            // 1. Arrange
            var army1 = new ArmyModel(1, 0);
            var army2 = new ArmyModel(0, 1);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Span<UnitModel> units = battle.GetUnits();
            Span<UnitModel> units1 = battle.GetUnits(0);
            Span<UnitModel> warriors1 = battle.GetUnits(0, 0);
            Span<UnitModel> archers1 = battle.GetUnits(0, 1);
            Span<UnitModel> units2 = battle.GetUnits(1);
            Span<UnitModel> warriors2 = battle.GetUnits(1, 0);
            Span<UnitModel> archers2 = battle.GetUnits(1, 1);

            // 3. Assert
            Assert.That(units.Length == 2);
            Assert.That(units1.Length == 1);
            Assert.That(warriors1.Length == 1);
            Assert.That(archers1.Length == 0);
            Assert.That(units2.Length == 1);
            Assert.That(warriors2.Length == 0);
            Assert.That(archers2.Length == 1);
        }
    }
}