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

        public ISet<ITypeSymbol> CollectedTypes { get; } = new HashSet<ITypeSymbol>();

        private SemanticModel GetSemanticModel(SyntaxNode node) => compilation.GetSemanticModel(node.SyntaxTree);

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

            if (!node.TryGetMappedMemberName(GetSemanticModel(node).GetSymbolInfo(node.Name), out ISymbol memberSymbol, out _))
            {
                if (memberSymbol.ContainingType != null)
                {
                    CollectedTypes.Add(memberSymbol.ContainingType);
                }
            }
        }
    }
}
