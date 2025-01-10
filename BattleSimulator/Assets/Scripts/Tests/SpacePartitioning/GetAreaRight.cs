using System;
using System.Reflection;
using GameLogic.Controllers;
using GameLogic.Interfaces;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.SpacePartitioning
{
    public class GetAreaRight
    {
        ISpacePartitioningController _spc;
        Type _spcType;
        MethodInfo _getAreaRight;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // 25 quadrants, 4 units
            _spc = new SpacePartitioningController(bounds, 5, 7);
            _spcType = _spc.GetType();
            _getAreaRight = _spcType.GetMethod(
                "GetAreaRight", BindingFlags.NonPublic | BindingFlags.Instance);

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
            // |   |   |   |   | 5 | <- 24th quadrant
            // |   |   |   |   |   |
            // |   |   | 0 |   |   |
            // |   | 1 |   | 2 |   |
            // | 6 |34 |   |   |   |

            // sort the elements
            sort!.Invoke(_spc, new object[] { });
        }

        [TearDown]
        public void TearDown()
        {
            _spc = null;
            _spcType = null;
            _getAreaRight = null;
        }

        [Test]
        public void Quadrant5_25Quadrants_Radius4()
        {
            // 2. Act
            dynamic memoryList = _getAreaRight!.Invoke(_spc, new object[] {0, 1, 4});
            dynamic memoryArray = Utils.MemoryListToMemoryArray(memoryList);
            dynamic memory = (memoryArray as Array)!.GetValue(4);
            dynamic units = Utils.MemoryToArray(memory);

            // 3. Assert
            Assert.IsTrue(memoryArray.Length == 5);
            Assert.IsTrue(units.Length == 1);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");

            Assert.IsTrue(id0 == 5);
        }
    }
}