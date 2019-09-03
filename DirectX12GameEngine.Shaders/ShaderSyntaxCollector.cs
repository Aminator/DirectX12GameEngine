using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderSyntaxCollector : CSharpSyntaxWalker
    {
        private readonly Compilation compilation;
        private readonly ShaderGenerator shaderGenerator;

        public ShaderSyntaxCollector(Compilation compilation, ShaderGenerator shaderGenerator)
        {
            this.compilation = compilation;
            this.shaderGenerator = shaderGenerator;
        }

        private SemanticModel GetSemanticModel(SyntaxNode node) => compilation.GetSemanticModel(node.SyntaxTree);

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

            if (!node.TryGetMappedMemberName(GetSemanticModel(node), out ISymbol? memberSymbol, out _))
            {
                if (memberSymbol?.ContainingType != null)
                {
                    string fullTypeName = memberSymbol.ContainingType.ToDisplayString(
                        new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                        + ", " + memberSymbol.ContainingType.ContainingAssembly.Identity.ToString();

                    Type? type = Type.GetType(fullTypeName);

                    if (type != null)
                    {
                        shaderGenerator.AddType(type);
                    }
                }
            }
        }
    }
}
