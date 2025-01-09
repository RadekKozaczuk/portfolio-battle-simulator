using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace GameLogic.Controllers
{
    /// <summary>
    /// Job of this controller is to speed up search of elements in 2D space.
    /// The algorithm divides the space into equal brackets.
    /// </summary>
    class SpacePartitioningController// : ISpatialPartitioningController
    {
        class UnitComparer : IComparer<Unit>
        {
            public int Compare(Unit a, Unit b)
            {
                int valueA = a.QuadrantIdX + a.QuadrantIdY * a.QuadrantSize;
                int valueB = b.QuadrantIdX + b.QuadrantIdY * b.QuadrantSize;

                return valueA.CompareTo(valueB); // Sort by age in ascending order
            }
        }

        struct Unit
        {
            // consider data packing
            internal readonly int UnitId;
            internal readonly int ArmyId;
            internal int QuadrantIdX;
            internal int QuadrantIdY;
            internal float2 Position;
            internal bool DeadOrOutside;

            // todo: unfortunately it has to present in every struct as we want the controller to have many instances
            internal readonly int QuadrantSize;

            internal Unit(int unitId, int armyId, int quadrantIdX, int quadrantIdY, float2 position, int quadrantSize)
            {
                UnitId = unitId;
                ArmyId = armyId;
                QuadrantIdX = quadrantIdX;
                QuadrantIdY = quadrantIdY;
                Position = position;
                DeadOrOutside = false;
                QuadrantSize = quadrantSize;
            }

            internal Unit(int unitId, int armyId, float2 position, int quadrantSize)
            {
                UnitId = unitId;
                ArmyId = armyId;
                QuadrantIdX = int.MinValue;
                QuadrantIdY = int.MinValue;
                Position = position;
                DeadOrOutside = false;
                QuadrantSize = quadrantSize;
            }
        }

        /// <summary>
        /// Units that are outside the boundaries.
        /// </summary>
        readonly List<Unit> _outside = new();

        /// <summary>
        /// Units that are inside the boundaries.
        /// Size is equal the elementCount.
        /// Elements at the end are noise.
        /// </summary>
        readonly Unit[] _inside;

        /// <summary>
        /// Number of elements in the <see cref="_inside"/> table.
        /// Everything on the right of that is a noise.
        /// </summary>
        int _insideCount;

        readonly int _size;

        readonly float[] _bracketsX;
        readonly float[] _bracketsY;

        readonly UnitComparer _comparer = new();

        // for DI, for now
        [Preserve]
        SpacePartitioningController()
        {
            _inside = null!;
            _bracketsX = null!;
            _bracketsY = null!;
        }

        /// <summary>
        /// Quadrant 0, 0 will be in the bottom left.
        /// </summary>
        /// <param name="bounds">Y value is ignored</param>
        /// <param name="size">Number of quadrants. Must be greater than 1.</param>
        /// <param name="elementCount">Number of units</param> // todo: should be unitCount
        internal SpacePartitioningController(Bounds bounds, int size, int elementCount)
        {
            Assert.IsTrue(size > 0, $"Quadrant matrix dimension size must be greater than 0. Was: {size}");

            float xMin = bounds.min.x;
            float xMax = bounds.max.x;
            float yMin = bounds.min.z;
            float yMax = bounds.max.z;

            _size = size;

            float bracketWidthX = (xMax - xMin) / size;
            _bracketsX = new float[size + 1];
            for (int i = 0; i <= size; i++)
                _bracketsX[i] = xMin + i * bracketWidthX;

            float bracketWidthY = (yMax - yMin) / size;
            _bracketsY = new float[size + 1];
            for (int i = 0; i <= size; i++)
                _bracketsY[i] = yMin + i * bracketWidthY;

            _inside = new Unit[elementCount];
        }

        /// <summary>
        /// Adding a new element means to simply set its quadrantId
        /// </summary>
        public void AddUnit(int unitId, int armyId, float2 position)
        {
            if (TryPositionToQuadrant(position, out int x, out int y))
            {
                _inside[unitId] = new Unit(unitId, armyId, x, y, position, _size);
                _insideCount++;
            }
            else
                _outside.Add(new Unit(unitId, armyId, x, y, position, _size));
        }

        public void UpdateUnit(int unitId, float2 position)
        {
            bool shouldBeInside = TryPositionToQuadrant(position, out int x, out int y);

            int index = _outside.FindIndex(u => u.UnitId == unitId);
            if (index == -1) // is in the inside table
            {
                for (int i = 0; i < _insideCount; i++)
                    if (_inside[i].UnitId == unitId)
                        // check where it supposes to be now
                        if (shouldBeInside)
                        {
                            // should stay in this table
                            _inside[unitId].Position = position;
                            _inside[unitId].QuadrantIdX = x;
                            _inside[unitId].QuadrantIdY = y;
                        }
                        else
                        {
                            // should move to outside table
                            _inside[unitId].DeadOrOutside = true; // will be moved to the end of the inside table
                            _insideCount--;
                            _outside.Add(new Unit(unitId, _inside[unitId].ArmyId, position, _size));
                        }
            }
            else // is in the outside table
            {
                int armyId = _outside[index].ArmyId;
                if (shouldBeInside)
                {
                    // should go to the inside table
                    _inside[_insideCount++] = new Unit(unitId, armyId, x, y, position, _size);
                    _outside.RemoveAt(index);
                }
                else
                {
                    // should stay in the outside table, just update position
                    _outside[index] = new Unit(unitId, armyId, position, _size);
                }
            }
        }

        public void KillUnit(int unitId)
        {
            int index = _outside.FindIndex(u => u.UnitId == unitId);
            if (index == -1) // is in the inside table
            {
                for (int i = 0; i < _insideCount; i++)
                    if (_inside[i].UnitId == unitId)
                    {
                        _inside[unitId].DeadOrOutside = true; // will be moved to the end of the inside table
                        _insideCount--;
                        _deadOrOutsideElementsThisFrame = true;
                        return;
                    }
            }
            else
            {
                _outside.RemoveAt(index);
            }
        }

        /// <summary>
        /// True if at least one unit was either removed whatsoever
        /// or moved from <see cref="_inside"/> table to <see cref="_outside"/> list.
        /// </summary>
        bool _deadOrOutsideElementsThisFrame;

        /// <summary>
        /// Sorts inside elements by quadrant id.
        /// Then by armyId.
        /// It allows for faster data retrieval.
        /// </summary>
        void SortElements()
        {
            // todo: probably we need to assert for small set sizes
            // todo: all dead units
            // todo: all units outside and such

            // todo: also case when there is only one quadrant

            // move dead to the right
            if (_deadOrOutsideElementsThisFrame)
            {
                // go from start to _elementCount and keep swapping elements until you reach the end
                for (int i = 0; i < _insideCount; i++)
                    if (_inside[i].DeadOrOutside)
                        Swap(_insideCount, i);

                _deadOrOutsideElementsThisFrame = false;
            }

            Array.Sort(_inside, 0, _insideCount, _comparer);
        }

        void Swap(int a, int b) => (_inside[b], _inside[a]) = (_inside[a], _inside[b]);

        // System Works Like This:
        // 1) elements are added
        // - elements are updated (positions)
        // - elements are sorted (by quadrants)
        // and then search algorithm goes through them using spans
        // up to us if we use lists or whatever to return values

        // todo: maybe will be reused if system is going to use list as a return structure
        //public void ReturnList(List<int> list) => _retValPool.Release(list);

        /// <summary>
        /// Returns the x and y of the quadrant this position belongs to.
        /// If no quadrant could be found (the position is out of bounds) the x and y will be equal to <see cref="int.MinValue"/>.
        /// </summary>
        bool TryPositionToQuadrant(float2 position, out int x, out int y)
        {
            // Find which bracket the number falls into
            for (int i = 0; i < _size; i++)
                if (position.x >= _bracketsX[i] && position.x < _bracketsX[i + 1])
                {
                    x = i;
                    goto step2;
                }

            x = int.MinValue;
            y = int.MinValue;
            return false;

        step2:
            for (int i = 0; i < _size; i++)
                if (position.y >= _bracketsY[i] && position.y < _bracketsY[i + 1])
                {
                    y = i;
                    return true;
                }

            x = int.MinValue;
            y = int.MinValue;
            return false;
        }
    }
}