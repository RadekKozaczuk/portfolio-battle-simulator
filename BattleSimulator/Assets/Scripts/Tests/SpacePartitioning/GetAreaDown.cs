using System;
using System.Reflection;
using GameLogic.Controllers;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.SpacePartitioning
{
    public class GetAreaDown
    {
        SpacePartitioningController _spc;
        Type _spcType;
        MethodInfo _getAreaDown;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // 25 quadrants, 4 units
            _spc = new SpacePartitioningController(bounds, 5, 7);
            _spcType = _spc.GetType();
            _getAreaDown = _spcType.GetMethod(
                "GetAreaDown", BindingFlags.NonPublic | BindingFlags.Instance);

            MethodInfo sort = _spcType.GetMethod(
                "SortElements", BindingFlags.NonPublic | BindingFlags.Instance);

            _spc.AddUnit(0, 0, new float2(0, 0));         // 12th quadrant
            _spc.AddUnit(1, 0, new float2(-2.5f, -2.5f)); // 6th quadrant
            _spc.AddUnit(2, 0, new float2(2.5f, -2.5f));  // 8th quadrant
            _spc.AddUnit(3, 0, new float2(-2.5f, -5f));   // 1st quadrant
            _spc.AddUnit(4, 0, new float2(-2.5f, -5f));   // 1st quadrant
            _spc.AddUnit(5, 0, new float2(4f, 4f));       // 24th quadrant
            _spc.AddUnit(6, 0, new float2(-10f, -15f));   // 0th quadrant

            // sort the elements
            sort!.Invoke(_spc, new object[] { });
        }

        [TearDown]
        public void TearDown()
        {
            _spc = null;
            _spcType = null;
            _getAreaDown = null;
        }

        [Test]
        public void Center_25Quadrants_Radius1()
        {
            // 2. Act
            dynamic units = Utils.MemoryToArray(_getAreaDown!.Invoke(_spc, new object[] {2, 2, 1}));

            // 3. Assert
            Assert.IsTrue(units.Length == 2);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");
            int id1 = Utils.GetElementValue(units, 1, "UnitId");

            Assert.IsTrue(id0 == 1);
            Assert.IsTrue(id1 == 2);
        }

        [Test]
        public void Center_25Quadrants_Radius2()
        {
            // 2. Act
            dynamic units = Utils.MemoryToArray(_getAreaDown!.Invoke(_spc, new object[] {2, 2, 2}));

            // 3. Assert
            Assert.IsTrue(units.Length == 3);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");
            int id1 = Utils.GetElementValue(units, 1, "UnitId");
            int id2 = Utils.GetElementValue(units, 2, "UnitId");

            Assert.IsTrue(id0 == 6);
            Assert.IsTrue(id1 == 3);
            Assert.IsTrue(id2 == 4);
        }

        [Test]
        public void TopLeftCorner_25Quadrants_Radius1()
        {
            // 2. Act
            dynamic units = Utils.MemoryToArray(_getAreaDown!.Invoke(_spc, new object[] {0, 4, 1}));

            // 3. Assert
            Assert.IsTrue(units.Length == 0);
        }

        [Test]
        public void TopLeftCorner_25Quadrants_Radius4()
        {
            // 2. Act
            dynamic units = Utils.MemoryToArray(_getAreaDown!.Invoke(_spc, new object[] {0, 4, 4}));

            // 3. Assert
            Assert.IsTrue(units.Length == 3);

            int id0 = Utils.GetElementValue(units, 0, "UnitId");
            int id1 = Utils.GetElementValue(units, 1, "UnitId");
            int id2 = Utils.GetElementValue(units, 2, "UnitId");

            Assert.IsTrue(id0 == 6);
            Assert.IsTrue(id1 == 3);
            Assert.IsTrue(id2 == 4);
        }
    }
}