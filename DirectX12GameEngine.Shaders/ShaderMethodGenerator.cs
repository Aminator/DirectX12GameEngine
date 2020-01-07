using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderMethodGenerator
    {
        private static readonly Regex AnonymousMethodDeclaringTypeDeclarationRegex = new Regex(@"(?<=private sealed class )<\w*>[\w_]+", RegexOptions.Compiled);
        private static readonly Regex AnonymousMethodDeclarationRegex = new Regex(@"(private|internal) void <\w+>[\w_|]+(?=\()", RegexOptions.Compiled);

        private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();
        private static readonly Dictionary<Type, Compilation> decompiledTypes = new Dictionary<Type, Compilation>();
        private static readonly Dictionary<string, MetadataReference> loadedMetadataReferences = new Dictionary<string, MetadataReference>();
        private static readonly Dictionary<string, List<MetadataReference>> loadedMetadataReferencesPerAssembly = new Dictionary<string, List<MetadataReference>>();

        public static string GetFullTypeName(ITypeSymbol typeSymbol)
        {
            string fullTypeName = typeSymbol.ToDisplayString(
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                + ", " + typeSymbol.ContainingAssembly.Identity.ToString();

            return fullTypeName;
        }

        public static ISet<ITypeSymbol> GetDependentTypes(MethodInfo methodInfo)
        {
            Compilation compilation = GetCompilation(methodInfo.DeclaringType);
            SyntaxNode methodBody = GetMethodBody(methodInfo, compilation.SyntaxTrees.Single());

            return GetDependentTypes(compilation, methodBody);
        }

        public static ISet<ITypeSymbol> GetDependentTypes(Compilation compilation, SyntaxNode methodBody)
        {
            ShaderSyntaxCollector syntaxCollector = new ShaderSyntaxCollector(compilation);
            syntaxCollector.Visit(methodBody);

            return syntaxCollector.CollectedTypes;
        }

        public static string GetMethodBody(MethodInfo methodInfo)
        {
            Compilation compilation = GetCompilation(methodInfo.DeclaringType);
            SyntaxNode methodBody = GetMethodBody(methodInfo, compilation.SyntaxTrees.Single());

            return GetMethodBody(compilation, methodBody);
        }

        public static string GetMethodBody(Compilation compilation, SyntaxNode methodBody)
        {
            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(compilation);
            SyntaxNode newBody = syntaxRewriter.Visit(methodBody);

            string shaderSource = newBody.ToFullString();

            return FormatShaderString(shaderSource);
        }

        public static SyntaxNode GetMethodBody(MethodInfo methodInfo, SyntaxTree syntaxTree)
        {
            SyntaxNode root = syntaxTree.GetRoot();

            MethodDeclarationSyntax methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .First(n => (n.Identifier.ValueText == methodInfo.Name || n.Identifier.ValueText == ShaderGenerator.AnonymousMethodEntryPointName)
                    && n.ParameterList.Parameters.Count == methodInfo.GetParameters().Length);

            return methodNode.Body!;
        }

        public static string FormatShaderString(string shaderString)
        {
            shaderString = shaderString.Replace("vector", "vec");
            shaderString = Regex.Replace(shaderString, @"\d+[fF]", m => m.Value.Replace("f", ""));

            StringBuilder indent = new StringBuilder("    ");

            for (int i = 0; i < shaderString.Length; i++)
            {
                if (shaderString[i] != ' ') break;

                indent.Append(' ');
            }

            return shaderString.Trim().Trim('{', '}').Trim().Replace(Environment.NewLine + indent.ToString(), Environment.NewLine);
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

                    sourceCode = AnonymousMethodDeclaringTypeDeclarationRegex.Replace(sourceCode, ShaderGenerator.AnonymousMethodDeclaringTypeName);
                    sourceCode = AnonymousMethodDeclarationRegex.Replace(sourceCode, $"internal void {ShaderGenerator.AnonymousMethodEntryPointName}");

                    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

                    IList<MetadataReference> metadataReferences = GetMetadataReferences(type.Assembly, type.Assembly.GetName(), typeof(object).Assembly.GetName());

                    compilation = CSharpCompilation.Create("ShaderAssembly", new[] { syntaxTree }, metadataReferences);

                    decompiledTypes.Add(type, compilation);
                }

                return compilation;
            }
        }

        private static IList<MetadataReference> GetMetadataReferences(Assembly assembly, params AssemblyName[] additionalAssemblyNames)
        {
            if (!loadedMetadataReferencesPerAssembly.TryGetValue(assembly.FullName, out List<MetadataReference> metadataReferences))
            {
                metadataReferences = new List<MetadataReference>();
                loadedMetadataReferencesPerAssembly.Add(assembly.FullName, metadataReferences);

                IEnumerable<AssemblyName> referencedAssemblies = assembly.GetReferencedAssemblies().Concat(additionalAssemblyNames);

                foreach (AssemblyName referencedAssemblyName in referencedAssemblies)
                {
                    if (referencedAssemblyName.Name == "netstandard")
                    {
                        metadataReferences.AddRange(GetMetadataReferences(Assembly.Load(referencedAssemblyName)));
                    }

                    metadataReferences.Add(GetMetadataReference(referencedAssemblyName));
                }
            }

            return metadataReferences;
        }

        private static MetadataReference GetMetadataReference(AssemblyName assemblyName)
        {
            if (!loadedMetadataReferences.TryGetValue(assemblyName.FullName, out MetadataReference metadataReference))
            {
                Assembly assembly = Assembly.Load(assemblyName);

                metadataReference = MetadataReference.CreateFromFile(assembly.Location);
                loadedMetadataReferences.Add(assemblyName.FullName, metadataReference);
            }

            return metadataReference;
        }

        private static CSharpDecompiler CreateDecompiler(string assemblyPath)
        {
            UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard");

            DecompilerSettings decompilerSettings = new DecompilerSettings()
            {
                ObjectOrCollectionInitializers = false
            };

            decompilerSettings.CSharpFormattingOptions.IndentationString = "    ";

            return new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);
        }
    }
}
