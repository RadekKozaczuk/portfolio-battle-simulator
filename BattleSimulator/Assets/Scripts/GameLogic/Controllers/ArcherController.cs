#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using Core.Enums;
using GameLogic.Interfaces;
using UnityEngine.Scripting;
#if DEVELOPMENT_BUILD
using UnityEngine.Assertions;
#endif

namespace GameLogic.Controllers
{
    class ArcherController : IUnitController
    {
        readonly Action<int>[] _strategies = {Basic, Defensive};

        [Preserve]
        ArcherController() { }

        Action<int> IUnitController.GetBehavior(Strategy strategy) => _strategies[(int)strategy];

        static void Basic(int unitId) { }

        static void Defensive(int unitId) { }
    }
}
