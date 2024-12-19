using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Services
{
    public abstract class AbstractStateService<TState, TTransitionParameter>
        where TState : struct, Enum
        where TTransitionParameter : struct, Enum
    {
        protected readonly struct StateDto
        {
            public readonly Action? OnEntry;
            public readonly Action? OnExit;

            internal StateDto(Action? onEntry, Action? onExit)
            {
                OnEntry = onEntry;
                OnExit = onExit;
            }
        }

        protected readonly struct TransitionDto
        {
            public readonly TState From;
            public readonly TState To;
            public readonly Func<(int[]?, int[]?)>? ScenesToLoadUnload;

            internal TransitionDto(TState from, TState to, Func<(int[]?, int[]?)>? scenesToLoadUnload)
            {
                From = from;
                To = to;
                ScenesToLoadUnload = scenesToLoadUnload;
            }
        }

        protected readonly List<TransitionDto> _transitions;
        protected readonly Dictionary<TState, StateDto> _states = new();
        protected TState _currentState;
        protected readonly Dictionary<TTransitionParameter, object?> _parameters = new();
        protected bool _shouldDefaultParameters;

        protected AbstractStateService(IReadOnlyList<(TState from, TState to, Func<(int[]?, int[]?)>? scenesToLoadUnload)> transitions,
            IReadOnlyList<(TState state, Action? onEntry, Action? onExit)> states)
        {
            _transitions = new List<TransitionDto>(transitions.Count);
            foreach ((TState from, TState to, Func<(int[]?, int[]?)>? scenesToLoadUnload) in transitions)
                _transitions.Add(new TransitionDto(from, to, scenesToLoadUnload));

            foreach ((TState state, Action? onEntry, Action? onExit) state in states)
                _states.Add(state.state, new StateDto(state.onEntry, state.onExit));
        }

        public TState GetCurrentState() => _currentState;

        protected static bool Equal(Enum a, Enum b) => Enum.GetName(a.GetType(), a) == Enum.GetName(b.GetType(), b);

        protected void EndFrameSignal()
        {
            if (!_shouldDefaultParameters)
                return;

            for (int i = 0; i < _parameters.Count; i++)
            {
                TTransitionParameter key = _parameters.ElementAt(i).Key;

                if (_parameters[key] == null)
                    continue;

                Type type = _parameters[key]!.GetType();
                _parameters[key] = type.IsValueType ? Activator.CreateInstance(type) : null;
            }

            _shouldDefaultParameters = false;
        }

        /// <summary>
        /// Returns a copy containing elements from both arrays.
        /// If a and b are both null or empty, returns empty array.
        /// </summary>
        protected static int[] CombineArrays(int[]? a, int[]? b)
        {
            if (a is { Length: > 0 } && b is { Length: > 0 })
            {
                int[] arr = new int[a.Length + b.Length];

                int i = 0;
                for (; i < a.Length; i++)
                    arr[i] = a[i];

                for (int j = 0; j < b.Length; j++)
                    arr[i++] = b[j];

                return arr;
            }

            if (a is { Length: > 0 })
            {
                int[] arr = new int[a.Length];

                int i = 0;
                for (; i < a.Length; i++)
                    arr[i] = a[i];

                return arr;
            }

            if (b is { Length: > 0 })
            {
                int[] arr = new int[b.Length];

                int i = 0;
                for (; i < b.Length; i++)
                    arr[i] = b[i];

                return arr;
            }

            return new int[] {};
        }
    }
}