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
        class AwaitingConstruction
        {
            /// <summary>
            /// Constructor that creates the instance.
            /// </summary>
            internal readonly ConstructorInfo Constructor;

            /// <summary>
            /// Parameters need to construct the object by using the <see cref="Constructor"/>.
            /// </summary>
            internal readonly ParameterInfo[] Parameters;

            /// <summary>
            /// Fields that this instance injects into.
            /// </summary>
            internal readonly List<FieldInfo> Fields = new();

            /// <summary>
            /// Instances of controllers or viewmodels that have injectable fields awaiting to be injected.
            /// Matches 1 to 1 <see cref="Fields"/>.
            /// </summary>
            internal readonly List<object> Instances = new();

            internal AwaitingConstruction(ConstructorInfo constructor, ParameterInfo[] parameters)
            {
                Constructor = constructor;
                Parameters = parameters;
            }
        }

        /// <summary>
        /// These instances are created once and last for the whole time.
        /// If the controller is also bound to an interface it will be present on that list twice.
        /// Key is the instance's type and value is the instance itself.
        /// </summary>
        static readonly Dictionary<Type, object> _staticInstances = new();

        /// <summary>
        /// These instances are created every time <see cref="BindModel{T}"/> is called.
        /// Key is the instance's type and value is the instance itself.
        /// </summary>
        static readonly Dictionary<Type, AwaitingConstruction> _dynamicInstances = new();

        static readonly List<IInitializable> _initializables = new();

        /// <summary>
        /// Key is an interface. Value is a list of types that binds with that interface.
        /// </summary>
        static readonly Dictionary<Type, List<Type>> _boundInterfaces = new();

        /// <summary>
        /// Key: Constructor parameter's type.
        /// Value: Instance of the parameter.
        /// </summary>
        static readonly Dictionary<Type, object> _boundModels = new();

        static readonly List<string> _assemblyNames = new()
        {
            "Boot",
            "Core",
            "GameLogic",
            "Presentation",
            "UI"
        };

        public static void Inject(Func<Type, TScriptableObject?> findConfig)
        {
            var assemblies = new Assembly[_assemblyNames.Count];
            for (int i = 0; i < _assemblyNames.Count; i++)
                assemblies[i] = Assembly.Load(_assemblyNames[i]);

            // this creates signal queues
            BindSignals(assemblies);

            // this injects configs and creates react method for services
            BindConfigsAndReactiveServices(assemblies, findConfig);

            // this creates all controllers/viewmodels that do not have parametrized constructors
            CreateStaticInstances(assemblies);

            // goes through all static instances and inject into fields that need other static instances
            InjectIntoStaticInstances();

            // at this moment we have
            // - all configs injected
            // - all static instances created
        }

        /// <summary>
        /// Associate a Controller/ViewModel with an interface. Many types can be bound to the same interface.
        /// In such case each field the type injects into must be an array or a list.
        /// Injected instances will be in present in the collection in the same order they were bound.
        /// </summary>
        /// <param name="type">The type of the controller or the viewmodel you want to associate (bind) with the interface.
        /// Must implement the interface.</param>
        /// <typeparam name="T">Type of the interface you want to bind to</typeparam>
        public static void BindInterface<T>(Type type)
        {
            Assert.IsTrue(typeof(T).IsInterface, "The generic type parameter {typeof(T).Name} must be an interface.");

            if (_boundInterfaces.TryGetValue(typeof(T), out List<Type> list))
            {
                Assert.IsFalse(list.Contains(type), "Binding the same element twice is not allowed.");
                list.Add(type);
            }
            else
                _boundInterfaces.Add(typeof(T), new List<Type> {type});
        }

        /// <summary>
        /// Each time this method is called, all controllers and view models that rely on this model are reconstructed and reinjected.
        /// </summary>
        public static void BindModel<T>(object model)
        {
            _boundModels.Add(typeof(T), model);

            // go through all awaiting injections, then recreate and inject them
            foreach (KeyValuePair<Type, AwaitingConstruction> kvp in _dynamicInstances)
            {
                AwaitingConstruction ac = kvp.Value;
                bool allParams = true;
                object[] actualParams = new object[ac.Parameters.Length];

                for (int i = 0; i < ac.Parameters.Length; i++)
                    if (_boundModels.TryGetValue(ac.Parameters[i].ParameterType, out object m))
                    {
                        actualParams[i] = m;
                    }
                    else
                    {
                        allParams = false;
                        break;
                    }

                if (!allParams)
                    continue;

                object injectedFieldInstance = ac.Constructor.Invoke(actualParams);

                // injection
                // ReSharper disable once ForCanBeConvertedToForeach
                for (int i = 0; i < ac.Fields.Count; i++)
                {
                    FieldInfo info = ac.Fields[i];

                }
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

        static void CreateStaticInstances(Assembly[] assemblies)
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

                        // todo: tak naprawde jedynie jaka rzecz sie liczy to ilosc parametrow a nie atrybut
                        // check if constructor injection
                        if (constructor.GetCustomAttributes(injectAttribute, false).Length > 0)
                            continue;

                        // normal construction
                        object instance = constructors[0].Invoke(new object[] { });

                        if (_staticInstances.TryGetValue(type, out object _))
                            throw new ArgumentException("Binding the same element twice is not allowed.");

                        _staticInstances.Add(type, instance);
                        SignalService.AddReactiveInstantiatable(instance);

                        if (typeof(IInitializable).IsAssignableFrom(type))
                            _initializables.Add((IInitializable)instance);
                    }
                }
        }

        /// <summary>
        /// Inject static instances into fields that needs them.
        /// Other fields are ignored.
        /// </summary>
        static void InjectIntoStaticInstances()
        {
            foreach ((Type instanceType, object instance) in _staticInstances)
            {
                Type injectAttribute = typeof(InjectAttribute);
                FieldInfo[] fields = instanceType.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

                // ReSharper disable once ForCanBeConvertedToForeach
                for (int index = 0; index < fields.Length; index++)
                {
                    FieldInfo info = fields[index];
                    if (info.IsConst())
                        continue;

                    // is injectable
                    if (Attribute.GetCustomAttributes(info, injectAttribute, false).Length == 0)
                        continue;

                    Type fieldType = info.FieldType;

                    // the field is an array
                    if (fieldType.IsArray)
                    {
                        Type elementType = fieldType.GetElementType()!;

                        // array of interfaces
                        if (elementType.IsInterface)
                        {
                            if (!_boundInterfaces.TryGetValue(elementType, out List<Type> types))
                                throw new Exception($"Could not find binding for the field {info.Name}");

                            object[] instances = new object[types.Count];

                            for (int i = 0; i < types.Count; i++)
                                instances[i] = _staticInstances[types[i]];

                            info.SetValue(instance, instances);
                        }
                        else
                        {
                            // impossible state
                            // todo: in the future could be abstract class
                        }

                        continue;
                    }

                    // the field is a list
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        Type elementType = fieldType.GetGenericArguments()[0];

                        if (!_boundInterfaces.TryGetValue(elementType, out List<Type> types))
                            throw new Exception($"Could not find binding for the field {info.Name}");

                        // todo: what if some of these require constructor injection?
                        // todo: should we inject only some of them? What about order? The rest will be null or not present? 
                        info.SetValue(instance, types.Select(t => _staticInstances[t]).ToList());

                        continue;
                    }

                    object value;

                    // the field is an interface
                    if (fieldType.IsInterface)
                    {
                        if (!_boundInterfaces.TryGetValue(fieldType, out List<Type> types))
                            throw new Exception($"Could not find binding for the field {info.Name}");

                        Assert.IsTrue(types.Count == 1,
                                      "Multiple bindings detected. Dependency Injector could not resolve which one to bind.");

                        _staticInstances.TryGetValue(types[0], out value);
                        info.SetValue(instance, value);

                        continue;
                    }

                    // the field is a normal field
                    _staticInstances.TryGetValue(fieldType, out value);
                    info.SetValue(instance, value);

                }
            }
        }
    }
}