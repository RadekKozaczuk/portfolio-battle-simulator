#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core.Services
{
    /// <summary>
    /// General purpose signal processing system.
    /// Signals are grouped per type.
    /// Groups are executed in alphabetical order.
    /// Within each group, signals are executed in a FIFO manner. <br/>
    /// If any new signals are sent during the signal execution phase, they are executed only when
    /// all the original signals had been processed. <br/>
    /// <i>Warning: Sending a signal without a corresponding React method will result in an error.</i>
    /// </summary>
    public class SignalService
    {
        readonly int _signalCount;
        static SignalService _instance;

        readonly string[] _signalNames;

        /// <summary>
        /// Key: signal name. <br/>
        /// Value: a delegate pointing at all corresponding reactive methods (decorated with <see cref="ReactAttribute"/>)<br/>
        ///     Important: a single delegate can be an aggregation of many methods. When invoked, the methods are called sequentially.
        /// </summary>
        readonly Delegate[] _reactMethods;

        /// <summary>
        /// SignalName is a unique signal identifier.
        /// Index is the index in the <see cref="_signalQueues"/> array that points at the corresponding queue.
        /// </summary>
        readonly (int queueIndex, Type[] types)[] _signalQueueLookup;

        /// <summary>
        /// This array stores signal runtime values for signals with parameters.
        /// Each signal can be represented by one or more queses depending on its parameters.
        /// For example, imagine we have two signals A and B. A is parameterless and B has two parameters of type int and string. <br/>
        /// null - Signal A (because parameterless signals are represented in this array as null)<br/>
        /// Queue(int) - Signal B <br/>
        /// Queue(string) - Signal B <br/><br/>
        /// First method/signal has no parameters and therefore is represented by one queue with boolean values in it. <br/>
        /// Second method/signal has two parameters (int and string) and is represented by two queues with respective value types.
        /// </summary>
        readonly Queue<object>?[] _signalQueues;

        /// <summary>
        /// Each time a new signal is called, its ID is added here.
        /// </summary>
        readonly Queue<int> _signals = new();

        readonly Type _reactAttribute;

        public SignalService(int signalCount, string[] signalNames, Queue<object>?[] signalQueues, Type reactAttribute)
        {
            _signalCount = signalCount;
            _reactMethods = new Delegate[signalCount];
            _signalQueueLookup = new (int index, Type[] types)[signalCount];
            _signalQueues = signalQueues;
            _signalNames = signalNames;

            _reactAttribute = reactAttribute;

            if (_instance != null)
                throw new Exception("There should be only once instance of SignalProcessor.");

            _instance = this;
        }

        public static void AddSignal(int id, object[] args) => _instance.AddSignal_Internal(id, args);

        public static void ExecuteSentSignals() => _instance.ExecuteSentSignals_Internal();

        /// <summary>
        /// Returns a delegate pointing at all corresponding reactive methods (decorated with <see cref="ReactAttribute"/>)<br/>
        /// Single delegate can be an aggregation of many methods. Such delegate, when invoked, calls the stored methods sequentially.
        /// </summary>
        public static Delegate GetReactMethods(int id) => _instance._reactMethods[id];

        internal static void BindSignals(MethodInfo[] signals) => _instance.BindSignals_Internal(signals);

        /// <summary>
        /// Adds instantiatable (controller, reference holder, or viewmodel) that has reactive methods (decorated with <see cref="ReactAttribute"/>).
        /// </summary>
        internal static void AddReactiveInstantiatable(object instance) =>
            _instance.BindReactiveMethods(instance.GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance));

        /// <summary>
        /// Adds a static class that has reactive methods (decorated with <see cref="ReactAttribute"/>).
        /// </summary>
        internal static void AddReactiveSystem(Type system) =>
            _instance.BindReactiveMethods(system.GetMethods(BindingFlags.NonPublic | BindingFlags.Static));

        /// <summary>
        /// Add new signal to the queue.
        /// All signals will be executed in FIFO (first-in-first-out) order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        void AddSignal_Internal(int id, IReadOnlyList<object> args)
        {
            _signals.Enqueue(id);

            if (args.Count <= 0)
                return;

            (int queueIndex, Type[] types) = _signalQueueLookup[id];

            for (int i = 0; i < types.Length; i++)
                _signalQueues[queueIndex + i]!.Enqueue(args[i]);
        }

        /// <summary>
        /// Process and execute all signals sent. Should be called only once per frame.
        /// </summary>
        void ExecuteSentSignals_Internal()
        {
            // go through signals one by one
            while (_signals.Count > 0)
            {
                int id = _signals.Dequeue();

                (int queueIndex, Type[] types) = _signalQueueLookup[id];
                Queue<object>? firstQueue = _signalQueues[queueIndex];

                if (firstQueue == null) // signal is parameterless
                {
                    _reactMethods[id].DynamicInvoke();
                    continue;
                }

                // in case of signals with many parameters iterate over all corresponding queues to gather arguments
                object value = firstQueue.Dequeue();
                object[] args = new object[types.Length];
                args[0] = Convert.ChangeType(value, types[0]);

                for (int i = 1; i < types.Length; i++)
                {
                    value = _signalQueues[queueIndex + i]!.Dequeue();
                    args[i] = Convert.ChangeType(value, types[i]);
                }

                _reactMethods[id].DynamicInvoke(args);
            }
        }

        void BindSignals_Internal(IReadOnlyList<MethodInfo> signals)
        {
            int queueIndex = 0;

            for (int i = 0; i < signals.Count; i++)
            {
                MethodInfo method = signals[i];
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    _signalQueueLookup[i] = (queueIndex++, Array.Empty<Type>());
                    continue;
                }

                var types = new Type[parameters.Length];
                for (int j = 0; j < parameters.Length; j++)
                    types[j] = parameters[j].ParameterType;

                _signalQueueLookup[i] = (queueIndex, types);
                queueIndex += parameters.Length;
            }
        }

        void BindReactiveMethods(IReadOnlyList<MethodInfo> methods)
        {
            foreach (MethodInfo method in methods)
            {
                if (Attribute.GetCustomAttributes(method, _reactAttribute).Length == 0)
                    continue;

                Assert.IsTrue(method.IsStatic, $"React method {method.Name} is not static. React methods must be static.");
                Assert.IsTrue(method.IsPrivate, $"React method {method.Name} is not private. React methods must be private.");

                ParameterInfo[] parameters = method.GetParameters();
                var types = new Type[parameters.Length];
                for (int j = 0; j < parameters.Length; j++)
                    types[j] = parameters[j].ParameterType;

                // use name to retrieve metadata
                // [React] methods are always named: "On" + SignalName
                string name = method.Name[2..];
                Type delegateType = Expression.GetActionType(types);

                int id = int.MinValue;
                for (int j = 0; j < _signalCount; j++)
                    if (_signalNames[j] == name)
                    {
                        id = j;
                        break;
                    }

                Assert.IsFalse(id == int.MinValue, "Could not find the signal.");

                var del = Delegate.CreateDelegate(delegateType, method);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                _reactMethods[id] = _reactMethods[id] == null ? del : Delegate.Combine(_reactMethods[id], del);
            }
        }
    }
}