using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public static class SyntaxNodeExtensions
    {
        public static TRoot ReplaceType<TRoot>(this TRoot node, TypeSyntax type) where TRoot : SyntaxNode
        {
            string value = HlslKnownTypes.GetMappedName(type.ToString());

            if (value == type.ToString())
            {
                return node;
            }
            else
            {
                return node.ReplaceNode(type, SyntaxFactory.ParseTypeName(value).WithTriviaFrom(type));
            }
        }

        public static bool TryGetMappedMemberName(this MemberAccessExpressionSyntax node, SemanticModel semanticModel, out ISymbol memberSymbol, out string? mappedName)
        {
            SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(node.Name);

            memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault() ?? throw new InvalidOperationException();
            INamedTypeSymbol containingTypeSymbol = memberSymbol.ContainingType;

            mappedName = null;

            if (containingTypeSymbol != null)
            {
                string fullMemberName = containingTypeSymbol.ToString() + Type.Delimiter + node.Name.Identifier.ValueText;

                if (HlslKnownMethods.ContainsKey(fullMemberName))
                {
                    mappedName = HlslKnownMethods.GetMappedName(fullMemberName);
                    return true;
                }
            }

            return false;
        }
    }
}
