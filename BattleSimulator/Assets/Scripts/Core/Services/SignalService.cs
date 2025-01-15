#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
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
    public static class SignalService
    {
        const int SignalCount = SignalProcessorPrecalculatedArrays.SignalCount;
        static readonly string[] _signalNames = SignalProcessorPrecalculatedArrays.SignalNames;

        // todo: maybe should be array or array as we should now the size ahead of time
        /// <summary>
        /// Key: signal name. <br/>
        /// Value: a delegate pointing at all corresponding reactive methods (decorated with <see cref="ReactAttribute"/>)<br/>
        ///     Important: a single delegate can be an aggregation of many methods. When invoked, the methods are called sequentially.
        /// </summary>
        static readonly List<Delegate>[] _reactMethods = new List<Delegate>[SignalProcessorPrecalculatedArrays.SignalCount];

        /// <summary>
        /// SignalName is a unique signal identifier.
        /// Index is the index in the <see cref="_signalQueues"/> array that points at the corresponding queue.
        /// </summary>
        static (int queueIndex, Type[] types)[] _signalQueueLookup;

        /// <summary>
        /// This array stores signal runtime values for signals with parameters.
        /// Each signal is represented by one or more queues depending on its parameters. For example, if you have a signal parameterless signal A
        /// and signal B that has two parameters of type in and string then the queue will look like follows:<br/>
        /// [0] = null - Signal A<br/>
        /// [1] = Queue(int) - Signal B<br/>
        /// [2] = Queue(string) - Signal B<br/>
        /// First method/signal has no parameters and is represented by a null. Second method/signal has two parameters
        /// (<see cref="int"/> and <see cref="string"/>) therefore is represented by two queues with respective value types.
        /// </summary>
        static readonly Queue<object>[] _signalQueues = SignalProcessorPrecalculatedArrays.SignalQueues!; // todo: get rid of nullability

        /// <summary>
        /// Each time a new signal is called, its ID is added here.
        /// </summary>
        static readonly Queue<int> _signals = new();

        static Type _reactAttribute;

        public static void Initialize(Type reactAttribute)
        {
            _signalQueueLookup = new (int index, Type[] types)[SignalCount];

            for (int i = 0; i < SignalProcessorPrecalculatedArrays.SignalCount; i++)
                _reactMethods[i] = new List<Delegate>();

            _reactAttribute = reactAttribute;
        }

        /// <summary>
        /// Returns a delegate pointing at all corresponding reactive methods (decorated with <see cref="ReactAttribute"/>)<br/>
        /// Single delegate can be an aggregation of many methods. Such delegate, when invoked, calls the stored methods sequentially.
        /// </summary>
        public static List<Delegate> GetReactMethods(int id) => _reactMethods[id];

        internal static void BindSignals(MethodInfo[] signals) => BindSignals_Internal(signals);

        /// <summary>
        /// Adds instantiatable (controller, reference holder, or viewmodel) that has reactive methods (decorated with <see cref="ReactAttribute"/>).
        /// </summary>
        internal static void AddReactiveInstantiatable(object instance)
        {
            MethodInfo[] methods = instance.GetType().GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
            BindReactiveMethods(methods);
        }

        /// <summary>
        /// Adds a static class that has reactive methods (decorated with <see cref="ReactAttribute"/>).
        /// </summary>
        internal static void AddReactiveSystem(Type system) =>
            BindReactiveMethods(system.GetMethods(BindingFlags.NonPublic | BindingFlags.Static));

#region AddSignal_Internal
        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id) => _signals.Enqueue(id);

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2, object arg3)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
            _signalQueues[++index].Enqueue(arg3);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
            _signalQueues[++index].Enqueue(arg3);
            _signalQueues[++index].Enqueue(arg4);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
            _signalQueues[++index].Enqueue(arg3);
            _signalQueues[++index].Enqueue(arg4);
            _signalQueues[++index].Enqueue(arg5);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
            _signalQueues[++index].Enqueue(arg3);
            _signalQueues[++index].Enqueue(arg4);
            _signalQueues[++index].Enqueue(arg5);
            _signalQueues[++index].Enqueue(arg6);
        }

        /// <summary>
        /// Adds a new signal to the queue. Signals will be executed in First-In-First-Out order when <see cref="ExecuteSentSignals"/> is called.
        /// </summary>
        internal static void AddSignal(int id, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            _signals.Enqueue(id);
            int index = _signalQueueLookup[id].queueIndex;

            _signalQueues[index].Enqueue(arg0);
            _signalQueues[++index].Enqueue(arg1);
            _signalQueues[++index].Enqueue(arg2);
            _signalQueues[++index].Enqueue(arg3);
            _signalQueues[++index].Enqueue(arg4);
            _signalQueues[++index].Enqueue(arg5);
            _signalQueues[++index].Enqueue(arg6);
            _signalQueues[++index].Enqueue(arg7);
        }
#endregion

        /// <summary>
        /// Process and execute all signals sent. Should be called only once per frame.
        /// </summary>
        internal static void ExecuteSentSignals()
        {
            // go through signals one by one
            while (_signals.Count > 0)
            {
                int id = _signals.Dequeue();

                (int queueIndex, Type[] types) = _signalQueueLookup[id];
                Queue<object> firstQueue = _signalQueues[queueIndex];

                if (types.Length == 0) // signal is parameterless
                {
                    _reactMethods[id][0].DynamicInvoke();
                    continue;
                }

                // in case of signals with many parameters iterate over all corresponding queues to gather arguments
                object value = firstQueue.Dequeue();
                object[] args = new object[types.Length];
                args[0] = Convert.ChangeType(value, types[0]);

                for (int i = 1; i < types.Length; i++)
                {
                    value = _signalQueues[queueIndex + i].Dequeue();
                    args[i] = Convert.ChangeType(value, types[i]);
                }

                for (int i = 0; i < _reactMethods[id].Count; i++)
                    _reactMethods[id][i].DynamicInvoke(args);
            }
        }

        static void BindSignals_Internal(IReadOnlyList<MethodInfo> signals)
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

        static void BindReactiveMethods(IReadOnlyList<MethodInfo> methods)
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
                for (int j = 0; j < SignalCount; j++)
                    if (_signalNames[j] == name)
                    {
                        id = j;
                        break;
                    }

                Assert.IsFalse(id == int.MinValue, "Could not find the signal.");

                _reactMethods[id].Add(Delegate.CreateDelegate(delegateType, method));
            }
        }
    }
}