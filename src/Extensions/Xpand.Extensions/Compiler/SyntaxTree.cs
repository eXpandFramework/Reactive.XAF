using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Compiler{
    public static class CodeCompiler {
        public static MemoryStream Compile(this SyntaxTree syntaxTree, string[] references){
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
                new[]{"mscorlib", "netstandard", "System.Collections", "System.Runtime","System.Drawing"}.Contains(assembly.GetName().Name));
            // assemblies = new Assembly [0];
            var metadataReferences = assemblies.Select(assembly => assembly.Location).Concat(references)
                .Select(path => MetadataReference.CreateFromFile(path))
                .ToArray();
            var memoryStream = new MemoryStream();
            var cSharpCompilation = CSharpCompilation.Create(Guid.NewGuid().ToString(), new[]{syntaxTree},
                metadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var emitResult = cSharpCompilation.Emit(memoryStream);
            if(!emitResult.Success) {
                throw new InvalidOperationException(emitResult.Diagnostics.Select(diagnostic => diagnostic).Join(Environment.NewLine));
            }
            return memoryStream;
        }
    }

}