using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DirectX12GameEngine.Rendering.Shaders
{
    internal static class ShaderGenerator
    {
        public static Assembly[] Assemblies { get; } = AppDomain.CurrentDomain.GetAssemblies();
        public static IEnumerable<MetadataReference> MetadataReferences { get; }
        public static CSharpCompilation Compilation { get; private set; }

        static ShaderGenerator()
        {
            MetadataReferences = Assemblies.Where(a => !a.IsDynamic).Select(a => MetadataReference.CreateFromFile(a.Location));
            Compilation = CSharpCompilation.Create("ShaderAssembly").WithReferences(MetadataReferences);
        }

        public static ShaderGenerationResult GenerateShaderSource(object shader, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        {
            StringWriter stringWriter = new StringWriter();
            IndentedTextWriter writer = new IndentedTextWriter(stringWriter);

            ShaderGenerationResult result = new ShaderGenerationResult();

            Type shaderType = shader.GetType();

            HlslBindingTracker tracker = new HlslBindingTracker();
            HashSet<Type> alreadyWrittenTypes = new HashSet<Type>();

            var staticClasses = Assemblies.SelectMany(a => a.GetTypes()).Where(t => t.IsDefined(typeof(StaticShaderClassAttribute)));

            foreach (Type staticType in staticClasses)
            {
                WriteStructure(writer, staticType, alreadyWrittenTypes, bindingAttr);
            }

            foreach (Type nestedType in shaderType.GetNestedTypesInTypeHierarchy(bindingAttr))
            {
                WriteStructure(writer, nestedType, alreadyWrittenTypes, bindingAttr);
            }

            foreach (MemberInfo memberInfo in shaderType.GetMembersInOrder(bindingAttr))
            {
                if (memberInfo.IsDefined(typeof(ShaderResourceAttribute)))
                {
                    Type? memberType = memberInfo.GetMemberType(shader);

                    if (memberType != null)
                    {
                        WriteStructure(writer, memberType, alreadyWrittenTypes, bindingAttr);
                    }

                    ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                    if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                    {
                        WriteMethod(writer, methodInfo);
                    }
                    else if (memberType != null && resourceType != null)
                    {
                        WriteResource(writer, memberInfo, memberType, resourceType, tracker);
                    }
                }
            }

            var shaderMethodInfos = shaderType.GetMethods(bindingAttr).Where(m => m.IsDefined(typeof(ShaderAttribute)));

            foreach (MethodInfo shaderMethodInfo in shaderMethodInfos)
            {
                ShaderAttribute shaderAttribute = shaderMethodInfo.GetCustomAttribute<ShaderAttribute>();
                result.SetShader(shaderAttribute.Name, shaderMethodInfo);
            }

            stringWriter.GetStringBuilder().TrimEnd();
            writer.WriteLine();

            result.ShaderSource = stringWriter.ToString();

            return result;
        }

        private static void WriteStructure(IndentedTextWriter writer, Type type, ISet<Type> alreadyWrittenTypes, BindingFlags bindingAttr)
        {
            type = GetElementOrDeclaredType(type);

            if (HlslKnownTypes.ContainsKey(type) || !alreadyWrittenTypes.Add(type)) return;

            var memberInfos = type.GetMembersInOrder(bindingAttr);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                if (memberInfo.IsDefined(typeof(ShaderResourceAttribute)))
                {
                    Type? memberType = memberInfo.GetMemberType();

                    if (memberType != null)
                    {
                        WriteStructure(writer, memberType, alreadyWrittenTypes, bindingAttr);
                    }
                }
            }

            writer.WriteLine($"struct {type.Name}");
            writer.WriteLine("{");
            writer.Indent++;

            HlslSemanticTracker tracker = new HlslSemanticTracker();

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType();
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    WriteMethod(writer, methodInfo);
                }
                else if (memberType != null && resourceType != null)
                {
                    WriteStructureField(writer, memberInfo, memberType, tracker);
                }
            }

            (writer.InnerWriter as StringWriter)?.GetStringBuilder().TrimEnd();

            writer.Indent--;
            writer.WriteLine();
            writer.WriteLine("};");
            writer.WriteLine();

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType();
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (memberType != null && resourceType is StaticResourceAttribute)
                {
                    WriteStaticResource(writer, memberInfo, memberType);
                }
            }
        }

        private static void WriteStructureField(IndentedTextWriter writer, MemberInfo memberInfo, Type type, HlslSemanticTracker tracker)
        {
            if (memberInfo.IsStatic())
            {
                writer.Write("static");
                writer.Write(' ');
            }

            writer.Write(HlslKnownTypes.GetMappedName(type));
            writer.Write(' ');
            writer.Write(memberInfo.Name);

            int arrayCount = type.IsArray ? 2 : 0;
            writer.Write(WriteArray(arrayCount));

            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>(), tracker));
            writer.WriteLine(';');
            writer.WriteLine();
        }

        private static void WriteResource(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, ShaderResourceAttribute resourceType, HlslBindingTracker tracker)
        {
            switch (resourceType)
            {
                case ConstantBufferResourceAttribute _:
                    WriteConstantBuffer(writer, memberInfo, memberType, tracker.ConstantBuffer++);
                    break;
                case SamplerResourceAttribute _:
                    WriteSampler(writer, memberInfo, memberType, tracker.Sampler++);
                    break;
                case Texture2DResourceAttribute _:
                    WriteTexture2D(writer, memberInfo, memberType, tracker.Texture++);
                    break;
                case StaticResourceAttribute _:
                    WriteStaticResource(writer, memberInfo, memberType);
                    break;
            }
        }

        private static void WriteConstantBuffer(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            int arrayCount = memberType.IsArray ? 2 : 0;

            writer.WriteLine($"cbuffer {memberInfo.Name}Buffer : register(b{binding})");
            writer.WriteLine("{");
            writer.WriteLine($"    {HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}{WriteArray(arrayCount)};");
            writer.WriteLine("}");
            writer.WriteLine();
        }

        private static void WriteSampler(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name} : register(s{binding});");
            writer.WriteLine();
        }

        private static void WriteStaticResource(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType)
        {
            string declaringType = memberInfo.IsStatic() ? HlslKnownTypes.GetMappedName(memberInfo.DeclaringType) + "::" : "";
            writer.WriteLine($"static {HlslKnownTypes.GetMappedName(memberType)} {declaringType}{memberInfo.Name};");
            writer.WriteLine();
        }

        private static void WriteTexture2D(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name} : register(t{binding});");
            writer.WriteLine();
        }

        private static string WriteArray(int arrayCount) => arrayCount > 0 ? $"[{arrayCount}]" : "";

        private static Type GetElementOrDeclaredType(this Type type) => type.IsArray ? type.GetElementType() : type;

        private static string GetHlslSemantic(ShaderSemanticAttribute? semanticType, HlslSemanticTracker tracker) => semanticType switch
        {
            PositionSemanticAttribute _ => " : Position" + tracker.Position++,
            NormalSemanticAttribute _ => " : Normal" + tracker.Normal++,
            TextureCoordinateSemanticAttribute _ => " : TexCoord" + tracker.TexCoord++,
            ColorSemanticAttribute _ => " : Color" + tracker.Color++,
            TangentSemanticAttribute _ => " : Tangent" + tracker.Tangent++,
            SystemPositionSemanticAttribute _ => " : SV_Position",
            SystemInstanceIdSemanticAttribute _ => " : SV_InstanceId",
            SystemRenderTargetArrayIndexSemanticAttribute _ => " : SV_RenderTargetArrayIndex",
            SystemTargetSemanticAttribute _ => " : SV_Target" + tracker.SystemTarget++,
            _ => ""
        };

        private static void WriteMethod(IndentedTextWriter writer, MethodInfo methodInfo)
        {
            CSharpDecompiler decompiler = CSharpDecompilers.GetMappedDecompiler(methodInfo.DeclaringType.Assembly.Location);

            EntityHandle methodHandle = MetadataTokenHelpers.TryAsEntityHandle(methodInfo.MetadataToken) ?? throw new Exception();
            string sourceCode = decompiler.DecompileAsString(methodHandle);

            sourceCode = sourceCode.Replace("vector", "vec");
            sourceCode = Regex.Replace(sourceCode, @"\d+[fF]", m => m.Value.Replace("f", ""));

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = syntaxTree.GetRoot();

            Compilation = Compilation.AddSyntaxTrees(syntaxTree);
            SemanticModel semanticModel = Compilation.GetSemanticModel(syntaxTree);

            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(semanticModel);
            root = syntaxRewriter.Visit(root);

            string shaderSource = root.ToFullString();

            // Indent every line
            string indent = "";

            for (int i = 0; i < writer.Indent; i++)
            {
                indent += IndentedTextWriter.DefaultTabString;
            }

            shaderSource = shaderSource.Replace(Environment.NewLine, Environment.NewLine + indent).TrimEnd(' ');

            writer.WriteLine(shaderSource);
        }

        internal static class CSharpDecompilers
        {
            private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();

            public static CSharpDecompiler GetMappedDecompiler(string assemblyPath)
            {
                if (!decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                {
                    UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard2.0");

                    DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)
                    {
                        UsingDeclarations = false
                    };

                    decompilerSettings.CSharpFormattingOptions.IndentationString = IndentedTextWriter.DefaultTabString;

                    decompiler = new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);

                    decompilers.Add(assemblyPath, decompiler);
                }

                return decompiler;
            }
        }

        internal static class HlslKnownTypes
        {
            private static readonly Dictionary<string, string> knownTypes = new Dictionary<string, string>()
            {
                { typeof(void).FullName, "void" },
                { typeof(bool).FullName, "bool" },
                { typeof(uint).FullName, "uint" },
                { typeof(int).FullName, "int" },
                { typeof(double).FullName, "double" },
                { typeof(float).FullName, "float" },
                { typeof(Vector2).FullName, "float2" },
                { typeof(Vector3).FullName, "float3" },
                { typeof(Vector4).FullName, "float4" },
                { typeof(Matrix4x4).FullName, "float4x4" },
                { typeof(SamplerResource).FullName, "SamplerState" },
                { typeof(SamplerComparisonResource).FullName, "SamplerComparisonState" },
                { typeof(Texture2DResource).FullName, "Texture2D" },
                { typeof(Texture2DArrayResource).FullName, "Texture2DArray" },
                { typeof(TextureCubeResource).FullName, "TextureCube" }
            };

            public static string GetMappedName(Type type)
            {
                string typeFullName = type.GetElementOrDeclaredType().FullName.Replace('+', '.');
                string typeName = type.GetElementOrDeclaredType().Name;
                return knownTypes.TryGetValue(typeFullName, out string mapped) ? mapped : typeName;
            }

            public static string GetMappedName(string name)
            {
                return knownTypes.TryGetValue(name, out string mapped) ? mapped : Regex.Match(name, @"[^\.]+$").Value;
            }

            public static bool ContainsKey(Type type)
            {
                return knownTypes.ContainsKey(type.GetElementOrDeclaredType().FullName.Replace('+', '.'));
            }

            public static bool ContainsKey(string name)
            {
                return knownTypes.ContainsKey(name);
            }
        }

        internal static class HlslKnownMethods
        {
            private static readonly Dictionary<string, string> knownMethods = new Dictionary<string, string>()
            {
                { "System.Math.Max", "max" },
                { "System.Math.PI", "3.1415926535897931" },
                { "System.MathF.PI", "3.14159274f" },

                { "System.Numerics.Vector3.Dot", "dot" },
                { "System.Numerics.Vector3.Normalize", "normalize" },
                { "System.Numerics.Vector3.Transform", "mul" },
                { "System.Numerics.Vector3.TransformNormal", "mul" },
                { "System.Numerics.Vector3.Zero", "(float3)0" },

                { "System.Numerics.Vector4.X", "x" },
                { "System.Numerics.Vector4.Y", "y" },
                { "System.Numerics.Vector4.Z", "z" },
                { "System.Numerics.Vector4.W", "w" },
                { "System.Numerics.Vector4.Transform", "mul" },
                { "System.Numerics.Vector4.Zero", "(float4)0" }
            };

            public static string? GetMappedName(ISymbol containingMemberSymbol, ISymbol memberSymbol)
            {
                string fullTypeName = containingMemberSymbol.IsStatic ? containingMemberSymbol.ToString() : memberSymbol.ContainingType.ToString();

                if (knownMethods.TryGetValue(fullTypeName + Type.Delimiter + memberSymbol.Name, out string mapped))
                {
                    if (!memberSymbol.IsStatic)
                    {
                        return containingMemberSymbol.ToString() + Type.Delimiter + mapped;
                    }

                    return mapped;
                }

                if (memberSymbol.IsStatic)
                {
                    return containingMemberSymbol.Name + "::" + memberSymbol.Name;
                }

                return null;
            }
        }

        private class HlslBindingTracker
        {
            public int ConstantBuffer { get; set; }
            public int Sampler { get; set; }
            public int Texture { get; set; }
        }

        private class HlslSemanticTracker
        {
            public int Position { get; set; }
            public int TexCoord { get; set; }
            public int Normal { get; set; }
            public int Tangent { get; set; }
            public int Color { get; set; }
            public int SystemTarget { get; set; }
        }
    }
}
