#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core;
using GameLogic.Interfaces;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace GameLogic.Controllers
{
    /// <summary>
    /// Job of this controller is to speed up search of elements in 2D space.
    /// The algorithm divides the space into equal brackets.
    /// Read operations are parallel-friendly.
    /// <see cref="SortElements"/> is single-threaded.
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
            internal bool Dead
            {
                get => _dead;
                set
                {
                    Assert.IsFalse(_dead == value, "Killing already dead unit is not allowed");
                    _dead = value;
                }
            } // 1 bit
            bool _dead;

            // consider data packing
            internal readonly int UnitId; // 16 bits
            internal readonly int ArmyId; // 8 bits
            internal int QuadrantIdX; // 8 bits
            internal int QuadrantIdY; // 8 bits
            internal float2 Position;

            // todo: unfortunately it has to present in every struct as we want the controller to have many instances
            internal readonly int QuadrantSize; // should be outside I think

            internal Unit(int unitId, int armyId, int quadrantIdX, int quadrantIdY, float2 position, int quadrantSize)
            {
                UnitId = unitId;
                ArmyId = armyId;
                QuadrantIdX = quadrantIdX;
                QuadrantIdY = quadrantIdY;
                Position = position;
                _dead = false;
                QuadrantSize = quadrantSize;
            }
        }

        /// <summary>
        /// Units that are inside the boundaries.
        /// Size is equal the elementCount.
        /// Elements at the end are noise.
        /// </summary>
        readonly Unit[] _inside;

        readonly float[] _bracketsX;
        readonly float[] _bracketsY;
        readonly int[] _quadrantStarts;
        readonly int[] _quadrantLengths;
        readonly Func<int, int, int, Memory<Unit>>[] _getHorizontalAreas;
        readonly Func<int, int, int, List<Memory<Unit>>>[] _getVerticalAreas;

        /// <summary>
        /// Number of elements in the <see cref="_inside"/> table.
        /// Everything on the right of that is a noise.
        /// </summary>
        int _aliveCount;

        readonly int _size;
        readonly UnitComparer _comparer = new();
        int _unitDiedThisFrame;

        readonly ObjectPool<List<Memory<Unit>>> _memoryPool;
        readonly ObjectPool<List<int>> _listPool;

        // todo: for DI, for now
        [Preserve]
        SpacePartitioningController()
        {
            _inside = null!;
            _bracketsX = null!;
            _bracketsY = null!;

            _quadrantStarts = null!;
            _quadrantLengths = null!;

            _getHorizontalAreas = null!;
            _getVerticalAreas = null!;

            _memoryPool = null!;
            _listPool = null!;
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

            _getHorizontalAreas = new Func<int, int, int, Memory<Unit>>[] {GetAreaUp, GetAreaDown};
            _getVerticalAreas = new Func<int, int, int, List<Memory<Unit>>>[] {GetAreaLeft, GetAreaRight};

            _memoryPool = new ObjectPool<List<Memory<Unit>>>(
                () => new List<Memory<Unit>>(4),
                list => list.Clear());

            _listPool = new ObjectPool<List<int>>(() => new List<int>(), list => list.Clear());
        }

#region Internal Methods
        /// <summary>
        /// Adding a new element means to simply set its quadrantId
        /// </summary>
        void ISpacePartitioningController.AddUnit(int unitId, int armyId, float2 position)
        {
            PositionToQuadrant(position, out int x, out int y);

            _inside[unitId] = new Unit(unitId, armyId, x, y, position, _size);
            _aliveCount++;
        }

        void ISpacePartitioningController.KillUnit(int unitId)
        {
            // it goes over all elements but in reality dead units are moved to the right it never does full run
            for (int i = 0; i < _inside.Length; i++)
                if (_inside[i].UnitId == unitId)
                {
                    _inside[i].Dead = true; // will be moved to the end of the inside table
                    _unitDiedThisFrame++;
                    return;
                }

            throw new Exception($"Could not find the unit to kill (id: {unitId}). Please ensure the unit was added in the first place.");
        }

        int ISpacePartitioningController.FindNearestEnemy(float2 position, int excludeArmyId)
        {
            // calculate your quadrant
            PositionToQuadrant(position, out int x, out int y);

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

            // did not find or next quadrant is closer
            float distToQuadrant = MinDistanceToNextQuadrant(position, x, y, 1);
            bool finished = nearestId != int.MinValue && minDistance < distToQuadrant;

            if (finished)
                return nearestId;

            // extended search
            for (int searchRadius = 1; searchRadius < _size; searchRadius++)
            {
                // horizontal search
                int area = 0;
                for (; area < 2; area++)
                {
                    Span<Unit> units = _getHorizontalAreas[area](x, y, searchRadius).Span;

                    for (int i = 0; i < units.Length; i++)
                    {
                        ref Unit unit = ref units[i];

                        // ignore allies
                        if (unit.ArmyId == excludeArmyId)
                            continue;

                        // check distance
                        float distance = math.distance(position, unit.Position);
                        if (distance >= minDistance)
                            continue;

                        minDistance = distance;
                        nearestId = unit.UnitId;
                    }
                }

                // vertical search
                for (area = 0; area < 2; area++)
                {
                    List<Memory<Unit>> memories = _getVerticalAreas[area](x, y, searchRadius);
                    foreach (Memory<Unit> memory in memories)
                    {
                        Span<Unit> units = memory.Span;

                        for (int i = 0; i < units.Length; i++)
                        {
                            ref Unit unit = ref units[i];

                            // ignore allies
                            if (unit.ArmyId == excludeArmyId)
                                continue;

                            // check distance
                            float distance = math.distance(position, unit.Position);
                            if (distance >= minDistance)
                                continue;

                            minDistance = distance;
                            nearestId = unit.UnitId;
                        }
                    }

                    _memoryPool.Release(memories);
                }

                distToQuadrant = MinDistanceToNextQuadrant(position, x, y, searchRadius + 1);
                finished = nearestId != int.MinValue && minDistance < distToQuadrant;

                if (finished)
                    return nearestId;
            }

            throw new Exception("Should not be called if there is no enemies present.");
        }

        /// <summary>
        /// Returns a list of id that are within the given distance.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="exceptUnitId"></param>
        /// <param name="maxDistance">Inclusive</param>
        /// <returns></returns>
        List<int> ISpacePartitioningController.FindAllNearbyUnits(float2 position, int exceptUnitId, float maxDistance)
        {
            List<int> nearestUnits = _listPool.Get();

            // calculate your quadrant
            PositionToQuadrant(position, out int x, out int y);

            // if no - extend search
            // go in bigger and bigger circles    
            int quadrant = ToQuadrant(x, y);

            // search within your quadrant
            Span<Unit> span = _inside.AsSpan(_quadrantStarts[quadrant], _quadrantLengths[quadrant]);
            for (int i = 0; i < span.Length; i++)
            {
                ref Unit unit = ref span[i];

                // ignore yourself
                if (unit.UnitId == exceptUnitId)
                    continue;

                float distance = math.distance(position, unit.Position);
                if (distance <= maxDistance)
                    nearestUnits.Add(unit.UnitId);
            }

            // elements can still be in surrounding quadrants
            float distToQuadrant = MinDistanceToNextQuadrant(position, x, y, 1);
            bool finished = distToQuadrant > maxDistance;

            if (finished)
                return nearestUnits;

            // extended search
            for (int searchRadius = 1; searchRadius < _size; searchRadius++)
            {
                // horizontal search
                int area = 0;
                for (; area < 2; area++)
                {
                    Span<Unit> units = _getHorizontalAreas[area](x, y, searchRadius).Span;

                    for (int i = 0; i < units.Length; i++)
                    {
                        ref Unit unit = ref units[i];

                        // ignore yourself
                        // in edge cases extended search will return an overlapping area to previous search except wider
                        if (unit.UnitId == exceptUnitId)
                            continue;

                        // check distance
                        float distance = math.distance(position, unit.Position);
                        if (distance >= maxDistance)
                            continue;

                        if (!nearestUnits.Contains(unit.UnitId))
                            nearestUnits.Add(unit.UnitId);
                    }
                }

                // vertical search
                for (area = 0; area < 2; area++)
                {
                    List<Memory<Unit>> memories = _getVerticalAreas[area](x, y, searchRadius);
                    foreach (Memory<Unit> memory in memories)
                    {
                        Span<Unit> units = memory.Span;

                        for (int i = 0; i < units.Length; i++)
                        {
                            ref Unit unit = ref units[i];

                            // ignore yourself
                            // in edge cases extended search will return an overlapping area to previous search except wider
                            if (unit.UnitId == exceptUnitId)
                                continue;

                            // check distance
                            float distance = math.distance(position, unit.Position);
                            if (distance >= maxDistance)
                                continue;

                            if (!nearestUnits.Contains(unit.UnitId))
                                nearestUnits.Add(unit.UnitId);
                        }
                    }

                    _memoryPool.Release(memories);
                }

                // elements can still be in surrounding quadrants
                distToQuadrant = MinDistanceToNextQuadrant(position, x, y, searchRadius + 1);
                finished = distToQuadrant > maxDistance;

                if (finished)
                    return nearestUnits;
            }

            return nearestUnits;
        }

        void ISpacePartitioningController.Release(List<int> list) => _listPool.Release(list);

        void ISpacePartitioningController.UpdateUnits()
        {
            for (int i = 0; i < _aliveCount; i++)
            {
                int id = _inside[i].UnitId;
                float2 pos = CoreData.UnitCurrPos[id];
                PositionToQuadrant(pos, out int x, out int y);

                _inside[i].Position = pos;
                _inside[i].QuadrantIdX = x;
                _inside[i].QuadrantIdY = y;
            }

            SortElements();
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
            int lastChecked = 0;
            while (_unitDiedThisFrame > 0)
                // go from start to _elementCount and keep swapping elements until you reach the end
                for (int i = lastChecked; i < _aliveCount; i++)
                    if (_inside[i].Dead)
                    {
                        Swap(i, --_aliveCount);
                        _unitDiedThisFrame--;
                        lastChecked++;
                        break;
                    }

            Array.Sort(_inside, 0, _aliveCount, _comparer);

            int currQuadrant = 0;
            int currIndex = 0; // in _inside array
            for (int i = 0; i < _size * _size; i++)
            {
                _quadrantStarts[currQuadrant] = currIndex;
                _quadrantLengths[currQuadrant] = 0;

                // iterate over units until you reach next quadrant
                for (; currIndex < _aliveCount; currIndex++)
                {
                    ref Unit unit = ref _inside[currIndex];
                    int quadrant = ToQuadrant(unit.QuadrantIdX, unit.QuadrantIdY);

                    // u belong until you don't
                    if (currQuadrant == quadrant)
                    {
                        _quadrantLengths[currQuadrant]++;

                        // last unit
                        if (currIndex == _aliveCount - 1)
                            currQuadrant++;
                    }
                    else
                    {
                        currQuadrant++;
                        break;
                    }
                }
            }
        }
#endregion

#region Private Methods
        /// <summary>
        /// Calculates the distance to the nearest quadrant.
        /// </summary>
        /// <param name="position">Our position</param>
        /// <param name="x">Quadrant we are in (x coefficient)</param>
        /// <param name="y">Quadrant we are in (y coefficient)</param>
        /// <param name="searchRadius"></param>
        /// <returns></returns>
        float MinDistanceToNextQuadrant(float2 position, int x, int y, int searchRadius)
        {
            float minDistance = float.MaxValue;
            float distance;

            // ignore searches that at the edge
            if (x - searchRadius >= 0)
            {
                distance = math.abs(position.x - _bracketsX[x - searchRadius + 1]);
                if (distance < minDistance)
                    minDistance = distance;
            }

            if (x + searchRadius < _size)
            {
                distance = math.abs(position.x - _bracketsX[x + searchRadius]);
                if (distance < minDistance)
                    minDistance = distance;
            }

            if (y - searchRadius >= 0)
            {
                distance = math.abs(position.y - _bracketsY[y - searchRadius + 1]);
                if (distance < minDistance)
                    minDistance = distance;
            }

            if (y + searchRadius < _size)
            {
                distance = math.abs(position.y - _bracketsY[y + searchRadius]);
                if (distance < minDistance)
                    minDistance = distance;
            }

            return minDistance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Swap(int a, int b) => (_inside[a], _inside[b]) = (_inside[b], _inside[a]);

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

            throw new Exception("Unable to map position to quadrant.");
        }

        /// <summary>
        /// The list count will always be 1. It returns a list only for compatibility reasons.
        /// </summary>
        // todo: Span would be better but is Memory because writing unit tests for Spans is extremely difficult
        Memory<Unit> GetAreaUp(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            Assert.IsTrue(searchRadius < _size, "Search radius must be smaller than the size of the quadrant matrix. "
                                                + "Higher radiuses makes no sense because the borderline quadrants are infinite.");

            int y = searchCenterY + searchRadius;
            if (y >= _size)
                y = _size - 1;

            GetXStartLength(searchCenterX, y, searchRadius, out int start, out int length);

            return _inside.AsMemory(start, length);
        }

        /// <summary>
        /// The list count will always be 1. It returns a list only for compatibility reasons.
        /// </summary>
        // todo: Span would be better but is Memory because writing unit tests for Spans is extremely difficult
        Memory<Unit> GetAreaDown(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            Assert.IsTrue(searchRadius < _size, "Search radius must be smaller than the size of the quadrant matrix. "
                                                + "Higher radiuses makes no sense because the borderline quadrants are infinite.");

            int y = searchCenterY - searchRadius;
            if (y < 0)
                y = 0;

            GetXStartLength(searchCenterX, y, searchRadius, out int start, out int length);

            return _inside.AsMemory(start, length);
        }

        void GetXStartLength(int searchCenterX, int y, int searchRadius, out int start, out int length)
        {
            int xMin = searchCenterX - searchRadius;
            if (xMin < 0)
                xMin = 0;

            int xMax = searchCenterX + searchRadius;
            if (xMax >= _size)
                xMax = _size - 1;

            int firstQuadrant = ToQuadrant(xMin, y);
            int lastQuadrant = ToQuadrant(xMax, y);

            start = _quadrantStarts[firstQuadrant];
            length = 0;
            for (int i = firstQuadrant; i <= lastQuadrant; i++)
                length += _quadrantLengths[i];
        }

        // todo: Span would be better but is Memory because writing unit tests for Spans is extremely difficult
        List<Memory<Unit>> GetAreaLeft(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            Assert.IsTrue(searchRadius < _size, "Search radius must be smaller than the size of the quadrant matrix. "
                                                + "Higher radiuses makes no sense because the borderline quadrants are infinite.");

            int x = searchCenterX - searchRadius;
            if (x < 0)
                x = 0;

            GetYStartLength(x, searchCenterY, searchRadius, out List<int> start, out List<int> length);

            List<Memory<Unit>> list = _memoryPool.Get();

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < start.Count; i++)
                list.Add(_inside.AsMemory(start[i], length[i]));

            _listPool.Release(start);
            _listPool.Release(length);

            return list;
        }

        // todo: Span would be better but is Memory because writing unit tests for Spans is extremely difficult
        List<Memory<Unit>> GetAreaRight(int searchCenterX, int searchCenterY, int searchRadius)
        {
            Assert.IsTrue(searchRadius > 0, "Search radius must be greater than 0. "
                                            + "For radius = 0 there is only one quadrant therefore returning a subarea makes no sense.");

            Assert.IsTrue(searchRadius < _size, "Search radius must be smaller than the size of the quadrant matrix. "
                                                + "Higher radiuses makes no sense because the borderline quadrants are infinite.");

            int x = searchCenterX + searchRadius;
            if (x >= _size)
                x = _size - 1;

            GetYStartLength(x, searchCenterY, searchRadius, out List<int> start, out List<int> length);

            List<Memory<Unit>> list = _memoryPool.Get();

            // ReSharper disable once LoopCanBeConvertedToQuery
            for (int i = 0; i < start.Count; i++)
                list.Add(_inside.AsMemory(start[i], length[i]));

            _listPool.Release(start);
            _listPool.Release(length);

            return list;
        }

        /// <summary>
        /// List are pooled and should be released.
        /// </summary>
        void GetYStartLength(int x, int searchCenterY, int searchRadius, out List<int> starts, out List<int> lengths)
        {
            int yMin = searchCenterY - searchRadius + 1;
            if (yMin < 0)
                yMin = 0;

            int yMax = searchCenterY + searchRadius - 1;
            if (yMax >= _size)
                yMax = _size - 1;

            int length = yMax + 1 - yMin;
            starts = _listPool.Get();
            lengths = _listPool.Get();

            for (int i = 0; i < length; i++)
            {
                int quadrant = ToQuadrant(x, yMin + i);

                // ignore empty quadrant
                if (_quadrantLengths[quadrant] == 0)
                    continue;

                starts.Add(_quadrantStarts[quadrant]);
                lengths.Add(_quadrantLengths[quadrant]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int ToQuadrant(int x, int y) => x + y * _size;
#endregion
    }
}