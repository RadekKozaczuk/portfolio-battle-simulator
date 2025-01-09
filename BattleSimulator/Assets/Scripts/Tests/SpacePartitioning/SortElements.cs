using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Models;
using GameLogic.Controllers;
using GameLogic.Interfaces;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.SpacePartitioning
{
    public class SortElements
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
        public void MoveDeadOrOutsideElements()
        {
            // 1. Arrange
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // four quadrants, 5 units
            var spc = new SpacePartitioningController(bounds, 2, 5);
            MethodInfo sortMethod = spc.GetType().GetMethod(
                "SortElements", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo insideInfo = spc.GetType().GetField(
                "_inside", BindingFlags.NonPublic | BindingFlags.Instance);

            FieldInfo insideCountInfo = spc.GetType().GetField(
                "_insideCount", BindingFlags.NonPublic | BindingFlags.Instance);

            // 2. Act
            // add 5 units
            spc.AddUnit(0, 0, new float2(3f, 2f));   // will land in 3rd
            spc.AddUnit(1, 0, new float2(4f, -4f));  // will land in 1st
            spc.AddUnit(2, 0, new float2(-2f, -1f)); // will land in 0th
            spc.AddUnit(3, 0, new float2(-3f, -2f)); // will land in 0th
            spc.AddUnit(4, 0, new float2(4f, 1f));   // will land in 3rd

            // kill 3rd
            spc.KillUnit(2);

            // check if the methods exists
            Assert.IsNotNull(sortMethod);
            Assert.IsNotNull(insideInfo);
            Assert.IsNotNull(insideCountInfo);

            // invoke sorting
            object _ = sortMethod.Invoke(spc, new object[] { });

            // 3. Assert
            dynamic units = insideInfo.GetValue(spc);
            dynamic insideCount = insideCountInfo.GetValue(spc);

            // there is 5 units in total
            Assert.IsTrue(units.Length == 5);

            // there is only 4 units alive
            Assert.IsTrue(insideCount == 4);

            // first for are sorted in ascending order by quadrant
            int x0 = GetUnitValue(0, "QuadrantIdX");
            int y0 = GetUnitValue(0, "QuadrantIdY");

            int x1 = GetUnitValue(1, "QuadrantIdX");
            int y1 = GetUnitValue(1, "QuadrantIdY");

            int x2 = GetUnitValue(2, "QuadrantIdX");
            int y2 = GetUnitValue(2, "QuadrantIdY");

            int x3 = GetUnitValue(3, "QuadrantIdX");
            int y3 = GetUnitValue(3, "QuadrantIdY");

            Assert.IsTrue(x0 <= x1 && x1 <= x2 && x2 <= x3);
            Assert.IsTrue(y0 <= y1 && y1 <= y2 && y2 <= y3);

            // fifth unit should be dead
            bool dead4 = GetUnitValue(4, "DeadOrOutside");
            int id4 = GetUnitValue(4, "UnitId");
            Assert.IsTrue(dead4 && id4 == 2);

            return;

            dynamic GetUnitValue(int index, string fieldName)
            {
                dynamic u = (units as Array)!.GetValue(index);
                FieldInfo fInfo = u.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                return fInfo.GetValue(u);
            }
        }
    }
}