using System;
using System.Reflection;
using GameLogic.Controllers;
using GameLogic.Interfaces;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.SpacePartitioning
{
    public class GetAreaLeft
    {
        ISpacePartitioningController _spc;
        Type _spcType;
        MethodInfo _getAreaLeft;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // 25 quadrants, 4 units
            _spc = new SpacePartitioningController(bounds, 5, 7);
            _spcType = _spc.GetType();
            _getAreaLeft = _spcType.GetMethod(
                "GetAreaLeft", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo sort = _spcType.GetMethod(
                "SortElements", BindingFlags.NonPublic | BindingFlags.Instance);

            _spc.AddUnit(0, 0, new float2(0, 0));         // 12th quadrant
            _spc.AddUnit(1, 0, new float2(-2.5f, -2.5f)); // 6th quadrant
            _spc.AddUnit(2, 0, new float2(2.5f, -2.5f));  // 8th quadrant
            _spc.AddUnit(3, 0, new float2(-2.5f, -5f));   // 1st quadrant
            _spc.AddUnit(4, 0, new float2(-2.5f, -5f));   // 1st quadrant
            _spc.AddUnit(5, 0, new float2(4f, 4f));       // 24th quadrant
            _spc.AddUnit(6, 0, new float2(-10f, -15f));   // 0th quadrant

            // layout
            //    -3  -1   1   3
            // |   |   |   |   | 5 | <- 24th quadrant
            // |   |   |   |   |   |
            // |   |   | 0 |   |   |
            // |   | 1 |   | 2 |   |
            // | 6 |34 |   |   |   |

            sort!.Invoke(_spc, new object[] { });
        }

        [TearDown]
        public void TearDown()
        {
            _spc = null;
            _spcType = null;
            _getAreaLeft = null;
        }

        [Test]
        public void Quadrant13_25Quadrants_Radius1()
        {
            // 2. Act
            dynamic memories = _getAreaLeft!.Invoke(_spc, new object[] {3, 2, 1});
            dynamic memory = Utils.GetMemoryFromMemoryList(memories, 0);
            dynamic units = Utils.MemoryToArray(memory);

            // 3. Assert
            Assert.IsTrue(units.Length == 1);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");

            Assert.IsTrue(id0 == 0);
        }

        [Test]
        public void Quadrant13_25Quadrants_Radius2()
        {
            // 2. Act
            dynamic memories = _getAreaLeft!.Invoke(_spc, new object[] {3, 2, 2});
            dynamic memory = Utils.GetMemoryFromMemoryList(memories, 0);
            dynamic units = Utils.MemoryToArray(memory);

            // 3. Assert
            Assert.IsTrue(units.Length == 1);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");

            Assert.IsTrue(id0 == 1);
        }
    }
}