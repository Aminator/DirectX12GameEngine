using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace GltfGenerator.Tasks
{
    [Generator]
    public class GltfSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.GltfGenerator_GltfSchemaPath", out var path))
                throw new InvalidOperationException("Missing GltfGenerator_GltfSchemaPath property in MSBuild file.");
            
            CodeGenerator generator = new CodeGenerator(path);
            generator.ParseSchemas();
            generator.ExpandSchemaReferences();
            generator.EvaluateInheritance();
            generator.PostProcessSchema();

            var generatedFiles = generator.CSharpCodeGen();

            foreach (var file in generatedFiles)
            {
                context.AddSource(file.Key, SourceText.From(file.Value, Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}
