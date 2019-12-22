using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderMethodGenerator
    {
        private static readonly Regex ClosureTypeDeclarationRegex = new Regex(@"(?<=private sealed class )<\w*>[\w_]+", RegexOptions.Compiled);
        private static readonly Regex LambdaMethodDeclarationRegex = new Regex(@"(private|internal) void <\w+>[\w_|]+(?=\()", RegexOptions.Compiled);

        private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();
        private static readonly Dictionary<Type, Compilation> decompiledTypes = new Dictionary<Type, Compilation>();

        private static Compilation globalCompilation;

        static ShaderMethodGenerator()
        {
            if (string.IsNullOrEmpty(Assembly.GetEntryAssembly().Location))
            {
                throw new PlatformNotSupportedException("Shader method generation in AOT compiled apps is not supported.");
            }

            var assemblyPaths = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location);
            var metadataReferences = assemblyPaths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();

            globalCompilation = CSharpCompilation.Create("ShaderAssembly").WithReferences(metadataReferences);

            AppDomain.CurrentDomain.AssemblyLoad += OnCurrentDomainAssemblyLoad;
        }

        public static IList<Type> GetDependentTypes(MethodInfo methodInfo)
        {
            Compilation compilation = GetCompilation(methodInfo.DeclaringType);
            MethodDeclarationSyntax methodNode = GetMethodDeclaration(methodInfo, compilation.SyntaxTrees.Single());

            ShaderSyntaxCollector syntaxCollector = new ShaderSyntaxCollector(compilation);
            syntaxCollector.Visit(methodNode.Body);

            return syntaxCollector.CollectedTypes.Distinct().ToList();
        }

        public static string GetMethodBody(MethodInfo methodInfo)
        {
            Compilation compilation = GetCompilation(methodInfo.DeclaringType);
            MethodDeclarationSyntax methodNode = GetMethodDeclaration(methodInfo, compilation.SyntaxTrees.Single());

            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(compilation);
            SyntaxNode newBody = syntaxRewriter.Visit(methodNode.Body);

            string shaderSource = newBody.ToFullString();

            // TODO: See why the System namespace in System.Math is not present in UWP projects.
            shaderSource = shaderSource.Replace("Math.Max", "max");
            shaderSource = shaderSource.Replace("Math.Pow", "pow");
            shaderSource = shaderSource.Replace("Math.Sin", "sin");

            shaderSource = shaderSource.Replace("vector", "vec");
            shaderSource = Regex.Replace(shaderSource, @"\d+[fF]", m => m.Value.Replace("f", ""));

            return shaderSource.Replace(Environment.NewLine + "    ", Environment.NewLine).Trim();
        }

        private static void OnCurrentDomainAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            if (!e.LoadedAssembly.IsDynamic)
            {
                PortableExecutableReference metadataReference = MetadataReference.CreateFromFile(e.LoadedAssembly.Location);
                globalCompilation = globalCompilation.AddReferences(metadataReference);
            }
        }

        private static Compilation GetCompilation(Type type)
        {
            lock (decompiledTypes)
            {
                if (!decompiledTypes.TryGetValue(type, out Compilation compilation))
                {
                    EntityHandle handle = MetadataTokenHelpers.TryAsEntityHandle(type.MetadataToken) ?? throw new InvalidOperationException();
                    string assemblyPath = type.Assembly.Location;

                    if (!decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                    {
                        decompiler = CreateDecompiler(assemblyPath);
                        decompilers.Add(assemblyPath, decompiler);
                    }

                    string sourceCode = decompiler.DecompileAsString(handle);

                    sourceCode = ClosureTypeDeclarationRegex.Replace(sourceCode, ShaderGenerator.DelegateTypeName);
                    sourceCode = LambdaMethodDeclarationRegex.Replace(sourceCode, $"internal void {ShaderGenerator.DelegateEntryPointName}");

                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, CSharpParseOptions.Default.WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8));
                    compilation = globalCompilation.AddSyntaxTrees(syntaxTree);

                    decompiledTypes.Add(type, compilation);
                }

                return compilation;
            }
        }

        private static MethodDeclarationSyntax GetMethodDeclaration(MethodInfo methodInfo, SyntaxTree syntaxTree)
        {
            SyntaxNode root = syntaxTree.GetRoot();

            MethodDeclarationSyntax methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .First(n => (n.Identifier.ValueText == methodInfo.Name || n.Identifier.ValueText == ShaderGenerator.DelegateEntryPointName)
                    && n.ParameterList.Parameters.Count == methodInfo.GetParameters().Length);

            return methodNode;
        }

        private static CSharpDecompiler CreateDecompiler(string assemblyPath)
        {
            UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard");

            DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp8_0)
            {
                ObjectOrCollectionInitializers = false,
                UsingDeclarations = false
            };

            decompilerSettings.CSharpFormattingOptions.IndentationString = "    ";

            return new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);
        }
    }
}
