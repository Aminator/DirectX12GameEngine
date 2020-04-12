using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DirectX12GameEngine.Shaders.Tasks
{
    public class GenerateShaders : Task
    {
        [Required]
        public ITaskItem[]? Inputs { get; set; }

        [Required]
        public ITaskItem[]? ReferencedAssemblies { get; set; }

        [Output]
        public ITaskItem? Output { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Generating shaders...");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var syntaxTrees = GetSyntaxTrees();
            var metadataReferences = GetMetadataReferences();

            Compilation compilation = CSharpCompilation.Create("ShaderAssembly", syntaxTrees, metadataReferences);

            INamedTypeSymbol? shaderMethodAttributeType = compilation.GetTypeByMetadataName(typeof(ShaderMethodAttribute).FullName);
            INamedTypeSymbol? anonymousShaderMethodAttributeType = compilation.GetTypeByMetadataName(typeof(AnonymousShaderMethodAttribute).FullName);

            var shaderMethodAttributes = GetAttributes(compilation)
                .Where(a => compilation.GetSemanticModel(a.SyntaxTree).GetTypeInfo(a).Type!.Equals(shaderMethodAttributeType, SymbolEqualityComparer.Default)
                    || compilation.GetSemanticModel(a.SyntaxTree).GetTypeInfo(a).Type!.Equals(anonymousShaderMethodAttributeType, SymbolEqualityComparer.Default));

            CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit();

            foreach (AttributeSyntax shaderMethodAttribute in shaderMethodAttributes)
            {
                MethodDeclarationSyntax? method = (MethodDeclarationSyntax?)shaderMethodAttribute.Parent?.Parent;
                if (method is null) continue;

                IMethodSymbol? methodSymbol = compilation.GetSemanticModel(method.SyntaxTree).GetDeclaredSymbol(method);
                if (methodSymbol is null) continue;

                ITypeSymbol attributeTypeSymbol = compilation.GetSemanticModel(shaderMethodAttribute.SyntaxTree).GetTypeInfo(shaderMethodAttribute).Type!;

                if (method.Body != null)
                {
                    if (attributeTypeSymbol.Equals(anonymousShaderMethodAttributeType, SymbolEqualityComparer.Default) && shaderMethodAttribute.ArgumentList!.Arguments.Count == 1)
                    {
                        if (shaderMethodAttribute.ArgumentList!.Arguments[0].Expression is LiteralExpressionSyntax literalExpression)
                        {
                            int anonymousMethodIndex = int.Parse(literalExpression.Token.ValueText, CultureInfo.InvariantCulture);

                            AnonymousFunctionExpressionSyntax anonymousFunction = method.Body.DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().ElementAt(anonymousMethodIndex);

                            compilationUnit = AddShaderMethod(compilationUnit, compilation, anonymousFunction.Body, methodSymbol, anonymousMethodIndex);
                        }
                    }
                    else if (attributeTypeSymbol.Equals(shaderMethodAttributeType, SymbolEqualityComparer.Default) && shaderMethodAttribute.ArgumentList is null)
                    {
                        compilationUnit = AddShaderMethod(compilationUnit, compilation, method.Body, methodSymbol);
                    }
                }
            }

            compilationUnit = compilationUnit.NormalizeWhitespace();

            Log.LogMessage($"{stopwatch.Elapsed}: Writing to file...");

            using (StreamWriter writer = new StreamWriter(Output!.ItemSpec))
            {
                compilationUnit.WriteTo(writer);
            }

            stopwatch.Stop();

            Log.LogMessage($"{stopwatch.Elapsed}: Shader generation complete.");

            return true;
        }

        private IEnumerable<SyntaxTree> GetSyntaxTrees()
        {
            if (Inputs is null) throw new InvalidOperationException();

            foreach (ITaskItem item in Inputs)
            {
                using FileStream stream = new FileStream(item.ItemSpec, FileMode.Open);
                SourceText sourceText = SourceText.From(stream);

                yield return CSharpSyntaxTree.ParseText(sourceText);
            }
        }

        private IEnumerable<MetadataReference> GetMetadataReferences()
        {
            if (ReferencedAssemblies is null) throw new InvalidOperationException();

            foreach (ITaskItem item in ReferencedAssemblies)
            {
                yield return MetadataReference.CreateFromFile(item.ItemSpec);
            }
        }

        private static IEnumerable<AttributeSyntax> GetAttributes(Compilation compilation)
        {
            return compilation.SyntaxTrees
                .SelectMany(syntaxTree => syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<AttributeSyntax>());
        }

        private static CompilationUnitSyntax AddShaderMethod(CompilationUnitSyntax compilationUnit, Compilation compilation, SyntaxNode methodBody, IMethodSymbol methodSymbol, int? anonymousMethodIndex = null)
        {
            string shaderMethodBody = ShaderMethodGenerator.GetMethodBody(compilation, methodBody);

            var dependentTypes = ShaderMethodGenerator.GetDependentTypes(compilation, methodBody);

            var parameterTypes = methodSymbol.Parameters
                .Select(p => SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(p.Type.ToString())))
                .ToArray();

            var dependentTypeArguments = dependentTypes
                .Select(t => SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(t.ToString())))
                .Select(e => SyntaxFactory.AttributeArgument(e))
                .ToArray();

            AttributeSyntax shaderMethodAttribute;

            if (anonymousMethodIndex.HasValue)
            {
                shaderMethodAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(GlobalAnonymousShaderMethodAttribute).FullName))
                    .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(anonymousMethodIndex.Value))));
            }
            else
            {
                shaderMethodAttribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(GlobalShaderMethodAttribute).FullName));
            }

            shaderMethodAttribute = shaderMethodAttribute
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(methodSymbol.ContainingType.ToString()))))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(methodSymbol.Name))))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.ArrayCreationExpression(
                    SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(typeof(Type).FullName)).AddRankSpecifiers(SyntaxFactory.ArrayRankSpecifier()),
                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression).AddExpressions(parameterTypes))))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(shaderMethodBody))))
                .AddArgumentListArguments(dependentTypeArguments);

            return compilationUnit.AddAttributeLists(SyntaxFactory.AttributeList()
                .WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)))
                .AddAttributes(shaderMethodAttribute));
        }
    }
}
