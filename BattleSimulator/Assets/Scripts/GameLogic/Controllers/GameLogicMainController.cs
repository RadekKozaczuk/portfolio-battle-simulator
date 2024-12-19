#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System.Threading.Tasks;
using Core.Interfaces;
using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace GameLogic.Controllers
{
    /// <summary>
    /// Main controller serves 3 distinct roles:<br/>
    /// 1) It allows you to control signal execution order. For example, instead of reacting on many signals in many different controllers,
    /// you can have one signal, react on it here, and call necessary controllers/systems in the order of your liking.<br/>
    /// 2) Serves as a 'default' controller. When you don't know where to put some logic or the logic is too small for its own controller
    /// you can put it into the main controller.<br/>
    /// 3) Reduces the size of the viewmodel. We could move all (late/fixed)update calls to viewmodel but over time it would lead to viewmodel
    /// being too long to comprehend. We also do not want to react on signals in viewmodels for the exact same reason.<br/>
    /// For better code readability all controllers meant to interact with this controller should implement
    /// <see cref="ICustomLateUpdate" /> interface.<br/>
    /// </summary>
    [UsedImplicitly]
    class GameLogicMainController : ICustomFixedUpdate, ICustomUpdate, ICustomLateUpdate
    {
        [Preserve]
        GameLogicMainController() { }

        public void CustomFixedUpdate() { }

        public void CustomUpdate()
        {
            const int NumberOfUnitSpans = 6; // todo: probably number of armies multiplied by number of unit types
            const int NumberOfProjectileSpans = 3; // todo: most likely equal to the number of projectile throwing units * the number of armies

            // we iterate as many times as there is memory spans
            // each unit type is updated in o
            Parallel.For(0, NumberOfUnitSpans + NumberOfProjectileSpans, spanId =>
            {

            });
        }

        public void CustomLateUpdate() { }
    }
}