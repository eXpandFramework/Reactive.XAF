using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xpand.Extensions.LinqExtensions;

namespace Xpand.Extensions.Compiler{
    public static class CodeCompiler {
        public static MemoryStream Compile(this SyntaxTree syntaxTree, params string[] references){
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
                new[]{
                    // "mscorlib",
                    // "netstandard", 
                    // "System.Collections", 
                    "System.Runtime",
                    // "System.Drawing",
                    "System.Private.CoreLib"
                    
                }.Contains(assembly.GetName().Name));
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