#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.Interfaces;
using UnityEngine.Assertions;
// ReSharper disable StaticMemberInGenericType

namespace Core.Services
{
    /// <summary>
    /// This class scans through all assemblies and injects <see cref="InjectAttribute"/> fields and configs (ScriptableObjects).
    /// Assembly names are hardcoded: Boot, Core, DataOriented, GameLogic, Presentation, Shared, and UI.
    /// </summary>
    /// <typeparam name="TScriptableObject">Always ScriptableObject type</typeparam>
    public static class DependencyInjectionService<TScriptableObject> where TScriptableObject : class
    {
        class AwaitedConstruction
        {
            /// <summary>
            /// Target type this instance binds to.
            /// </summary>
            internal readonly Type Type;

            /// <summary>
            /// Constructor that creates the instance.
            /// </summary>
            internal readonly ConstructorInfo Constructor;

            /// <summary>
            /// Parameters of the <see cref="Constructor"/>.
            /// </summary>
            internal readonly ParameterInfo[] Parameters;

            /// <summary>
            /// Fields that this instance injects into.
            /// </summary>
            internal List<FieldInfo> Fields;

            internal AwaitedConstruction(Type type, ConstructorInfo constructor, ParameterInfo[] parameters)
            {
                Type = type;
                Constructor = constructor;
                Parameters = parameters;
            }
        }

        /// <summary>
        /// Key is a controller/view-model's type. Value is the instance.
        /// </summary>
        static readonly Dictionary<Type, object> _instances = new();

        /// <summary>
        /// Key is a controller/view-model's type. Value is the instance.
        /// </summary>
        static readonly List<AwaitedConstruction> _instancesConstructorInjection = new();

        static readonly List<IInitializable> _initializables = new();

        /// <summary>
        /// Key is an interface. Value is a list of types that binds with that interface.
        /// </summary>
        static readonly Dictionary<Type, List<Type>> _interfaceBindings = new();

        static readonly Dictionary<Type, object> _boundModels = new();

        static readonly List<string> _assemblyNames = new()
        {
            "Boot",
            "Core",
            "GameLogic",
            "Presentation",
            "UI"
        };

        public static void Inject(Func<Type, TScriptableObject?> findConfig, List<string>? additionalAssemblies = null)
        {
            if (additionalAssemblies != null)
                _assemblyNames.AllocFreeAddRange(additionalAssemblies);

            var assemblies = new Assembly[_assemblyNames.Count];
            for (int i = 0; i < _assemblyNames.Count; i++)
                assemblies[i] = Assembly.Load(_assemblyNames[i]);

            // this creates signal queues
            BindSignals(assemblies);

            // this injects configs and creates react method for services
            BindConfigsAndReactiveServices(assemblies, findConfig);

            // this creates instances of controllers/viewmodel that can be constructed (have parameterless constructors)
            // classes with injectable constructors are added to list
            CreateOrPostponeInstances(assemblies);

            // what is still to do:
            // - inject what possible (constructor-less)
            // - that's it
            // - wait for BindModels and then go through all fields again (do we
            InjectWhatPossible(assemblies);
        }

        /// <summary>
        /// Associate a Controller/ViewModel with an interface. Many types can be bound to the same interface.
        /// In such case each field the type injects into must be an array or a list.
        /// Injected instances will be in present in the collection in the same order they were bound.
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
        }

        public static void RebindModel<T>(object model)
        {
            _boundModels.Add(typeof(T), model);

            // go through all awaiting injections, recreate and inject them
            for (int i = 0; i < _instancesConstructorInjection.Count; i++)
            {

            }
        }

        /// <summary>
        /// Invoke all <see cref="IInitializable"/> methods implemented by all Controllers and ViewModels.
        /// This method myst be called on different frame as the method that inject fields (or example configs)
        /// because those methods at not accessible at the same frame.
        /// </summary>
        public static void InvokeInitialization()
        {
            foreach (IInitializable instance in _initializables)
                instance.Initialize();
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

        static void CreateOrPostponeInstances(Assembly[] assemblies)
        {
            Type injectAttribute = typeof(InjectAttribute);

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested || type.IsInterface || type.IsArray)
                        continue;

                    // filter out types created by the compiler f.e. "PrivateImplementationDetails"
                    if (type.Namespace == null)
                        continue;

                    // Controllers are never abstract
                    if (type.IsAbstract)
                        continue;

                    // todo: should ViewModels be even instantiated? Yes, they may have interfaces
                    // bind type in the container if it is a Controller or a ViewModel
                    // ReSharper disable once InvertIf
                    if (type.Namespace.EndsWith("Controllers") || type.Namespace.EndsWith("ViewModels") || type.Name.EndsWith("Controller"))
                    {
                        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

                        if (constructors.Length == 0)
                            throw new Exception($"{type.Name} has no parameterless constructor. Please add one with the attribute [Preserve].");

                        ConstructorInfo constructor = constructors[0];

                        // check if constructor injection
                        if (constructor.GetCustomAttributes(injectAttribute, false).Length > 0)
                        {
                            ParameterInfo[] parameters = constructor.GetParameters();

                            // these need to wait at this moment we only need constructors and what they bind to
                            _instancesConstructorInjection.Add(new AwaitedConstruction(type, constructor, parameters));
                        }
                        else
                        {
                            // normal construction
                            object instance = constructors[0].Invoke(new object[] { });

                            if (_instances.TryGetValue(type, out object _))
                                throw new ArgumentException("Binding the same element twice is not allowed.");

                            _instances.Add(type, instance);
                            SignalService.AddReactiveInstantiatable(instance);

                            if (typeof(IInitializable).IsAssignableFrom(type))
                                _initializables.Add((IInitializable)instance);
                        }
                    }
                }
        }

        static void InjectWhatPossible(Assembly[] assemblies)
        {
            Type injectAttribute = typeof(InjectAttribute);

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested)
                        continue;

                    // filter out types created by the compiler f.e. "PrivateImplementationDetails"
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

                        // first check if an array, a generic list, or a regular field
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

                            // todo: what if some of these require constructor injection?
                            // todo: should we inject only some of them? What about order? The rest will be null or not present? 
                            info.SetValueDirect(typedRef, types.Select(t => _instances[t]).ToList());
                        }
                        else
                        {
                            if (fieldType.IsInterface)
                            {
                                if (!_interfaceBindings.TryGetValue(fieldType, out List<Type> types))
                                    throw new Exception($"Could not find binding for the field {info.Name}");

                                Assert.IsTrue(types.Count == 1,
                                              "Multiple bindings detected. Dependency Injector could not resolve which one to bind.");

                                TryInject(info, types[0]);
                            }
                            else
                            {
                                TryInject(info, fieldType);
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// two possibilities:<b/>
        /// - we either inject immediately if it is on the <see cref="_instances"/> list<b/>
        /// - or we ignore it for now if it requires construction injection (is on the <see cref="_instancesConstructorInjection"/> list)<b/>
        /// - if neither from above then exception<b/>
        /// </summary>
        static void TryInject(FieldInfo info, Type fieldType)
        {
            if (_instances.TryGetValue(fieldType, out object instance))
            {
                TypedReference typedRef = __makeref(info); // fast injection
                info.SetValueDirect(typedRef, instance);
            }
            else
            {
                // check if it is on the awaited list
                AwaitedConstruction? ac = _instancesConstructorInjection.FirstOrDefault(a => a.Type == fieldType);
                if (ac == null)
                    throw new Exception($"Could not find a suitable constructor for field {info.Name} of type {fieldType}.");
            }
        }

        static void BindConfigsAndReactiveServices(Assembly[] assemblies, Func<Type, TScriptableObject?> findConfig)
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

                    // Services are never abstract
                    if (type.IsAbstract)
                        continue;

                    // todo: in the future, make suffix "Service" a requirement
                    if (type.IsStatic() && (type.Namespace.EndsWith("Services") || type.Name.EndsWith("Service")))
                        SignalService.AddReactiveService(type);
                }
        }
    }
}