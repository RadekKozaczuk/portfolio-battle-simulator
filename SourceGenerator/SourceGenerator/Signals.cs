using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace SourceGenerator;

static class Signals
{
    static readonly int Length = "        public static void () => ArchitectureService.Intercept(\"\", );".Length;

    internal static string Code(IEnumerable<SyntaxTree> syntaxTrees, out string prCode)
    {
        string usings = "";
        var implementationSb = new StringBuilder();
        var staticSb = new StringBuilder();
        var paramSb = new StringBuilder();
        var paramNamesSb = new StringBuilder();
        var signalNamesSb = new StringBuilder();
        var signalQueuesSb = new StringBuilder();
        int signalId = 0; // id and counter at the same time

        foreach (SyntaxTree tree in syntaxTrees)
        {
            IEnumerable<SyntaxNode> rootChildNodes = tree.GetRoot().ChildNodes();

            var namespaceDeclarationSyntaxes = rootChildNodes.OfType<NamespaceDeclarationSyntax>();
            if (!namespaceDeclarationSyntaxes.Any())
                continue;

            // we don't care about multi-namespace files as we know our file has only one so we take only the first one into account
            IEnumerable<SyntaxNode> childNodes = namespaceDeclarationSyntaxes.First().ChildNodes();

            // if null then instead of IdentifierNameSyntax there is either QualifiedNameSyntax for multi-word namespaces (f.e. Common.Dtos)
            // or there is no namespace at all
            // in both cases we can skip this file because we know our file will have one-word namespace
            if (childNodes.OfType<IdentifierNameSyntax>().FirstOrDefault() == null)
                continue;

            InterfaceDeclarationSyntax ids = childNodes.OfType<InterfaceDeclarationSyntax>().FirstOrDefault();

            // if null then it is not an interface
            if (ids == null)
                continue;

            // last check if the name matches
            string interfaceName = ids.ChildTokens().First(m => m.IsKind(SyntaxKind.IdentifierToken)).ValueText;
            if (interfaceName != "ISignal")
                continue;

            usings = ParseUsings(rootChildNodes);

            SyntaxList<MemberDeclarationSyntax> members = ids.Members;
            for (int i = 0; i < members.Count; i++)
            {
                MemberDeclarationSyntax member = members[i];
                IEnumerable<SyntaxToken> childTokens = member.ChildTokens();
                if (!childTokens.Any(m => m.IsKind(SyntaxKind.IdentifierToken)))
                    continue;

                paramSb.Clear();
                paramNamesSb.Clear();
                string methodName = childTokens.First(m => m.IsKind(SyntaxKind.IdentifierToken)).ValueText;
                var documentationComment = GetDocumentationComment(member);
                IEnumerable<ParameterListSyntax> parameterListSyntaxes = member.ChildNodes().OfType<ParameterListSyntax>();

                if (!parameterListSyntaxes.Any())
                    continue;

                SeparatedSyntaxList<ParameterSyntax> parameters = parameterListSyntaxes.First().Parameters;

                if (parameters.Count() == 0)
                {
                    // skip new line for the last element
                    if (i == members.Count - 1)
                        signalQueuesSb.Append($"            null   // {methodName}");
                    else
                        signalQueuesSb.AppendLine($"            null,  // {methodName}");
                }
                else
                {
                    for (int j = 0; j < parameters.Count; j++)
                    {
                        // skip new line for the last element
                        if (i == members.Count - 1 && j == parameters.Count - 1)
                            signalQueuesSb.Append("            new()");
                        else if (j == 0)
                            signalQueuesSb.AppendLine($"            new(), // {methodName}");
                        else
                            signalQueuesSb.AppendLine("            new(), ");

                        ParameterSyntax parameter = parameters[j];
                        string typeName = "";
                        // in case of primitive types it will be PredefinedTypeSyntax
                        if (parameter.Type is PredefinedTypeSyntax pts)
                            typeName = pts.Keyword.ValueText;
                        // in case of classes it will be IdentifierNameSyntax
                        else if (parameter.Type is IdentifierNameSyntax ins)
                            typeName = ins.Identifier.ValueText;
                        // in case of generics (touples, lists, etc.) - GenericNameSyntax
                        else if (parameter.Type is GenericNameSyntax gns)
                            typeName = ParseGenericName(gns);
                        // in case of arrays - ArrayTypeSyntax
                        else if (parameter.Type is ArrayTypeSyntax ats)
                            typeName = ParseArrayType(ats);

                        paramSb.Append(typeName).Append(" ");

                        string paramName = parameter.ChildTokens().First(m => m.IsKind(SyntaxKind.IdentifierToken)).ValueText;
                        paramSb.Append(paramName);
                        paramNamesSb.Append(paramName);

                        // skip coma for the last element
                        if (j < parameters.Count - 1)
                        {
                            paramSb.Append(", ");
                            paramNamesSb.Append(", ");
                        }
                    }
                }

                implementationSb.AppendLine($"            [Preserve]");

                // skip new line for the last element
                if (i < members.Count - 1)
                {
                    implementationSb.AppendLine($"            void ISignal.{methodName}({paramSb}) {{ }}");
                    implementationSb.AppendLine("");

                    if (!string.IsNullOrEmpty(documentationComment))
                        staticSb.AppendLine(documentationComment);

                    if (Length + methodName.Length * 2 + paramSb.Length + paramNamesSb.Length > 160)
                    {
                        staticSb.AppendLine($"        public static void {methodName}({paramSb}) =>");
                        staticSb.AppendLine($"            " + GetInterceptString(paramNamesSb, methodName, ref signalId));
                    }
                    else
                        staticSb.AppendLine($"        public static void {methodName}({paramSb}) => {GetInterceptString(paramNamesSb, methodName, ref signalId)}");

                    staticSb.AppendLine("");
                    signalNamesSb.AppendLine($"            \"{methodName}\",");
                }
                else
                {
                    implementationSb.Append($"            void ISignal.{methodName}({paramSb}) {{ }}");

                    if (!string.IsNullOrEmpty(documentationComment))
                        staticSb.AppendLine(documentationComment);

                    if (Length + methodName.Length * 2 + paramSb.Length + paramNamesSb.Length > 160)
                    {
                        staticSb.AppendLine($"        public static void {methodName}({paramSb}) =>");
                        staticSb.Append("            " + GetInterceptString(paramNamesSb, methodName, ref signalId));
                    }
                    else
                        staticSb.Append($"        public static void {methodName}({paramSb}) => {GetInterceptString(paramNamesSb, methodName, ref signalId)}");

                    signalNamesSb.Append($"            \"{methodName}\"");
                }
            }
        }

        prCode = CreatePrecalculatedValuesSource(signalId, signalNamesSb, signalQueuesSb);
        return CreateSource(usings, implementationSb, staticSb);

        static string GetInterceptString(StringBuilder paramNamesSb, string methodName, ref int signalId) =>
            $"ArchitectureService.Intercept({signalId++}, \"{methodName}\"{(paramNamesSb.Length > 0 ? $", {paramNamesSb}" : "")});";
    }

    static string ParseUsings(IEnumerable<SyntaxNode> rootChildNodes)
    {
        var sb = new StringBuilder();
        IEnumerable<UsingDirectiveSyntax> usings = rootChildNodes.OfType<UsingDirectiveSyntax>();

        foreach (UsingDirectiveSyntax uds in usings)
        {
            IEnumerable<SyntaxNode> nodes = uds.ChildNodes();
            IEnumerable<QualifiedNameSyntax> qualified = nodes.OfType<QualifiedNameSyntax>();

            if (qualified.Any())
            {
                string qualifier = ParseQualifiedUsing(qualified.First());
                sb.AppendLine($"using {qualifier};");
            }
            else
            {
                // this will happen only for usings with exactly one part
                IdentifierNameSyntax identifer = nodes.OfType<IdentifierNameSyntax>().First();
                sb.AppendLine($"using {identifer.Identifier.ValueText};");
            }
        }

        return sb.ToString();
    }

    // takes first level qualified syntax
    // this means that we went deeper at least once
    static string ParseQualifiedUsing(QualifiedNameSyntax root)
    {
        Stack<string> strings = new();
        StringBuilder sb = new();
        IEnumerable<SyntaxNode> childNodes = root.ChildNodes();
        QualifiedNameSyntax qualified = root;

        // null if the end of tree
        QualifiedNameSyntax? deepQualified = childNodes.OfType<QualifiedNameSyntax>().FirstOrDefault();

        // normally one, two at the end of tree
        IEnumerable<IdentifierNameSyntax> indentifier;

        while (deepQualified != null) // can go deeper
        {
            qualified = deepQualified;
            indentifier = childNodes.OfType<IdentifierNameSyntax>();
            childNodes = qualified.ChildNodes();

            strings.Push(indentifier.First().Identifier.ValueText);

            // null if the end of tree
            deepQualified = childNodes.OfType<QualifiedNameSyntax>().FirstOrDefault();
        }

        indentifier = qualified.ChildNodes().OfType<IdentifierNameSyntax>();

        strings.Push(indentifier.ElementAt(1).Identifier.ValueText);
        strings.Push(indentifier.ElementAt(0).Identifier.ValueText);

        do
        {
            sb.Append(strings.Pop());

            if (strings.Count > 0)
                sb.Append(".");
        }
        while (strings.Count > 0);

        return sb.ToString();
    }

    static string ParseGenericName(GenericNameSyntax gns)
    {
        var sb = new StringBuilder();

        sb.Append(gns.Identifier.ValueText);
        sb.Append("<");

        var typeArguments = gns.TypeArgumentList.Arguments;
        for (int i = 0; i < typeArguments.Count; i++)
        {
            var typeArgument = typeArguments[i];
            if (typeArgument is TupleTypeSyntax tupleType)
            {
                sb.Append("(");
                int count = tupleType.Elements.Count;
                for (int j = 0; j < count; j++)
                {
                    TupleElementSyntax element = tupleType.Elements[j];
                    var predefinedType = element.ChildNodes().OfType<PredefinedTypeSyntax>().First();
                    string keyword = predefinedType.Keyword.ValueText;
                    string name = element.Identifier.ValueText;

                    sb.Append(keyword + " " + name);

                    // Skip comma for the last element
                    if (j < count - 1)
                        sb.Append(", ");
                }
                sb.Append(")");
            }
            else
            {
                sb.Append(typeArgument.ToString());
            }

            // Skip comma for the last type argument
            if (i < typeArguments.Count - 1)
                sb.Append(", ");
        }

        sb.Append(">");
        return sb.ToString();
    }



    static string ParseArrayType(ArrayTypeSyntax ats)
    {
        var elementType = ats.ElementType.ToString();
        return $"{elementType}[]";
    }

    static string GetDocumentationComment(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia();
        var docCommentTrivia = trivia
            .Select(tr => tr.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (docCommentTrivia == null)
            return string.Empty;

        var indentedComment = docCommentTrivia.ToFullString().Replace(Environment.NewLine, Environment.NewLine);
        return $"\t\t{indentedComment.TrimEnd()}";
    }

    static string CreateSource(string usings, StringBuilder implementationSb, StringBuilder staticSb) =>
$@"{Constants.AutoGenerated}

#nullable enable // generated classes must explicitly enable nullable
using Core.Services;
using UnityEngine.Scripting;
{usings}
namespace Core
{{
    public static class Signals
    {{
        class SignalsImplementation : ISignal
        {{
{implementationSb}
        }}

{staticSb}
    }}
}}";

    static string CreatePrecalculatedValuesSource(int signalCount, StringBuilder signalNamesSb, StringBuilder signalQueuesSb) =>
$@"{Constants.AutoGenerated}

#nullable enable // generated classes must explicitly enable nullable
using System;
using System.Collections.Generic;

namespace Core
{{
    public static class SignalProcessorPrecalculatedArrays
    {{
        // used by SignalProcessor to preallocate internal arrays
        public const int SignalCount = {signalCount};

        // used by SignalProcessor to bind react methods
        public static readonly string[] SignalNames =
        {{
{signalNamesSb}
        }};

        /// <summary>
        /// This array stores signal runtime values for signals with parameters.
        /// Each signal can be represented by one or more queses depending on its parameters.
        /// For example, imagine we have two signals A and B. A is parameterless and B has two parameters of type int and string. <br/>
        /// null - Signal A (becasue parameterless signals are represented in this array as null)<br/>
        /// Queue(int) - Signal B <br/>
        /// Queue(string) - Signal B <br/><br/>
        /// First method/signal has no parameters and therefore is represented by one queue with boolean values in it. <br/>
        /// Second method/signal has two parameters (int and string) and is represented by two queues with respective value types.
        /// </summary>
        public static readonly Queue<object>?[] SignalQueues =
        {{
{signalQueuesSb}
        }};
    }}
}}";
}