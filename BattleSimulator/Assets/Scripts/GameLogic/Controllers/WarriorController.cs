using System;
using Core.Enums;
using GameLogic.Interfaces;
using UnityEngine.Scripting;
#if DEVELOPMENT_BUILD
using UnityEngine.Assertions;
#endif

namespace GameLogic.Controllers
{
    class WarriorController : IUnitController
    {
        readonly Action<int>[] _strategies = {Basic, Defensive};

        [Preserve]
        WarriorController() { }

        Action<int> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        static void Basic(int unitId) { }

        static void Defensive(int unitId) { }
    }
}
