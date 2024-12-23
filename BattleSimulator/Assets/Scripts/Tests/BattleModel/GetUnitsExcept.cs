using System;
using System.Collections.Generic;
using Core.Models;
using GameLogic.Interfaces;
using NUnit.Framework;

namespace Tests.BattleModel
{
    class GetUnitsExcept
    {
        [Test]
        public void OneArmy_FiftyArchers_ExceptSecondToLast()
        {
            // 1. Arrange
            var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Memory<UnitModel>[] units = battle.GetUnitsExcept(0, 48);
            Memory<UnitModel>[] warriors = battle.GetUnitsExcept(0, 0, 48);
            Memory<UnitModel>[] archers = battle.GetUnitsExcept(0, 1, 48);

            Span<UnitModel> unitsSpan1 = units[0].Span;
            Span<UnitModel> unitsSpan2 = units[1].Span;
            Span<UnitModel> warriorsSpan = warriors[0].Span;
            Span<UnitModel> archersSpan1 = archers[0].Span;
            Span<UnitModel> archersSpan2 = archers[1].Span;

            // 3. Assert
            Assert.That(units.Length == 2);
            Assert.That(warriors.Length == 1);
            Assert.That(archers.Length == 2);

            Assert.That(unitsSpan1.Length == 47);
            Assert.That(unitsSpan2.Length == 1);
            Assert.That(warriorsSpan.Length == 0);
            Assert.That(archersSpan1.Length == 47);
            Assert.That(archersSpan2.Length == 1);
        }

        [Test]
        public void OneArmy_OneWarrior()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 0);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
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
        public void OneArmy_OneWarriorOneArcher_ExceptFirst()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
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
        public void TwoArmies_FiftySoldiersCombined_Except20th()
        {
            // 1. Arrange
            var army1 = new ArmyModel(10, 10);
            var army2 = new ArmyModel(15, 15);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies);
            Memory<UnitModel>[] units1 = battle.GetUnitsExcept(0, 20);
            Memory<UnitModel>[] warriors1 = battle.GetUnitsExcept(0, 0, 20);
            Memory<UnitModel>[] archers1 = battle.GetUnitsExcept(0, 1, 20);

            Memory<UnitModel>[] units2 = battle.GetUnitsExcept(1, 20);
            Memory<UnitModel>[] warriors2 = battle.GetUnitsExcept(1, 0, 20);
            //Memory<UnitModel>[] archers2 = battle.GetUnitsExcept(1, 1, 20);

            Span<UnitModel> unitsSpan1 = units1[0].Span;
            Span<UnitModel> warriorsSpan1 = warriors1[0].Span;
            Span<UnitModel> archersSpan1 = archers1[0].Span;

            Span<UnitModel> unitsSpan2 = units2[0].Span;
            Span<UnitModel> warriorsSpan2 = warriors2[0].Span;
            //Span<UnitModel> archersSpan2 = archers2[0].Span;

            // 3. Assert
            Assert.That(units1.Length == 1);
            Assert.That(warriors1.Length == 1);
            Assert.That(archers1.Length == 1);

            Assert.That(units2.Length == 1);
            Assert.That(warriors2.Length == 0);
            //Assert.That(archers2.Length == 1);
        }
    }
}