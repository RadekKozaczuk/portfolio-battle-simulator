using System;
using Core.Enums;

namespace GameLogic.Interfaces
{
    internal interface IUnitController
    {
        internal Action<int> GetBehavior(Strategy strategy);
    }
}
