using System.Collections.Generic;
using System.Reflection;
using GameLogic.Controllers;
using GameLogic.Interfaces;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Tests.SpacePartitioning
{
    public class FindAllNearbyUnits
    {
        ISpacePartitioningController _spc;

        [SetUp]
        public void SetUp()
        {
            // Arrange the common setup for the tests
            var bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(10, 1, 10));

            // 25 quadrants, 4 units
            _spc = new SpacePartitioningController(bounds, 5, 7);

            MethodInfo sort = _spc.GetType().GetMethod(
                "SortElements", BindingFlags.NonPublic | BindingFlags.Instance);

            _spc.AddUnit(0, 1, new float2(0, 0));         // 12th quadrant
            _spc.AddUnit(1, 0, new float2(-2.5f, -2.5f)); // 6th quadrant
            _spc.AddUnit(2, 0, new float2(2.5f, -2.5f));  // 8th quadrant
            _spc.AddUnit(3, 0, new float2(-2.5f, -4.1f)); // 1st quadrant
            _spc.AddUnit(4, 1, new float2(-2.5f, -5f));   // 1st quadrant
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
        }

        [Test]
        public void VariousPositionsAndMaxDistances()
        {
            // 2. Act
            List<int> enemies1 = _spc.FindAllNearbyUnits(new float2(-4f, 4f), 0, 2f);
            List<int> enemies2 = _spc.FindAllNearbyUnits(new float2(3.5f, -2f), 0, 2f);
            List<int> enemies3 = _spc.FindAllNearbyUnits(new float2(-2.5f, -4.1f), 3, 4f);

            // 3. Assert
            Assert.That(enemies1.Count == 0);

            Assert.That(enemies2.Count == 1);
            Assert.That(enemies2.Contains(2));

            Assert.That(enemies3.Count == 2);
            Assert.That(enemies3.Contains(1));
            Assert.That(enemies3.Contains(4));
        }
    }
}