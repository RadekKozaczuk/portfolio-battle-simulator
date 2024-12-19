using Microsoft.CodeAnalysis;

#if RELEASE
using System.Text;
using Microsoft.CodeAnalysis.Text;
#endif

#if DEBUG
using System;
#endif

namespace SourceGenerator;

// both public and internal are called by all assemblies
// but Unity tutorial recommends internal as it apparenetly prevents some warnings from happening
// because all assemblies are iterated over, we have to filter out all assemblies but Core
[Generator]
class SourceGenerator : ISourceGenerator
{
    // this executes once per assembly - in the exactly same order as the one below
    public void Initialize(GeneratorInitializationContext context) { }

    // this also executes once per assembly - the order seems to be deterministic (Shared is always first then Core)
    public void Execute(GeneratorExecutionContext context)
    {
        string? asmName = context.Compilation.AssemblyName;

        if (asmName == null)
            return;

#if DEBUG
        if (asmName == "ExampleCodeToTest")
        {
            string code = Signals.Code(context.Compilation.SyntaxTrees, out string precalculated);
            Console.WriteLine(code);
            Console.ReadKey();
        }
#else
        if (asmName == "Core")
        {
            string code = Signals.Code(context.Compilation.SyntaxTrees, out string precalculated);
            context.AddSource("Signals.g.cs", SourceText.From(code, Encoding.UTF8));
            context.AddSource("PrecalculatedArrays.g.cs", SourceText.From(precalculated, Encoding.UTF8));
        }
#endif
    }
}