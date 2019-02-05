using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Rendering.Materials
{
    public static class SyntaxNodeExtensions
    {
        public static TRoot ReplaceType<TRoot>(this TRoot node, TypeSyntax type) where TRoot : SyntaxNode
        {
            string value = ShaderLoader.HlslKnownTypes.GetMappedName(type.ToString());

            if (value == type.ToString())
            {
                return node;
            }
            else
            {
                return node.ReplaceNode(type, SyntaxFactory.ParseTypeName(value).WithLeadingTrivia(type.GetLeadingTrivia()).WithTrailingTrivia(type.GetTrailingTrivia()));
            }
        }

        public static SyntaxNode ReplaceMethod(this SyntaxNode node)
        {
            string value = ShaderLoader.HlslKnownMethods.GetMappedName(node.ToString());

            if (value == node.ToString())
            {
                return node;
            }
            else
            {
                return SyntaxFactory.IdentifierName(value).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
            }
        }
    }
}
