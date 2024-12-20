#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GameLogic.Models
{
    internal readonly struct MemoryLayoutModel
    {
        internal readonly int AllyIndex, AllyLength, EnemyIndex1, EnemyLength1, EnemyIndex2, EnemyLength2;

        /// <summary>
        /// For armies on the edge of the memory block.
        /// </summary>
        internal MemoryLayoutModel(int allyIndex, int allyLength, int enemyIndex1, int enemyLength1)
        {
            // todo: add meaningful assertions
            //Assert.IsTrue(allyIndex <= allyLength);

            AllyIndex = allyIndex;
            AllyLength = allyLength;
            EnemyIndex1 = enemyIndex1;
            EnemyLength1 = enemyLength1;
            EnemyIndex2 = int.MinValue;
            EnemyLength2 = int.MinValue;
        }

        /// <summary>
        /// For armies in the middle of the memory block.
        /// </summary>
        internal MemoryLayoutModel(int allyIndex, int allyLength, int enemyIndex1, int enemyLength1, int enemyIndex2, int enemyLength2)
        {
            // todo: add meaningful assertions
            //Assert.IsTrue(allyIndex <= allyLength);
            //Assert.IsTrue(allyLength <= enemyIndex1);
            //Assert.IsTrue(enemyIndex1 <= enemyLength1);

            AllyIndex = allyIndex;
            AllyLength = allyLength;
            EnemyIndex1 = enemyIndex1;
            EnemyLength1 = enemyLength1;
            EnemyIndex2 = enemyIndex2;
            EnemyLength2 = enemyLength2;
        }
    }
}