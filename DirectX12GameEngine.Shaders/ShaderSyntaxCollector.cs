using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderSyntaxCollector : CSharpSyntaxWalker
    {
        private readonly Compilation compilation;

        public ShaderSyntaxCollector(Compilation compilation)
        {
            this.compilation = compilation;
        }

        public IList<Type> CollectedTypes { get; } = new List<Type>();

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

                    CollectedTypes.Add(type);
                }
            }
        }
    }
}
