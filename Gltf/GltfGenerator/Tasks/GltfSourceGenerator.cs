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
        public string? GltfSchema { get; set; } = @"..\Gltf\specification\2.0\schema\glTF.schema.json";

        public void Execute(SourceGeneratorContext context)
        {
            try
            {
                CodeGenerator generator = new CodeGenerator(GltfSchema);
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
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create("GLTF1", "Compiler", e.Message, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1));
            }
        }

        public void Initialize(InitializationContext context)
        {
        }
    }
}
