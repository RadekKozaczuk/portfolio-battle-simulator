#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;

namespace GameLogic.Interfaces
{
    internal interface IUnitController
    {
        internal Action<int, int, IBattleModel> GetBehavior(Strategy strategy);
    }
}
