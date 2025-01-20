using System;
using System.Collections.Generic;
using Core.Models;
using GameLogic.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace Tests.BattleModel
{
    class GetUnitsExcept_OneArmy
    {
        Bounds[] _bounds;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            _bounds = new Bounds[] {new(Vector3.zero, Vector3.zero), new(Vector3.zero, Vector3.zero)};
        }

        [Test]
        public void FiftyArchers_Except48th_AllTypesOnly()
        {
            // 1. Arrange
            var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] units = battle.GetUnitsExcept(0, 48);
            Span<UnitModel> unitsSpan1 = units[0].Span;
            Span<UnitModel> unitsSpan2 = units[1].Span;

            // 3. Assert
            Assert.That(units.Length == 2);
            Assert.That(unitsSpan1.Length == 48);
            Assert.That(unitsSpan2.Length == 1);
        }

        [Test]
        public void FiftyArchers_Except48th_WarriorsOnly()
        {
            // 1. Arrange
            var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] warriors = battle.GetUnitsExcept(0, 0, 48);
            Span<UnitModel> warriorsSpan = warriors[0].Span;

            // 3. Assert
            Assert.That(warriors.Length == 1);
            Assert.That(warriorsSpan.Length == 0);
        }

        [Test]
        public void FiftyArchers_Except48th_ArchersOnly()
        {
            // 1. Arrange
            var army = new ArmyModel(0, 50);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] archers = battle.GetUnitsExcept(0, 1, 48);
            Span<UnitModel> archersSpan1 = archers[0].Span;
            Span<UnitModel> archersSpan2 = archers[1].Span;

            // 3. Assert
            Assert.That(archers.Length == 2);
            Assert.That(archersSpan1.Length == 48);
            Assert.That(archersSpan2.Length == 1);
        }

        [Test]
        public void OneWarrior()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 0);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
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
        public void OneWarriorOneArcher_ExceptFirst()
        {
            // 1. Arrange
            var army = new ArmyModel(1, 1);
            var armies = new List<ArmyModel> {army};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
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
    }
}