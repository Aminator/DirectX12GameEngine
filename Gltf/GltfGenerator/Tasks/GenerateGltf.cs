using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

#nullable enable

namespace GltfGenerator.Tasks
{
    public class GenerateGltf : Task
    {
        [Required]
        public ITaskItem[]? Inputs { get; set; }

        [Required]
        public ITaskItem? GltfSchema { get; set; }

        [Output]
        public ITaskItem? Output { get; set; }

        public override bool Execute()
        {
            CodeGenerator generator = new CodeGenerator(GltfSchema!.ItemSpec);
            generator.ParseSchemas();
            generator.ExpandSchemaReferences();
            generator.EvaluateInheritance();
            generator.PostProcessSchema();
            generator.CSharpCodeGen(Output!.ItemSpec);

            return true;
        }
    }
}
