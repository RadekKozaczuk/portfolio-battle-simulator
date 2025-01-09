using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameLogic.Interfaces;
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
    class SpacePartitioningController : ISpacePartitioningController
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

        readonly int[] _quadrantStarts;
        readonly int[] _quadrantLengths;

        bool _unitDiedThisFrame;

        // for DI, for now
        [Preserve]
        SpacePartitioningController()
        {
            _inside = null!;
            _bracketsX = null!;
            _bracketsY = null!;

            _quadrantStarts = null!;
            _quadrantLengths = null!;
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
                if (i == 0)
                    _bracketsX[0] = float.MinValue;
                else if (i == size)
                    _bracketsX[size] = float.MaxValue;
                else
                    _bracketsX[i] = xMin + i * bracketWidthX;

            float bracketWidthY = (yMax - yMin) / size;
            _bracketsY = new float[size + 1];
            for (int i = 0; i <= size; i++)
                if (i == 0)
                    _bracketsY[0] = float.MinValue;
                else if (i == size)
                    _bracketsY[size] = float.MaxValue;
                else
                    _bracketsY[i] = yMin + i * bracketWidthY;

            _inside = new Unit[elementCount];

            _quadrantStarts = new int[_size * _size];
            _quadrantLengths = new int[_size * _size];
        }

        /// <summary>
        /// Adding a new element means to simply set its quadrantId
        /// </summary>
        public void AddUnit(int unitId, int armyId, float2 position)
        {
            PositionToQuadrant(position, out int x, out int y);

            _inside[unitId] = new Unit(unitId, armyId, x, y, position, _size);
            _insideCount++;
        }

        public void UpdateUnit(int unitId, float2 position)
        {
            PositionToQuadrant(position, out int x, out int y);

            for (int i = 0; i < _insideCount; i++)
                if (_inside[i].UnitId == unitId)
                {
                    // should stay in this table
                    _inside[unitId].Position = position;
                    _inside[unitId].QuadrantIdX = x;
                    _inside[unitId].QuadrantIdY = y;
                    return;
                }
        }

        public void KillUnit(int unitId)
        {
            for (int i = 0; i < _insideCount; i++)
                if (_inside[i].UnitId == unitId)
                {
                    _inside[unitId].DeadOrOutside = true; // will be moved to the end of the inside table
                    _insideCount--;
                    _unitDiedThisFrame = true;
                    return;
                }
        }

        public int FindNearestEnemy(float2 position, int excludeArmyId)
        {
            // calculate your quadrant

            PositionToQuadrant(position, out int x, out int y);

            // can I find the nearest?

            float minDistance = float.MaxValue;
            int nearestId = int.MinValue;

            // if no - extend search
            // go in bigger and bigger circles    
            int quadrant = ToQuadrant(x, y);

            // search within your quadrant
            Span<Unit> span = _inside.AsSpan(_quadrantStarts[quadrant], _quadrantLengths[quadrant]);
            for (int i = 0; i < span.Length; i++)
            {
                ref Unit unit = ref span[i];

                // ignore allies
                if (unit.ArmyId == excludeArmyId)
                    continue;

                float distance = math.distance(position, unit.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestId = unit.UnitId;
                }
            }

            if (nearestId == int.MinValue)
                return nearestId;

            // todo: potential exception - distance to nearest is higher that the distance to the nearest quadrant 
            // todo: extend search
            return 0;
        }

        public List<int> FindAllAllies(float2 position, int armyId, float maxDistance)
        {
            PositionToQuadrant(position, out int x, out int y);

            return new List<int>();
        }

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
            if (_unitDiedThisFrame)
            {
                // go from start to _elementCount and keep swapping elements until you reach the end
                for (int i = 0; i < _insideCount; i++)
                    if (_inside[i].DeadOrOutside)
                        Swap(_insideCount, i);

                _unitDiedThisFrame = false;
            }

            Array.Sort(_inside, 0, _insideCount, _comparer);

            int currQuadrant = 0;
            int currIndex = 0; // in _inside array
            for (int i = 0; i < _size * _size; i++)
            {
                _quadrantStarts[currQuadrant] = currIndex;

                // iterate over units until you reach next quadrant
                for (; currIndex < _insideCount; currIndex++)
                {
                    ref Unit unit = ref _inside[currIndex];
                    int quadrant = ToQuadrant(unit.QuadrantIdX, unit.QuadrantIdY);

                    // u belong until you don't
                    if (currQuadrant == quadrant)
                    {
                        _quadrantLengths[currQuadrant]++;
                    }
                    else
                    {
                        currQuadrant++;
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Swap(int a, int b) => (_inside[b], _inside[a]) = (_inside[a], _inside[b]);

        // todo: maybe will be reused if system is going to use list as a return structure
        //public void ReturnList(List<int> list) => _retValPool.Release(list);

        /// <summary>
        /// Returns the x and y of the quadrant this position belongs to.
        /// If no quadrant could be found (the position is out of bounds) the x and y will be equal to <see cref="int.MinValue"/>.
        /// </summary>
        void PositionToQuadrant(float2 position, out int x, out int y)
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

        step2:
            for (int i = 0; i < _size; i++)
                if (position.y >= _bracketsY[i] && position.y < _bracketsY[i + 1])
                {
                    y = i;
                    return;
                }

            throw new Exception("Unreachable code reached");
        }

        // todo: Memory instead og Span to make it testable as testing Span is extremely difficult
        Memory<Unit> GetAreaUp(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            int y = searchCenterY + searchRadius;
            if (y >= _size)
                y = _size - 1;

            GetXQuadrantRange(searchCenterX, y, searchRadius, out int firstQuadrant, out int lastQuadrant);

            int start = _quadrantStarts[firstQuadrant];
            int length = 0;
            for (int i = firstQuadrant; i <= lastQuadrant; i++)
                length += _quadrantLengths[i];

            return _inside.AsMemory(start, length);
        }

        // todo: Memory instead og Span to make it testable as testing Span is extremely difficult
        Memory<Unit> GetAreaDown(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            int y = searchCenterY - searchRadius;
            if (y < 0)
                y = 0;

            GetXQuadrantRange(searchCenterX, y, searchRadius, out int firstQuadrant, out int lastQuadrant);

            int start = _quadrantStarts[firstQuadrant];
            int length = 0;
            for (int i = firstQuadrant; i <= lastQuadrant; i++)
                length += _quadrantLengths[i];

            return _inside.AsMemory(start, length);
        }

        void GetXQuadrantRange(int searchCenterX, int y, int searchRadius, out int firstQuadrant, out int lastQuadrant)
        {
            int xMin = searchCenterX - searchRadius;
            if (xMin < 0)
                xMin = 0;

            int xMax = searchCenterX + searchRadius;
            if (xMax >= _size)
                xMax = _size - 1;

            firstQuadrant = ToQuadrant(xMin, y);
            lastQuadrant = ToQuadrant(xMax, y);
        }

        // todo: Memory instead og Span to make it testable as testing Span is extremely difficult
        Memory<Unit> GetAreaLeft(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            int x = searchCenterX - searchRadius;
            if (x < 0)
                x = 0;

            int yMin = searchCenterY - searchRadius + 1;
            if (yMin < 0)
                yMin = 0;

            int yMax = searchCenterY + searchRadius - 1;
            if (yMax >= _size)
                yMax = _size - 1;

            int firstQuadrant = ToQuadrant(x, yMin);

            int start = _quadrantStarts[firstQuadrant];
            int length = 0;
            for (int i = yMin; i < yMax; i++)
                length += _quadrantLengths[ToQuadrant(x, i)];

            return _inside.AsMemory(start, length);
        }

        // todo: Memory instead og Span to make it testable as testing Span is extremely difficult
        Memory<Unit> GetAreaRight(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            int x = searchCenterX + searchRadius;
            if (x >= _size)
                x = _size - 1;

            int yMin = searchCenterY - searchRadius + 1;
            if (yMin < 0)
                yMin = 0;

            int yMax = searchCenterY + searchRadius - 1;
            if (yMax >= _size)
                yMax = _size - 1;

            int firstQuadrant = ToQuadrant(x, yMin);

            int start = _quadrantStarts[firstQuadrant];
            int length = 0;
            for (int i = yMin; i < yMax; i++)
                length += _quadrantLengths[ToQuadrant(x, i)];

            return _inside.AsMemory(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int ToQuadrant(int x, int y) => x + y * _size;
    }
}