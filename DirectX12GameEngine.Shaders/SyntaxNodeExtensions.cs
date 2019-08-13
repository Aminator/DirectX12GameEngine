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

        public static bool TryGetMappedMemberName(this MemberAccessExpressionSyntax node, SemanticModel semanticModel, out ISymbol? containingSymbol, out ISymbol? memberSymbol, out string? mappedName)
        {
            SymbolInfo containingMemberSymbolInfo = semanticModel.GetSymbolInfo(node.Expression);
            SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(node.Name);

            memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault();

            containingSymbol = null;
            mappedName = null;

            if (memberSymbol is null)
            {
                ISymbol? containingMemberSymbol = containingMemberSymbolInfo.Symbol ?? containingMemberSymbolInfo.CandidateSymbols.FirstOrDefault();

                if (containingMemberSymbol != null)
                {
                    containingSymbol = containingMemberSymbol switch
                    {
                        IFieldSymbol fieldSymbol => fieldSymbol.Type,
                        IPropertySymbol propertySymbol => propertySymbol.Type,
                        IMethodSymbol methodSymbol => methodSymbol.ContainingType,
                        _ => null
                    };
                }
            }
            else
            {
                containingSymbol = memberSymbol.ContainingSymbol;
            }


            if (containingSymbol is null) return false;

            string fullMemberName = containingSymbol.ToString() + Type.Delimiter + node.Name.Identifier.ValueText;

            if (HlslKnownMethods.ContainsKey(fullMemberName))
            {
                mappedName = HlslKnownMethods.GetMappedName(fullMemberName);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
