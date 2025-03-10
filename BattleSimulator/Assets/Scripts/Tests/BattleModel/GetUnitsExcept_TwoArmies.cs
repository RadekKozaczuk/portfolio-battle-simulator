using System;
using System.Collections.Generic;
using Core.Models;
using GameLogic.Interfaces;
using NUnit.Framework;
using UnityEngine;

namespace Tests.BattleModel
{
    class GetUnitsExcept_TwoArmies
    {
        Bounds[] _bounds;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            _bounds = new Bounds[] {new(Vector3.zero, Vector3.zero), new(Vector3.zero, Vector3.zero)};
        }

        [Test]
        public void FiftySoldiersCombined_Except20th()
        {
            // 1. Arrange
            var army1 = new ArmyModel(10, 10);
            var army2 = new ArmyModel(15, 15);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] units1 = battle.GetUnitsExcept(0, 20);
            Memory<UnitModel>[] warriors1 = battle.GetUnitsExcept(0, 0, 20);
            Memory<UnitModel>[] archers1 = battle.GetUnitsExcept(0, 1, 20);

            Memory<UnitModel>[] units2 = battle.GetUnitsExcept(1, 20);
            Memory<UnitModel>[] warriors2 = battle.GetUnitsExcept(1, 0, 20);
            Memory<UnitModel>[] archers2 = battle.GetUnitsExcept(1, 1, 20);

            Span<UnitModel> unitsSpan1 = units1[0].Span;
            Span<UnitModel> warriorsSpan1 = warriors1[0].Span;
            Span<UnitModel> archersSpan1 = archers1[0].Span;

            Span<UnitModel> unitsSpan2 = units2[0].Span;
            Span<UnitModel> warriorsSpan2 = warriors2[0].Span;
            Span<UnitModel> archersSpan2 = archers2[0].Span;

            // 3. Assert
            Assert.That(units1.Length == 1);
            Assert.That(warriors1.Length == 1);
            Assert.That(archers1.Length == 1);

            Assert.That(units2.Length == 1);
            Assert.That(warriors2.Length == 1);
            Assert.That(archers2.Length == 1);

            Assert.That(unitsSpan1.Length == 20);
            Assert.That(warriorsSpan1.Length == 10);
            Assert.That(archersSpan1.Length == 10);

            Assert.That(unitsSpan2.Length == 29);
            Assert.That(warriorsSpan2.Length == 14);
            Assert.That(archersSpan2.Length == 15);
        }

        [Test]
        public void Each100Soldiers_Except102nd_AllTypesOnly()
        {
            // 1. Arrange
            var army1 = new ArmyModel(100, 0);
            var army2 = new ArmyModel(100, 0);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] army1Units = battle.GetUnitsExcept(0, 101);
            Memory<UnitModel>[] army2Units = battle.GetUnitsExcept(1, 101);

            Span<UnitModel> army1UnitsSpan = army1Units[0].Span;
            Span<UnitModel> army2UnitsSpan1 = army2Units[0].Span;
            Span<UnitModel> army2UnitsSpan2 = army2Units[1].Span;

            // 3. Assert
            Assert.That(army1Units.Length == 1);
            Assert.That(army2Units.Length == 2);
            Assert.That(army1UnitsSpan.Length == 100);
            Assert.That(army2UnitsSpan1.Length == 1);
            Assert.That(army2UnitsSpan2.Length == 98);
        }

        [Test]
        public void Each100Soldiers_Except102nd_WarriorsOnly()
        {
            // 1. Arrange
            var army1 = new ArmyModel(100, 0);
            var army2 = new ArmyModel(100, 0);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] army1Warriors = battle.GetUnitsExcept(0, 0, 101);
            Memory<UnitModel>[] army2Warriors = battle.GetUnitsExcept(1, 0, 101);

            Span<UnitModel> army1WarriorsSpan = army1Warriors[0].Span;
            Span<UnitModel> army2WarriorsSpan1 = army2Warriors[0].Span;
            Span<UnitModel> army2WarriorsSpan2 = army2Warriors[1].Span;

            // 3. Assert
            Assert.That(army1Warriors.Length == 1);
            Assert.That(army2Warriors.Length == 2);
            Assert.That(army1WarriorsSpan.Length == 100);
            Assert.That(army2WarriorsSpan1.Length == 1);
            Assert.That(army2WarriorsSpan2.Length == 98);
        }

        [Test]
        public void Each100Soldiers_Except102nd_ArchersOnly()
        {
            // 1. Arrange
            var army1 = new ArmyModel(100, 0);
            var army2 = new ArmyModel(100, 0);
            var armies = new List<ArmyModel> {army1, army2};

            // 2. Act
            IBattleModel battle = new GameLogic.Models.BattleModel(armies, _bounds);
            Memory<UnitModel>[] army1Archers = battle.GetUnitsExcept(0, 1, 101);
            Memory<UnitModel>[] army2Archers = battle.GetUnitsExcept(1, 1, 101);

            Span<UnitModel> army1ArchersSpan = army1Archers[0].Span;
            Span<UnitModel> army2ArchersSpan1 = army2Archers[0].Span;

            // 3. Assert
            Assert.That(army1Archers.Length == 1);
            Assert.That(army2Archers.Length == 1);

            Assert.That(army1ArchersSpan.Length == 0);
            Assert.That(army2ArchersSpan1.Length == 0);
        }
    }
}