using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Interfaces;
using JetBrains.Annotations;

namespace Core.Services
{
    /// <summary>
    /// This class scans through all assemblies and injects <see cref="InjectAttribute"/> fields and configs (ScriptableObjects).
    /// Assembly names are hardcoded: Boot, Core, DataOriented, GameLogic, Presentation, Shared, and UI.
    /// </summary>
    /// <typeparam name="TScriptableObject">Always ScriptableObject type</typeparam>
    public static class DependencyInjectionService<TScriptableObject> where TScriptableObject : class
    {
        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once IdentifierTypo
        static readonly List<IInitializable> _initializables = new();

        [UsedImplicitly]
        public static void Inject(Func<Type, TScriptableObject?> findConfig, List<string>? additionalAssemblies = null)
        {
            Type injectAttribute = typeof(InjectAttribute);
            var instances = new Dictionary<Type, object>();
            var awaitingInjectFields = new Queue<(Type ownerInstance, FieldInfo fieldInfo)>();

            var assemblyNames = new List<string>
            {
                "Boot",
                "Core",
                "GameLogic",
                "Presentation",
                "UI"
            };

            if (additionalAssemblies != null)
                assemblyNames.AllocFreeAddRange(additionalAssemblies);

            var assemblies = new Assembly[assemblyNames.Count];
            for (int i = 0; i < assemblyNames.Count; i++)
                assemblies[i] = Assembly.Load(assemblyNames[i]);

            // todo: parallelization isn't faster as of yet. Maybe on bigger projects it will be
            // test result (in Editor)
            // Parallel: 17ms on average (5 measurements)
            // Normal: 13ms on average (5 measurements) 

            bool signalsBound = false;

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested)
                        continue;

                    // generally ignore interfaces expect for ISignal
                    if (type.IsInterface)
                        if (signalsBound)
                            continue;
                        else if (type.Name == "ISignal")
                        {
                            SignalService.BindSignals(type.GetMethods());
                            signalsBound = true;
                        }

                    // todo: there are some types created by the compiler f.e. "PrivateImplementationDetails" that we want to filter out here
                    // todo: I don't know how to do it and this method is kinda too generic
                    // todo: however it works well in our case because our convention forces to add namespaces everywhere
                    if (type.Namespace == null)
                        continue;

                    foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (field.IsConst())
                            continue;

                        if (field.FieldType.IsSubclassOf(typeof(TScriptableObject)) && field.FieldType.Name[^6..] == "Config")
                        {
                            TScriptableObject config = findConfig.Invoke(field.FieldType)
                                                       ?? throw new Exception($"No Config found for the field named: '{field.Name}', "
                                                                              + $"of type: {field.FieldType}, located in: {type.Name}");
                            field.SetValue(type, config);
                        }
                        else if (Attribute.GetCustomAttributes(field, injectAttribute, false).Length > 0)
                            if (instances.TryGetValue(field.GetType(), out object instance))
                                field.SetValue(type, instance);
                            else
                                awaitingInjectFields.Enqueue((type, field));
                    }

                    if (type.IsStatic() && (type.Namespace.EndsWith("Services") || type.Name.EndsWith("Service")))
                    {
                        SignalService.AddReactiveSystem(type);
                        continue;
                    }

                    // Controllers are never abstract
                    if (type.IsAbstract)
                        continue;

                    // bind type in the container if it is a Controller or a ViewModel
                    if (type.Namespace.EndsWith("Controllers") || type.Namespace.EndsWith("ViewModels") || type.Name.EndsWith("Controller"))
                    {
                        object instance = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { });
                        instances.Add(type, instance);
                        SignalService.AddReactiveInstantiatable(instance);

                        if (typeof(IInitializable).IsAssignableFrom(type))
                            _initializables.Add((IInitializable)instance);
                    }
                }

            // bind remaining
            while (awaitingInjectFields.Count > 0)
            {
                (Type ownerInstance, FieldInfo fieldInfo) = awaitingInjectFields.Dequeue();
                instances.TryGetValue(fieldInfo.FieldType, out object instance);
                fieldInfo.SetValue(ownerInstance, instance);
            }
        }

        /// <summary>
        /// Initialization invoke must be done on a different frame as injected fields (for example configs)
        /// will not be accessible if accessed on the same frame.
        /// </summary>
        [UsedImplicitly]
        public static void InvokeInitialization()
        {
            foreach (IInitializable instance in _initializables)
                instance.Initialize();
        }
    }
}