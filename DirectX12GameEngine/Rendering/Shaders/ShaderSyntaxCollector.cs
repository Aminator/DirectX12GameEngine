using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Rendering.Shaders
{
    public class ShaderSyntaxCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel semanticModel;
        private readonly ShaderGenerator shaderGenerator;

        public ShaderSyntaxCollector(ShaderGenerator shaderGenerator, SemanticModel semanticModel)
        {
            this.shaderGenerator = shaderGenerator;
            this.semanticModel = semanticModel;
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

            SymbolInfo containingMemberSymbolInfo = semanticModel.GetSymbolInfo(node.Expression);
            SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(node.Name);

            ISymbol? memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault();

            if (memberSymbol is null || (memberSymbol.Kind != SymbolKind.Field && memberSymbol.Kind != SymbolKind.Property && memberSymbol.Kind != SymbolKind.Method))
            {
                return;
            }

            ISymbol containingMemberSymbol = containingMemberSymbolInfo.Symbol;

            if (!ShaderGenerator.HlslKnownMethods.Contains(containingMemberSymbol, memberSymbol))
            {
                string fullTypeName = containingMemberSymbol.IsStatic ? containingMemberSymbol.ToString() : memberSymbol.ContainingType.ToString();
                shaderGenerator.AddType(Type.GetType(fullTypeName));
            }
        }
    }
}
