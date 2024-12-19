#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core.Services
{
    /// <summary>
    /// Architecture entry point. Injects configuration files.
    /// </summary>
    public static class ArchitectureService
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly List<Type> _usedConfigs = new();
        static int _lastFrameCount;
#endif

        static readonly ScriptableObject[] _configs = Resources.LoadAll<ScriptableObject>("Configs");

        /// <summary>
        /// Starts dependency injection and debug commands initialization.
        /// Tapping delay is used only when the game is build on mobile devices and tells how fast player has to tap to open the debug console.
        /// </summary>
        public static void Initialize(int signalCount, string[] signalNames, Queue<object>?[] signalQueues, float tappingDelay = 0.5f)
        {
#if UNITY_EDITOR
            CodeValidationService.Validate();
#endif

            _ = new SignalService(signalCount, signalNames, signalQueues, typeof(ReactAttribute)); // instance disposed as the object is a singleton
            DependencyInjectionService<ScriptableObject>.Inject(FindConfig);
        }

        /// <summary>
        /// This method should only be called by autogenerated class called Signals.g.cs.
        /// </summary>
        public static void Intercept(int signalId, string methodName, params object[] args) => SignalDispatch.Intercept(signalId, methodName, args);

        /// <summary>
        /// Should be invoked at least one frame later than <see cref="Initialize"/>.
        /// </summary>
        public static void InvokeInitialization() => DependencyInjectionService<ScriptableObject>.InvokeInitialization();

        // todo: are signals deterministic?
        // todo: what if new signals are sent while Processor executes already scheduled signals?

        /// <summary>
        /// Process and execute all signals sent. Should be called only once per frame.
        /// </summary>
        public static void ExecuteSentSignals()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Assert.IsFalse(_lastFrameCount == Time.frameCount, "Signals should be executed once per frame.");
            _lastFrameCount = Time.frameCount;
#endif

            SignalService.ExecuteSentSignals();
        }

        static ScriptableObject? FindConfig(Type type)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_usedConfigs.Contains(type))
                _usedConfigs.Add(type);
#endif
            return _configs.FirstOrDefault(c => c.GetType() == type);
        }
    }
}