#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Core.Services
{
    public static class CodeValidationService
    {
        const BindingFlags Flags = BindingFlags.NonPublic
                                   | BindingFlags.Public
                                   | BindingFlags.Static
                                   | BindingFlags.DeclaredOnly
                                   | BindingFlags.Instance;

        static readonly List<string> _assemblyNames = new()
        {
            "Boot",
            "Core",
            "GameLogic",
            "Presentation",
            "UI"
        };

        static readonly List<string> _nameSpaces = new()
        {
            "Collections",
            "Config",
            "Controllers",
            "Data",
            "Dtos",
            "Editor",
            "Enums",
            "Services",
            "ViewModels",
            "Views"
        };

        /// <summary>
        /// Order matches the execution order in the engine.
        /// </summary>
        static readonly List<string> _lifeCycleDeclarationOrder = new()
        {
            "Awake",
            "OnEnable",
            "Reset",
            "OnValidate",
            "Start",
            "FixedUpdate",
            "OnTriggerEnter",
            "OnTriggerStay",
            "OnTriggerExit",
            "OnCollisionEnter",
            "OnCollisionStay",
            "OnCollisionExit",
            "Update",
            "LateUpdate",
            "OnStateMachineEnter",
            "OnStateMachineExit",
            "OnAnimatorMove",
            "OnAnimatorIK",
            "WriteProperties",
            "OnPreCull",
            "OnBecameVisible",
            "OnBecameInvisible",
            "OnWillRenderObject",
            "OnPreRender",
            "OnRenderObject",
            "OnPostRender",
            "OnRenderImage",
            "OnDrawGizmos",
            "OnGUI",
            "OnApplicationPause",
            "OnApplicationQuit",
            "OnDisable",
            "OnDestroy",
        };

        static readonly List<string> _dotsMethods = new()
        {
            "OnCreate",
            "OnUpdate"
        };

        static readonly List<string> _autoGeneratedDotsMethods = new()
        {
            "CompareJob",
            "ReceiveJob",
            "ResolvedChunk",
            "SendJob",
            "SendRpc",
            "TypeHandle"
        };

        /// <summary>
        /// Job of this system is find code inconsistencies and convention violations that can be found via Reflection.
        /// And then list them all in a readable format with explanation how to fix them.
        /// This system is meant to reduce the workflow put on coded reviewers by making the code more self-cleaning.
        /// </summary>
        /// <param name="additionalAssemblies">If the project has more assemblies than the standard 7, add them here.</param>
        public static void Validate(List<string>? additionalAssemblies = null)
        {
            ValidateConfigs(out HashSet<Type> createdConfigs);

            string currentTypeName = "";
            List<MethodInfo> methodsInCurrentClass = new();
            List<string> usedSignals = new();
            Type iSignal = null!;

            foreach (Assembly asm in GetAssemblies(additionalAssemblies))
            {
                string assemblyName = asm.GetName().Name;

                foreach (Type type in asm.GetTypes())
                {
                    // Exceptions for types names:
                    // - don't validate this class auto-generated by Unity
                    // - don't validate this class auto-generated by DOTS
                    // - don't validate auto-generated types with a name starting with '<' || '_'
                    // - don't validate auto-generated DOTS files with a name contains 'DataOriented_Generated' || 'LambdaJob'
                    char first = type.Name[0];
                    if (type.Name == "UnitySourceGeneratedAssemblyMonoScriptTypes_v1"
                        || type.Name == @"$BurstDirectCallInitializer"
                        || first == '<' || first == '_'
                        || type.Name.Contains("DataOriented_Generated")
                        || type.Name.Contains("LambdaJob"))
                        continue;

                    // checks if all our classes and interfaces have namespace
                    if (type.Namespace == null)
                    {
                        if (type.IsClass || type.IsInterface)
                            Debug.LogError($"{assemblyName}:{type.Name} {(type.IsClass ? "class" : "interface")} has no namespace. "
                                           + "The namespace should match the folder structure."
                                           + "f.e. if PlayerView is in Presentation/Views folder therefore the namespace should be Presentation.Views");
                    }
                    else
                    {
                        if (type.Name.EndsWith("Controller"))
                        {
                            ValidateNamespace(type, "Controllers", type.Namespace);
                            ValidateAccessModifier(type, type.Namespace);
                        }

                        if (type.Name.EndsWith("Service"))
                        {
                            ValidateNamespace(type, "Services", type.Namespace);
                            ValidateAccessModifier(type, type.Namespace);
                        }

                        if (type.Name.EndsWith("View"))
                        {
                            ValidateNamespace(type, "Views", type.Namespace);
                            ValidateAccessModifier(type, type.Namespace);
                        }

                        if (type.Name.EndsWith("Config") && type.IsSubclassOf(typeof(ScriptableObject)))
                            if (!createdConfigs.Contains(type))
                                Debug.LogError($"{assemblyName}:{type.Name} don't have instance in resources, all configs should have one instance");
                    }

                    if (type.Name == "ISignal")
                        iSignal = type;

                    foreach (FieldInfo field in type.GetFields(Flags))
                        ValidateField(field, assemblyName, type);

                    foreach (MethodInfo method in type.GetMethods(Flags))
                    {
                        first = method.Name[0];
                        // Exceptions for methods:
                        // - don't validate auto-generated setter/getter
                        // - don't validate auto-generated methods with a name starting with '<' || '_'
                        if (method.IsSpecialName || first == '<' || first == '_')
                            continue;

                        //collect all used methods with react attribute
                        if (type.Name.EndsWith("Controller") || type.Name.EndsWith("Service"))
                            if (method.CustomAttributes.Any(e => e.AttributeType.Name == "ReactAttribute"))
                                usedSignals.Add(method.Name[2..]);

                        ValidateMethod(method, assemblyName, type);

                        if (currentTypeName != type.Name)
                        {
                            methodsInCurrentClass.Clear();
                            currentTypeName = type.Name;
                        }

                        // this method is auto-generated at the end of each system causing a method declaration order errors
                        if (method.Name == "OnCreateForCompiler")
                            continue;

                        methodsInCurrentClass.Add(method);
                    }

                    MethodDeclarationOrderCheck(methodsInCurrentClass, assemblyName, type);
                }
            }

            ValidateISignal(iSignal, usedSignals);
        }

        static IEnumerable<Assembly> GetAssemblies(IReadOnlyCollection<string>? additionalAssemblies)
        {
            List<string> assemblyToLoad = new();
            assemblyToLoad.AddRange(_assemblyNames);
            if (additionalAssemblies != null)
                assemblyToLoad.AddRange(additionalAssemblies);

            var assemblies = new Assembly[assemblyToLoad.Count];
            for (int i = 0; i < assemblyToLoad.Count; i++)
                assemblies[i] = Assembly.Load(assemblyToLoad[i]);

            return assemblies;
        }

        /// <summary>
        /// Checks if the type's namespace is correct.<br/>
        /// - Controllers should have a namespace does have "Controllers" at any level.<br/>
        /// - Systems that are in a namespace that does not have "Systems" at any level.<br/>
        /// - Views that are in a namespace that does not have "Views" at any level.<br/>
        /// (exception when directly in the root).<br/>
        /// <param name="type"></param>
        /// <param name="correctNamespace">Namespace that this type should be in</param>
        /// <param name="namespaceName"></param>
        /// </summary>
        static void ValidateNamespace(MemberInfo type, string correctNamespace, string namespaceName)
        {
            string[] names = namespaceName.Split('.');
            if (names.Length == 1)
                return;

            if (names.Any(name => name == correctNamespace))
                return;

            foreach (string name in names)
                if (_nameSpaces.Contains(name))
                    // todo: this error is not very helpful
                    // todo: the summary tells more about the method than this error
                    Debug.LogError($"{type.Name} in {namespaceName} is not in correct namespace. "
                                   + $"This namespace should have nested {correctNamespace} or can't be in {name}");
        }

        /// <summary>
        /// Checks if the type's access modifier is correct.<br/>
        /// Controllers, Systems and Views should have maximum internal access modifier.<br/>
        /// Exception is everything in Core/Shared assembly and Systems in DataOriented assembly. <br/>
        /// <param name="type"></param>
        /// <param name="namespaceName"></param>
        /// </summary>
        static void ValidateAccessModifier(Type type, string namespaceName)
        {
            // Exceptions:
            // - don't validate 'Core' and 'Shared' assembly
            // - don't validate Systems in 'DataOriented' assembly
            if (namespaceName.Contains("Core") || namespaceName.Contains("Shared"))
                return;

            if (type.IsPublic)
                Debug.LogError($"{type.Name} in {namespaceName} has a 'public' access modifier and should have a maximum 'internal'");
        }

        /// <summary>
        /// Validates:<br/>
        /// - duplicated configs (instantiated more than once)<br/>
        /// - configs containing reference fields that are null or collections that are empty
        /// </summary>
        /// <param name="createdConfigs">List of unique configs</param>
        static void ValidateConfigs(out HashSet<Type> createdConfigs)
        {
            UnityEngine.Object[] configs = Resources.LoadAll("Configs");
            // Using sets to check if element is duplicated (faster approach)
            createdConfigs = new HashSet<Type>();
            foreach (UnityEngine.Object config in configs)
            {
                Type type = config.GetType();
                if (createdConfigs.Contains(type))
                    Debug.LogError($"Config \" {type.Name} \" is duplicated. "
                                   + "One must be removed to ensure that dependency injector will work fine.");

                createdConfigs.Add(type);

                foreach (FieldInfo field in type.GetFields(Flags))
                    if (field.GetValue(config) == null)
                        Debug.LogError($"Config \" {type.Name} \" contains null value in {field.Name}");
            }
        }

        /// <summary>
        /// Checks if the signals are declared alphabetically.
        /// Checks if there is at least one <see cref="ReactAttribute"/> for each signal declared.
        /// </summary>
        static void ValidateISignal(IReflect type, ICollection<string> usedSignals)
        {
            MethodInfo[] signals = type.GetMethods(Flags);
            for (int i = 0; i < signals.Length; i++)
            {
                // Check if signals are in alphabetical order
                if (i < signals.Length - 1 && StringComparer.OrdinalIgnoreCase.Compare(signals[i].Name, signals[i + 1].Name) > 0)
                    Debug.LogError($"Signal \"{signals[i + 1].Name}\" is defined after \"{signals[i].Name}\" "
                                   + "which don't match alphabetical order. Signals should be defined alphabetically for better readability.");

                // Check if every signal is used
                if (!usedSignals.Contains(signals[i].Name))
                    Debug.LogError($"Signal \"{signals[i].Name}\" does not have a corresponding [React] method. "
                                   + "Either add a method or delete the signal.");
            }
        }

        /// <summary>
        /// Check if const/public/internal variable is named with upper letter.<br/>
        /// Check if private variable starts with _ and small letter.<br/>
        /// Check if controller is static private and readonly.<br/>
        /// Check if config is static private and readonly.<br/>
        /// </summary>
        [SuppressMessage("ReSharper", "MergeIntoPattern")]
        static void ValidateField(FieldInfo field, string assemblyName, MemberInfo type)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            Attribute? attribute = Attribute.GetCustomAttribute(type, typeof(SerializableAttribute), false);
            bool isSerialized = attribute != null;
            char first = field.Name[0];

            // Exceptions for fields:
            // - don't validate auto-generated variables with a name "value__"
            // - don't validate auto-generated variables with a name starting with '<'
            if (field.Name == "value__" || first == '<')
                return;

            // - don't validate variables in auto-generated DOTS methods
            if (_autoGeneratedDotsMethods.Any(name => type.Name == name))
                return;

            // todo: temporary fix, AudioConfig is internal for now but will be private once Music and Sound Systems are move to Common (autogen)
            if (field.Name == "AudioConfig")
                return; // this is super exceptional case will be handled later

            // for some reason public fields with a name starting with a prefix "On" are returned as private
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (field.IsPrivate && field.Name.StartsWith("On"))
                return;

            // char.IsLower returns false for upper letters and non letters
            if (field.IsPrivate && !field.IsConst())
                if (first != '_' || char.IsUpper(field.Name[1]))
                    VariableNameError("Private variables should start with an underscore and small letter.");

            if (field.IsConst() || field.IsAssembly)
            {
                if (char.IsLower(first))
                    VariableNameError($"{(field.IsConst() ? "Constants" : "Internal")} variables should start with a capital letter.");
            }
            else if (field.IsPublic && !isSerialized) // ignore Serialized classes as for compatibility reasons they have to start with lower-case
                if (char.IsLower(first))
                    VariableNameError("Public variables should start with a capital letter.");

            // configs and injectables should be private, readonly, and static
            if (field.FieldType.IsSubclassOf(typeof(ScriptableObject)) && field.FieldType.Name.EndsWith("Config"))
                if (!field.IsPrivate || !field.IsStatic || !field.IsInitOnly)
                    VariableNameError("Configs should be private, readonly, and static.");

            // injectables should be private, readonly, and static
            // ReSharper disable once InvertIf
            if (Attribute.GetCustomAttributes(field, typeof(InjectAttribute), false).Length > 0)
                if (!field.IsPrivate || !field.IsStatic)
                    VariableNameError("Controllers should be private and static.");

            return;

            void VariableNameError(string msg)
            {
                Debug.LogError($"Variable \"{field.Name}\" in {assemblyName}:{type.Name} has invalid name. {msg}");
            }
        }

        /// <summary>
        /// Check if the method's name starts with an upper letter.
        /// Check if each of the method's parameters starts with a lower letter
        /// </summary>
        static void ValidateMethod(MethodBase method, string assemblyName, MemberInfo type)
        {
            if (!char.IsUpper(method.Name[0]))
                Debug.LogError($"Method \"{method.Name}\" in {assemblyName}:{type.Name} class has wrong name. "
                               + "Methods should start with a capital letter.");

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                // Exceptions for parameters:
                // - don't validate parameters with a name '_' || "_<number>"
                if (parameter.Name == '_'.ToString()
                    || parameter.Name[0] == '_' && int.TryParse(parameter.Name[1..], out _))
                    continue;

                if (!char.IsLower(parameter.Name[0]))
                    Debug.LogError($"Parameter \"{parameter.Name}\" in {assemblyName}: class \"{type.Name}\", "
                                   + $"method \"{method.Name}\" has wrong name. Parameters should start with a lower letter.");
            }
        }

        /// <summary>
        /// Checks if <see cref="ReactAttribute"/> methods are next to each other (declared in a continuous block).<br/>
        /// Checks if <see cref="ReactAttribute"/> methods are declared in alphabetical order.<br/>
        /// Checks if methods are in correct order public -> internal -> private.<br/>
        /// </summary>
        static void MethodDeclarationOrderCheck(IReadOnlyList<MethodInfo> methods, string asmName, MemberInfo type)
        {
            List<int> publicIndexes = new();
            List<int> internalIndexes = new();
            List<int> privateIndexes = new();
            List<int> signalsIndexes = new();

            for (int i = 0; i < methods.Count; i++)
            {
                if (type.Name.EndsWith("Controller") || type.Name.EndsWith("System"))
                    if (methods[i].CustomAttributes.Any(e => e.AttributeType.Name == "ReactAttribute"))
                        signalsIndexes.Add(i);

                if (methods[i].IsPublic)
                    publicIndexes.Add(i);
                else if (methods[i].IsAssembly)
                    internalIndexes.Add(i);
                else
                    privateIndexes.Add(i);
            }

            foreach (int index in privateIndexes)
            {
                // Check if any private method is declared higher than public method
                if (publicIndexes.Any(i => index < i))
                    MethodDeclarationOrderError(methods[index].Name, asmName, type.Name);

                // Check if any private method is declared higher than internal method
                if (internalIndexes.Any(i => index < i))
                    MethodDeclarationOrderError(methods[index].Name, asmName, type.Name);
            }

            // Check if any internal method is declared higher than public method
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (int index in internalIndexes)
                if (publicIndexes.Any(i => index < i))
                    MethodDeclarationOrderError(methods[index].Name, asmName, type.Name);

            for (int i = 0; i < signalsIndexes.Count - 1; i++)
            {
                int index = signalsIndexes[i];
                int nextIndex = signalsIndexes[i + 1];
                if (nextIndex - index > 1)
                    Debug.LogError($"Signal \"{methods[index].Name}\" in {asmName}:{type.Name} "
                                   + "is inconsistent with the order of declarations. All signals must be next to each other");

                if (StringComparer.OrdinalIgnoreCase.Compare(methods[index].Name, methods[nextIndex].Name) > 0)
                    Debug.LogError($"Signal \"{methods[nextIndex].Name}\" in {asmName}:{type.Name} is defined after \"{methods[index].Name}\""
                                   + " which don't match alphabetical order");
            }
        }

        static void MethodDeclarationOrderError(string methodName, string asmName, string typeName)
        {
            if (_lifeCycleDeclarationOrder.Any(s => methodName == s)
                || _dotsMethods.Any(s => methodName == s))
                return;

            Debug.LogError($"Method \"{methodName}\" in {asmName}:{typeName} "
                           + "is inconsistent with the order of declarations. The order of declarations should be \"public > internal > private\"");
        }
    }
}
#endif