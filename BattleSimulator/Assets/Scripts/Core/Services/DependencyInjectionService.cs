#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Interfaces;
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
        /// Key is a controller/view-model's type. Value is the instance.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        static readonly Dictionary<Type, object> _instances = new();

        // ReSharper disable once StaticMemberInGenericType
        static readonly List<IInitializable> _initializables = new();

        /// <summary>
        /// Key is an interface. Value is a list of types that binds with that interface.
        /// </summary>
        // ReSharper disable once StaticMemberInGenericType
        static readonly Dictionary<Type, List<Type>> _interfaceBindings = new();

        public static void Inject(Func<Type, TScriptableObject?> findConfig, List<string>? additionalAssemblies = null)
        {
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

            BindSignals(assemblies);
            BindConfigsAndCreateInstances(assemblies, findConfig);
        }

        /// <summary>
        /// Binds Controller/ViewModel to an interface. You can bind one or more types to the same interface.
        /// In case of more types than one the field must be an array or a list.
        /// Injects in that list will be in the same order they were bound.
        /// </summary>
        /// <param name="type">The type of the controller or the viewmodel you want to associate (bind) with the interface.
        /// Must implement the interface.</param>
        /// <typeparam name="T">Type of the interface you want to bind to</typeparam>
        public static void BindToInterface<T>(Type type)
        {
            Assert.IsTrue(typeof(T).IsInterface, "The generic type parameter {typeof(T).Name} must be an interface.");

            if (_interfaceBindings.TryGetValue(typeof(T), out List<Type> list))
            {
                Assert.IsFalse(list.Contains(type), "Binding the same element twice is not allowed.");
                list.Add(type);
            }
            else
                _interfaceBindings.Add(typeof(T), new List<Type> {type});

            CreateInstance(type);
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

        public static void ResolveBindings(List<string>? additionalAssemblies = null)
        {
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

            Type injectAttribute = typeof(InjectAttribute);

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested)
                        continue;

                    // todo: there are some types created by the compiler f.e. "PrivateImplementationDetails" that we want to filter out here
                    // todo: I don't know how to do it and this method is kinda too generic
                    // todo: however it works well in our case because our convention forces to add namespaces everywhere
                    if (type.Namespace == null)
                        continue;

                    FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int index = 0; index < fields.Length; index++)
                    {
                        FieldInfo info = fields[index];
                        if (info.IsConst())
                            continue;

                        // is injectable
                        if (Attribute.GetCustomAttributes(info, injectAttribute, false).Length <= 0)
                            continue;

                        Type fieldType = info.FieldType;
                        TypedReference typedRef = __makeref(info); // fast injection

                        // first check if an array
                        if (fieldType.IsArray)
                        {
                            Type elementType = fieldType.GetElementType()!;

                            // array of interfaces
                            if (elementType.IsInterface)
                            {
                                if (!_interfaceBindings.TryGetValue(elementType, out List<Type> types))
                                    throw new Exception($"Could not find binding for the field {info.Name}");

                                object[] instances = new object[types.Count];

                                for (int i = 0; i < types.Count; i++)
                                    instances[i] = _instances[types[i]];

                                info.SetValueDirect(typedRef, instances);
                            }
                            else
                            {
                                // impossible state
                            }
                        }
                        else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type elementType = fieldType.GetGenericArguments()[0];

                            if (!_interfaceBindings.TryGetValue(elementType, out List<Type> types))
                                throw new Exception($"Could not find binding for the field {info.Name}");

                            info.SetValueDirect(typedRef, types.Select(t => _instances[t]).ToList());
                        }
                        else // normal field
                        {
                            // is interface?
                            if (fieldType.IsInterface)
                            {
                                if (!_interfaceBindings.TryGetValue(fieldType, out List<Type> types))
                                    throw new Exception($"Could not find binding for the field {info.Name}");

                                Assert.IsTrue(types.Count == 1,
                                              "Multiple bindings detected. Dependency Injector could not resolve which one to bind.");
                                info.SetValueDirect(typedRef, _instances[types[0]]);
                            }
                            else
                            {
                                if (_instances.TryGetValue(fieldType, out object instance))
                                    info.SetValueDirect(typedRef, instance);
                                else
                                    throw new Exception($"No instance of type {fieldType} to bind into the field {info.Name}.");
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// Goes through 'Core' assembly, searches for <see cref="ISignal"/> and binds all the methods.
        /// </summary>
        static void BindSignals(Assembly[] assemblies)
        {
            Assembly core = assemblies.First(a => a.GetName().Name == "Core");

            foreach (Type type in core.GetTypes())
            {
                // ignore internal classes, enums
                if (type.IsEnum || type.IsNested)
                    continue;

                if (type is not {IsInterface: true, Name: "ISignal"})
                    continue;

                SignalService.BindSignals(type.GetMethods());
                return;
            }

            throw new Exception("Impossible state - ISignal class not found.");
        }

        static void CreateInstance(Type type)
        {
            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

            if (constructors.Length == 0)
                throw new Exception($"{type.Name} has no parameterless constructor. Please add one with the attribute [Preserve].");

            object instance = constructors[0].Invoke(new object[] { });

            if (_instances.TryGetValue(type, out object _))
                throw new ArgumentException("Binding the same element twice is not allowed.");

            _instances.Add(type, instance);
            SignalService.AddReactiveInstantiatable(instance);

            if (typeof(IInitializable).IsAssignableFrom(type))
                _initializables.Add((IInitializable)instance);
        }

        /// <summary>
        /// This method does a few things:
        /// -
        /// -
        /// - 
        /// Goes through all assemblies, injects all config files, and
        /// </summary>
        /// <param name="assemblies"></param>
        /// <param name="findConfig"></param>
        /// <exception cref="Exception"></exception>
        static void BindConfigsAndCreateInstances(Assembly[] assemblies, Func<Type, TScriptableObject?> findConfig)
        {
            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested)
                        continue;

                    // todo: there are some types created by the compiler f.e. "PrivateImplementationDetails" that we want to filter out here
                    // todo: I don't know how to do it and this method is kinda too generic
                    // todo: however it works well in our case because our convention forces to add namespaces everywhere
                    if (type.Namespace == null)
                        continue;

                    foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (field.IsConst())
                            continue;

                        // is config
                        if (!field.FieldType.IsSubclassOf(typeof(TScriptableObject)) || field.FieldType.Name[^6..] != "Config")
                            continue;

                        TScriptableObject config = findConfig.Invoke(field.FieldType)
                                                   ?? throw new Exception($"No Config found for the field named: '{field.Name}', "
                                                                          + $"of type: {field.FieldType}, located in: {type.Name}");

                        field.SetValue(type, config);
                    }

                    // todo: in the future, make suffix "Service" a requirement
                    if (type.IsStatic() && (type.Namespace.EndsWith("Services") || type.Name.EndsWith("Service")))
                    {
                        SignalService.AddReactiveSystem(type);
                        continue;
                    }

                    // Controllers are never abstract
                    if (type.IsAbstract)
                        continue;

                    // todo: should ViewModels be even instantiated? Yes, they may have interfaces
                    // bind type in the container if it is a Controller or a ViewModel
                    // ReSharper disable once InvertIf
                    if (type.Namespace.EndsWith("Controllers") || type.Namespace.EndsWith("ViewModels") || type.Name.EndsWith("Controller"))
                        // if present on the binding list then the instance already exists
                        if (!type.GetInterfaces().Any(i => _interfaceBindings.TryGetValue(i, out _)))
                            CreateInstance(type);
                }
        }
    }
}