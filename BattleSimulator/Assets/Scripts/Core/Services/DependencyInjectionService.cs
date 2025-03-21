﻿#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
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
        class DynamicInstance
        {
            /// <summary>
            /// Constructor that creates the instance.
            /// </summary>
            internal readonly ConstructorInfo Constructor;

            /// <summary>
            /// Parameters need to construct the object by using the <see cref="Constructor"/>.
            /// </summary>
            internal readonly ParameterInfo[] Parameters;

            internal readonly List<FieldInfo> DynamicDependencies = new();

            internal object Instance;

            internal DynamicInstance(ConstructorInfo constructor, ParameterInfo[] parameters)
            {
                Constructor = constructor;
                Parameters = parameters;
            }
        }

        class StaticInstance
        {
            internal readonly Type Type;
            internal readonly object Instance;

            /// <summary>
            /// If there are fields that bind by interface to this instance, this field will contain that interface.
            /// </summary>
            internal Type? BoundInterface;

            internal readonly List<FieldInfo> DynamicDependencies = new();

            internal StaticInstance(Type type, object instance)
            {
                Type = type;
                Instance = instance;
            }
        }

        /// <summary>
        /// These instances are created once and last for the whole time.
        /// If the controller is also bound to an interface it will be present on that list twice.
        /// Key is the instance's type and value is the instance itself.
        /// </summary>
        static readonly List<StaticInstance> _staticInstances = new();

        /// <summary>
        /// Key: type, Value: instance.
        /// In the first pass instances are null.
        /// Instances are constructed later.
        /// In the final pass instance will never be null.
        /// </summary>
        static readonly Dictionary<Type, DynamicInstance> _dynamicInstances = new();

        static readonly List<IInitializable> _initializables = new();

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
            FirstPass(assemblies);

            // goes through all static instances and inject into fields that need other static instances
            // todo: when we are at GameLogicViewModel
            // todo: it has one field GameLogicMainController _mainController
            // todo: this field is then identified as static even tho it is not - it has a parameterized constructor
            SecondPass(assemblies);
        }

        public static void BindModel<T>(object model) => _boundModels.Add(typeof(T), model);

        public static void ResolveBindings()
        {
            // create instances
            foreach (KeyValuePair<Type, DynamicInstance> kvp in _dynamicInstances)
            {
                ParameterInfo[] ctorParams = kvp.Value.Parameters;
                ConstructorInfo ctor = kvp.Value.Constructor;

                object[] paramValues = new object[ctorParams.Length];

                for (int i = 0; i < ctorParams.Length; i++)
                {
                    Type type = ctorParams[i].ParameterType;

                    if (_boundModels.TryGetValue(type, out object value))
                        paramValues[i] = value;
                    else
                    {
                        // todo: probably invalid state
                    }
                }

                object instance = ctor.Invoke(paramValues);
                kvp.Value.Instance = instance;
            }

            // todo: something is wrong here
            // inject instances
            foreach (StaticInstance staticInstance in _staticInstances)
                foreach (FieldInfo info in staticInstance.DynamicDependencies)
                    if (_dynamicInstances.TryGetValue(info.FieldType, out DynamicInstance dynamicInstance))
                        // field is static therefore we pass null as the instance
                        info.SetValue(null, dynamicInstance.Instance);

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

        /// <summary>
        /// Creates static instances.
        /// calls SignalService.AddReactiveInstantiatable(instance); on these instances.
        /// adds them to Initializable list
        /// For dynamic instances only creates entries without the instance.
        /// </summary>
        static void FirstPass(Assembly[] assemblies)
        {
            Type injectAttribute = typeof(InjectAttribute);
            var interfacesForLater = new List<Type>(); // field type

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // ignore internal classes, enums
                    if (type.IsEnum || type.IsNested || type.IsInterface)
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
                        FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                        StaticInstance? staticInstance;

                        // search for interfaces
                        foreach (FieldInfo info in fields)
                        {
                            // is injectable
                            if (Attribute.GetCustomAttributes(info, injectAttribute, false).Length == 0)
                                continue;

                            if (info.FieldType.IsInterface)
                            {
                                // is on the static instance list
                                staticInstance = _staticInstances.Find(si => si.Type == info.FieldType);

                                // schedule for later
                                if (staticInstance == null)
                                    interfacesForLater.Add(info.FieldType);
                                else
                                    staticInstance.BoundInterface = info.FieldType;
                            }
                            else if (info.FieldType.IsArray)
                            {
                                Type elementType = info.FieldType.GetElementType()!;

                                // array of interfaces
                                if (elementType.IsInterface)
                                    // injection at this point is not possible as we don't know all the possibilities
                                    // is on the static instance list
                                    // we must schedule it for later
                                    interfacesForLater.Add(elementType);
                            }
                        }

                        // constructor zero
                        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

                        if (constructors.Length == 0) // must be present
                            throw new Exception($"{type.Name} has no parameterless constructor. Please add one with the attribute [Preserve].");

                        ConstructorInfo constructor = constructors[0];
                        ParameterInfo[] ctorParams = constructor.GetParameters();

                        // check if constructor injection
                        if (ctorParams.Length > 0)
                        {
                            // we know this type is dynamic, but we do not have the instance yet
                            _dynamicInstances.Add(type, new DynamicInstance(constructor, ctorParams));
                        }
                        else
                        {
                            // normal construction
                            object instance = constructors[0].Invoke(new object[] { });

                            staticInstance = _staticInstances.Find(si => si.Type == type);
                            if (staticInstance != null)
                                throw new ArgumentException("Binding the same element twice is not allowed.");

                            _staticInstances.Add(new StaticInstance(type, instance));
                            SignalService.AddReactiveInstantiatable(instance);

                            if (typeof(IInitializable).IsAssignableFrom(type))
                                _initializables.Add((IInitializable)instance);
                        }
                    }
                }

            foreach (Type fieldType in interfacesForLater)
                // can be more than one
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (StaticInstance staticInstance in _staticInstances)
                {
                    // go through type's interfaces and if any of them matches fieldType then add it to the bound
                    Type[] interfaces = staticInstance.Type.GetInterfaces();
                    Type? _ = interfaces.FirstOrDefault(type => type == fieldType);

                    if (_ != null)
                        staticInstance.BoundInterface = fieldType;
                }
        }

        /// <summary>
        /// Inject static instances into fields that needs them.
        /// Other fields are ignored.
        /// </summary>
        static void SecondPass(Assembly[] assemblies)
        {
            // Archer and Warrior should be able to be injected in this pass

            Type injectAttribute = typeof(InjectAttribute);

            foreach (Assembly asm in assemblies)
                foreach (Type type in asm.GetTypes())
                {
                    // go through all fields
                    FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (int i = 0; i < fields.Length; i++)
                    {
                        // we go through all injectable fields
                        FieldInfo info = fields[i];

                        // is injectable
                        if (Attribute.GetCustomAttributes(info, injectAttribute, false).Length == 0)
                            continue;

                        Type fieldType = info.FieldType;

                        if (fieldType.IsInterface)
                        {
                            List<StaticInstance> instances = _staticInstances.FindAll(si => si.BoundInterface != null && si.BoundInterface == fieldType);

                            // zero: dynamic, 1: static, more than one: invalid state
                            Assert.IsTrue(instances.Count is 0 or 1, "Found more than one matching instances. Should be zero or one.");

                            // must be dynamic
                            if (instances.Count == 0)
                                AddDynamicDependency(type, info);
                            else
                                info.SetValue(type, instances[0].Instance);

                            continue;
                        }

                        // is an array or a generic list
                        bool isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
                        if (fieldType.IsArray || isList)
                        {
                            Type t = isList ? fieldType.GetGenericArguments()[0] : fieldType.GetElementType()!;

                            // if on the dynamic list - add it
                            if (_dynamicInstances.TryGetValue(t, out DynamicInstance dynamicInstance))
                                dynamicInstance.DynamicDependencies.Add(info);

                            IEnumerable<object> instances = from si in _staticInstances
                                                            where t.IsInterface ? si.BoundInterface == t : si.Type == t
                                                            select si.Instance;

                            // direct injection to bypass type security check
                            Type t1 = type;
                            info.SetValueDirect(__makeref(t1), isList ? instances : instances.ToArray());
                        }
                        else // is a normal field
                        {
                            List<StaticInstance> instances = _staticInstances.FindAll(si => si.Type == fieldType);

                            // zero: dynamic, 1: static, more than one: invalid state
                            Assert.IsTrue(instances.Count is 0 or 1, "Found more than one matching instances. Should be one.");

                            if (instances.Count == 0) // must be dynamic
                                AddDynamicDependency(type, info);
                            else
                                info.SetValue(type, instances[0].Instance);
                        }
                    }
                }
        }

        static void AddDynamicDependency(Type type, FieldInfo fieldInfo)
        {
            // the field is not a dynamic dependency
            if (!_dynamicInstances.TryGetValue(fieldInfo.FieldType, out DynamicInstance _))
                throw new Exception("Invalid program state.");

            // check if the class is a dynamic dependency
            if (_dynamicInstances.TryGetValue(type, out DynamicInstance dynamicInstance))
            {
                dynamicInstance.DynamicDependencies.Add(fieldInfo);
            }
            else
            {
                // must be in static 
                StaticInstance staticInstance = _staticInstances.Find(si => si.Type == type);
                staticInstance.DynamicDependencies.Add(fieldInfo);
            }
        }
    }
}