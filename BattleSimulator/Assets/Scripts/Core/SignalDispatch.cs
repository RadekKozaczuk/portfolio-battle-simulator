#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Config;
using Core.Services;
using UnityEngine;
using UnityEngine.Assertions;

namespace Core
{
    static class SignalDispatch
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly Dictionary<string, int> _lookup = new ()
        {
            ["Boot"] = 0,
            ["Core"] = 1,
            ["GameLogic"] = 2,
            ["Presentation"] = 3,
            ["UI"] = 4
        };

        /// <summary>
        /// True - communicates through signals.
        /// False - direct communication (through ViewModels) or none.
        /// </summary>
        static readonly bool[,] _allowance =
        {         /* Boot   Core   GameLogic Presentation UI */
/* Boot */          {false, false, false,    false,       false}, // Boot does not need to send signals as it has access to everything
/* Core */          {false, false, true,     true,        true},
/* GameLogic */     {false, false, false,    true,        true},
/* Presentation */  {false, false, false,    false,       true},
/* UI */            {false, false, false,    false,       false}  // UI communicates with everything directly (through ViewModels)
        };

        static readonly DebugConfig _config;
#endif

        internal static void Intercept(int signalId, string signalName, params object[] args)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Delegate reactMethods = SignalService.GetReactMethods(signalId);
            Delegate[] delegates = reactMethods.GetInvocationList();

            foreach (Delegate d in delegates)
            {
                Assembly targetAssembly = d.Method.DeclaringType!.Assembly;
                string receivingName = targetAssembly.GetName().Name;
                int receivingId = _lookup[receivingName];

                MethodBase? method = new System.Diagnostics.StackTrace().GetFrame(3).GetMethod();
                Assembly callingAssembly = method.DeclaringType!.Assembly;
                string callingName = callingAssembly.GetName().Name;

                if (_lookup.TryGetValue(callingName, out int callingId))
                {
                    // normal execution
                    Assert.IsTrue(_allowance[callingId, receivingId], Message());
                }
                else
                {
                    // listener execution
                    method = new System.Diagnostics.StackTrace().GetFrame(2).GetMethod();
                    callingAssembly = method.DeclaringType!.Assembly;
                    callingName = callingAssembly.GetName().Name;
                }

                if (_config.LogSentSignals)
                {
                    string part, part2 = "";

                    // constructor
                    if (method.Name == ".ctor")
                        part = "'s constructor";
                    // setter
                    else if (method.Name.Contains("set_"))
                        part = $".{method.Name.Split('_')[1]} setter";
                    // getter
                    else if (method.Name.Contains("get_"))
                        part = $".{method.Name.Split('_')[1]} getter";
                    // method
                    else
                        part = $".{method.Name}";

                    for (int j = 0; j < args.Length; j++)
                    {
                        object obj = args[j];
                        if (j > 0)
                            part2 += ", ";
                        part2 += obj;
                    }

                    Debug.Log($"{signalName} was sent in: {method.DeclaringType.FullName}{part}, args: {part2}");
                }

                Assert.IsTrue(_allowance[callingId, receivingId], Message());

                continue;

                string Message() => callingId == receivingId
                    ? $"Sending and receiving signals in the same assembly ({callingName}) is not allowed as it adds needless complexity. "
                      + "Please use normal function calls."
                    : $"Sending signals from {callingName} and receiving them in {receivingName} is not allowed. "
                      + $"If {callingName} assembly references {receivingName} please use normal calls (public methods are declared in ViewModels).";
            }
#endif

            SignalService.AddSignal(signalId, args);
        }
    }
}