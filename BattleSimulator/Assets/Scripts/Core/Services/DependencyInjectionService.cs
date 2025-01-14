#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Interfaces;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Core.Services
{
    /// <summary>
    /// This class scans through all assemblies and injects <see cref="InjectAttribute"/> fields and configs (ScriptableObjects).
    /// Assembly names are hardcoded: Boot, Core, DataOriented, GameLogic, Presentation, Shared, and UI.
    /// </summary>
    /// <typeparam name="TScriptableObject">Always ScriptableObject type</typeparam>
    public static class DependencyInjectionService<TScriptableObject> where TScriptableObject : class
    {
        /// <summary>
        /// Every instance (controller or viewmodel) can be identified by a variety  of different elements.
        /// Typically, the type is enough but in more complicated cases the name and the interfaces are needed as well.
        /// </summary>
        class DependencyUnit
        {
            // identifiers
            internal readonly Type Type;
            internal readonly string? Name;
            internal readonly Type[] Interfaces;

            // instance
            internal readonly object Instance;

            internal DependencyUnit(Type type, object instance)
            {
                Type = type;
                Instance = instance;
                Interfaces = Type.EmptyTypes;
            }

            internal DependencyUnit(Type type, string name, Type[] interfaces, object instance)
            {
                Type = type;
                Name = name;
                Interfaces = interfaces;
                Instance = instance;
            }
        }

        static readonly List<DependencyUnit> _dependencyUnits = new ();

        // ReSharper disable once StaticMemberInGenericType
        // ReSharper disable once IdentifierTypo
        static readonly List<IInitializable> _initializables = new();

        [UsedImplicitly]
        public static void Inject(Func<Type, TScriptableObject?> findConfig, List<string>? additionalAssemblies = null)
        {
            Type injectAttribute = typeof(InjectAttribute);
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

            bool signalsBound = false;

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested)
                        continue;

                    // generally ignore interfaces expect for ISignal
                    if (!signalsBound)
                        if (type is {IsInterface: true, Name: "ISignal"})
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
                            if (TryGetInstance(field, out object? instance))
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
                        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

                        if (constructors.Length == 0)
                            throw new Exception($"{type.Name} has no parameterless constructor. Please add one with the attribute [Preserve].");

                        object instance = constructors[0].Invoke(new object[] { });
                        AddDependencyUnit(type, instance);
                        SignalService.AddReactiveInstantiatable(instance);

                        if (typeof(IInitializable).IsAssignableFrom(type))
                            _initializables.Add((IInitializable)instance);
                    }
                }

            // bind remaining
            while (awaitingInjectFields.Count > 0)
            {
                (Type ownerInstance, FieldInfo fieldInfo) = awaitingInjectFields.Dequeue();

                if (TryGetInstance(fieldInfo, out object? instance))
                    fieldInfo.SetValue(ownerInstance, instance);
                else
                    throw new Exception("impossible state");
            }
        }

        static readonly Dictionary<Type, List<Type>> _bindings = new();

        public static void Bind<T>(Type type)
        {
            Assert.IsTrue(typeof(T).IsInterface, "The generic type parameter {typeof(T).Name} must be an interface.");

            if (_bindings.TryGetValue(typeof(T), out List<Type> list))
            {
                Assert.IsFalse(list.Contains(type), "Binding the same element twice is not allowed.");
                list.Add(type);
            }
            else
                _bindings.Add(typeof(T), new List<Type> {type});
        }

        /// <summary>
        /// Initialization invoke must be done on a different frame as injected fields (for example configs)
        /// will not be accessible if accessed on the same frame.
        /// </summary>
        public static void InvokeInitialization()
        {
            foreach (IInitializable instance in _initializables)
                instance.Initialize();
        }

        static void AddDependencyUnit(Type type, object instance)
        {
            // check interfaces 
            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                string name;
                // slice name
                if (type.Name.EndsWith("Controller"))
                    name = type.Name[..^10].ToLower();
                else if (type.Name.EndsWith("ViewModel"))
                    name = type.Name[..^9].ToLower();
                else
                    throw new Exception("Impossible state");

                _dependencyUnits.Add(new DependencyUnit(type, name, interfaces, instance));
            }
            else
            {
                _dependencyUnits.Add(new DependencyUnit(type, instance));
            }
        }

        static bool TryGetInstance(FieldInfo field, out object? instance)
        {
            Type fieldType = field.FieldType;

            // if it is an interface we try to bind by interface and name
            if (fieldType.IsInterface)
            {
                string name;

                if (field.Name.EndsWith("Controller"))
                    name = field.Name.Substring(1, field.Name.Length - 11).ToLower();
                else if (field.Name.EndsWith("ViewModel"))
                    name = field.Name.Substring(1, field.Name.Length - 10).ToLower();
                else
                    throw new Exception("Impossible state");

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (DependencyUnit unit in _dependencyUnits)
                {
                    Type? inter = unit.Interfaces.FirstOrDefault(t => t == fieldType);

                    if (inter == null)
                        continue;

                    if (unit.Name != name)
                        continue;

                    instance = unit.Instance;
                    return true;
                }
            }
            else
            {
                // find matching by type
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (DependencyUnit unit in _dependencyUnits)
                    if (unit.Type == fieldType)
                    {
                        instance = unit.Instance;
                        return true;
                    }

                instance = null;
                return false;
            }

            instance = null;
            return false;
        }
    }
}