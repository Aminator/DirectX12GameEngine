using System.IO;
using GeneratorLib;

namespace Generator
{
    public class Program
    {
        private static void Main()
        {
            var generator = new CodeGenerator(@"..\..\..\..\Gltf\specification\2.0\schema\glTF.schema.json");
            generator.ParseSchemas();
            generator.ExpandSchemaReferences();
            generator.EvaluateInheritance();
            generator.PostProcessSchema();
            generator.CSharpCodeGen(Path.GetFullPath(@"..\..\..\..\GltfLoader\Schema"));
        }
    }
}
