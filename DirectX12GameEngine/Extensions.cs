using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Text;

namespace DirectX12GameEngine
{
    public static class DisposableExtensions
    {
        public static T DisposeBy<T>(this T item, ICollector collector) where T : IDisposable
        {
            collector.Disposables.Add(item);
            return item;
        }
    }

    public static class StringBuilderExtensions
    {
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb.Length == 0) return sb;

            int i;

            for (i = sb.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(sb[i]))
                {
                    break;
                }
            }

            if (i < sb.Length - 1)
            {
                sb.Length = i + 1;
            }

            return sb;
        }
    }

    public static class SyntaxNodeExtensions
    {
        public static TRoot ReplaceType<TRoot>(this TRoot node, TypeSyntax type) where TRoot : SyntaxNode
        {
            string value = ShaderLoader.HlslKnownTypes.GetMappedName(type.ToString());
            return node.ReplaceNode(type, SyntaxFactory.ParseTypeName(value).WithLeadingTrivia(type.GetLeadingTrivia()).WithTrailingTrivia(type.GetTrailingTrivia()));
        }

        public static IdentifierNameSyntax ReplaceMethod<TRoot>(this TRoot node) where TRoot : SyntaxNode
        {
            string value = ShaderLoader.HlslKnownMethods.GetMappedName(node.ToString());
            return SyntaxFactory.IdentifierName(value).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());
        }
    }
}
