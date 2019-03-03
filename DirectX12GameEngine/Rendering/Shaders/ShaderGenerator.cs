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
    public static class ShaderGenerator
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

            HlslBindingTracker bindingTracker = new HlslBindingTracker();

            Dictionary<Type, List<ResourceDefinition>> alreadyWrittenTypes = new Dictionary<Type, List<ResourceDefinition>>();

            foreach (Type staticType in Assemblies.SelectMany(a => a.GetTypes()).Where(t => t.IsDefined(typeof(StaticShaderClassAttribute))))
            {
                WriteStructure(writer, staticType, null, alreadyWrittenTypes, bindingAttr);
            }

            foreach (Type nestedType in shaderType.GetNestedTypesInTypeHierarchy(bindingAttr))
            {
                WriteStructure(writer, nestedType, null, alreadyWrittenTypes, bindingAttr);
            }

            foreach (MemberInfo memberInfo in shaderType.GetMembersInOrder(bindingAttr).Where(m => m.IsDefined(typeof(ShaderResourceAttribute))))
            {
                Type? memberType = memberInfo.GetMemberType(shader);

                if (memberType != null)
                {
                    WriteStructure(writer, memberType, memberInfo.GetMemberValue(shader), alreadyWrittenTypes, bindingAttr);
                }

                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    WriteMethod(writer, methodInfo);
                }
                else if (memberType != null && resourceType != null)
                {
                    WriteResource(writer, memberInfo, memberType, resourceType, alreadyWrittenTypes, bindingTracker);
                }
            }

            foreach (MethodInfo shaderMethodInfo in shaderType.GetMethods(bindingAttr).Where(m => m.IsDefined(typeof(ShaderAttribute))))
            {
                ShaderAttribute shaderAttribute = shaderMethodInfo.GetCustomAttribute<ShaderAttribute>();
                result.SetShader(shaderAttribute.Name, shaderMethodInfo);
            }

            stringWriter.GetStringBuilder().TrimEnd();
            writer.WriteLine();

            result.ShaderSource = stringWriter.ToString();

            return result;
        }

        private static void WriteStructure(IndentedTextWriter writer, Type type, object? obj, Dictionary<Type, List<ResourceDefinition>> alreadyWrittenTypes, BindingFlags bindingAttr)
        {
            type = GetElementOrDeclaredType(type);

            if (HlslKnownTypes.ContainsKey(type) || alreadyWrittenTypes.ContainsKey(type)) return;

            alreadyWrittenTypes.Add(type, new List<ResourceDefinition>());

            var memberInfos = type.GetMembersInOrder(bindingAttr).Where(m => m.IsDefined(typeof(ShaderResourceAttribute)));

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);

                if (memberType != null)
                {
                    WriteStructure(writer, memberType, memberInfo.GetMemberValue(obj), alreadyWrittenTypes, bindingAttr);

                    ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                    if (resourceType != null)
                    {
                        alreadyWrittenTypes[type].Add(new ResourceDefinition(memberType, resourceType));
                    }
                }
            }

            if (type.IsEnum)
            {
                writer.WriteLine($"enum class {type.Name}");
            }
            else
            {
                writer.WriteLine($"struct {type.Name}");
            }

            writer.WriteLine("{");
            writer.Indent++;

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    WriteMethod(writer, methodInfo);
                }
                else if (memberType != null && resourceType != null)
                {
                    if (type.IsEnum)
                    {
                        writer.Write(memberInfo.Name);
                        writer.WriteLine(",");
                    }
                    else
                    {
                        WriteStructureField(writer, memberInfo, memberType);
                    }
                }
            }

            (writer.InnerWriter as StringWriter)?.GetStringBuilder().TrimEnd();

            writer.Indent--;
            writer.WriteLine();
            writer.WriteLine("};");
            writer.WriteLine();

            if (type.IsEnum) return;

            foreach (MemberInfo memberInfo in memberInfos.Where(m => m.IsStatic()))
            {
                Type? memberType = memberInfo.GetMemberType(obj);

                if (memberType != null)
                {
                    WriteStaticStructureField(writer, memberInfo, memberType);
                }
            }
        }

        private static void WriteStructureField(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType)
        {
            if (memberInfo.IsStatic())
            {
                writer.Write("static");
                writer.Write(" ");
            }

            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");

            int arrayCount = memberType.IsArray ? 2 : 0;
            writer.Write(GetArrayString(arrayCount));

            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private static void WriteStaticStructureField(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType)
        {
            string declaringType = HlslKnownTypes.GetMappedName(memberInfo.DeclaringType);
            writer.WriteLine($"static {HlslKnownTypes.GetMappedName(memberType)} {declaringType}::{memberInfo.Name};");
            writer.WriteLine();
        }

        private static void WriteResource(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, ShaderResourceAttribute resourceType, Dictionary<Type, List<ResourceDefinition>> alreadyWrittenTypes, HlslBindingTracker bindingTracker)
        {
            switch (resourceType)
            {
                case ConstantBufferResourceAttribute _:
                    WriteConstantBuffer(writer, memberInfo, memberType, bindingTracker.ConstantBuffer++);
                    break;
                case SamplerResourceAttribute _:
                    WriteSampler(writer, memberInfo, memberType, bindingTracker.Sampler++);
                    break;
                case Texture2DResourceAttribute _:
                    WriteTexture2D(writer, memberInfo, memberType, bindingTracker.Texture++);
                    break;
                case StaticResourceAttribute _:
                    WriteStaticResource(writer, memberInfo, memberType, alreadyWrittenTypes, bindingTracker);
                    break;
                default:
                    throw new NotSupportedException("This shader resource type is not supported.");
            }
        }

        private static void WriteConstantBuffer(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            int arrayCount = memberType.IsArray ? 2 : 0;

            writer.Write($"cbuffer {memberInfo.Name}Buffer");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.WriteLine($" : register(b{binding})");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}{GetArrayString(arrayCount)};");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        private static void WriteSampler(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(s{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private static void WriteTexture2D(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(t{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private static void WriteStaticResource(IndentedTextWriter writer, MemberInfo memberInfo, Type memberType, Dictionary<Type, List<ResourceDefinition>> alreadyWrittenTypes, HlslBindingTracker bindingTracker)
        {
            List<MemberInfo> generatedMemberInfos = new List<MemberInfo>();

            foreach (ResourceDefinition resourceDefinition in alreadyWrittenTypes[memberType])
            {
                MemberInfo generatedMemberInfo = new FakeMemberInfo($"__Generated__{bindingTracker.StaticResource++}__");
                generatedMemberInfos.Add(generatedMemberInfo);

                WriteResource(writer, generatedMemberInfo, resourceDefinition.MemberType, resourceDefinition.ResourceType, alreadyWrittenTypes, bindingTracker);
            }

            writer.Write($"static {HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));

            if (generatedMemberInfos.Count != 0)
            {
                writer.Write(" = { ");

                foreach (MemberInfo generatedMemberInfo in generatedMemberInfos)
                {
                    writer.Write(generatedMemberInfo.Name);
                    writer.Write(", ");
                }

                writer.Write("}");
            }

            writer.WriteLine(";");
            writer.WriteLine();
        }

        private static string GetArrayString(int arrayCount) => arrayCount > 0 ? $"[{arrayCount}]" : "";

        private static Type GetElementOrDeclaredType(this Type type) => type.IsArray ? type.GetElementType() : type;

        private static string GetHlslSemantic(ShaderSemanticAttribute? semanticType) => semanticType switch
        {
            PositionSemanticAttribute a => " : Position" + a.Index,
            NormalSemanticAttribute a => " : Normal" + a.Index,
            TextureCoordinateSemanticAttribute a => " : TexCoord" + a.Index,
            ColorSemanticAttribute a => " : Color" + a.Index,
            TangentSemanticAttribute a => " : Tangent" + a.Index,
            SystemTargetSemanticAttribute a => " : SV_Target" + a.Index,
            SystemIsFrontFaceSemanticAttribute _ => " : SV_IsFrontFace",
            SystemInstanceIdSemanticAttribute _ => " : SV_InstanceId",
            SystemPositionSemanticAttribute _ => " : SV_Position",
            SystemRenderTargetArrayIndexSemanticAttribute _ => " : SV_RenderTargetArrayIndex",
            _ => ""
        };

        private static void WriteMethod(IndentedTextWriter writer, MethodInfo methodInfo)
        {
            List<MethodInfo> methodInfos = new List<MethodInfo>() { methodInfo };
            int counter = 1;

            if (methodInfo.IsOverride())
            {
                MethodInfo? currentMethodInfo = methodInfo.DeclaringType.BaseType?.GetMethod(methodInfo.Name);

                while (currentMethodInfo != null)
                {
                    methodInfos.Add(currentMethodInfo);
                    currentMethodInfo = currentMethodInfo.DeclaringType.BaseType?.GetMethod(currentMethodInfo.Name);
                    counter++;
                }
            }

            for (int depth = counter - 1; depth >= 0; depth--)
            {
                CSharpDecompiler decompiler = CSharpDecompilers.GetMappedDecompiler(methodInfos[depth].DeclaringType.Assembly.Location);

                EntityHandle methodHandle = MetadataTokenHelpers.TryAsEntityHandle(methodInfos[depth].MetadataToken) ?? throw new Exception();
                string sourceCode = decompiler.DecompileAsString(methodHandle);

                sourceCode = sourceCode.Replace("vector", "vec");
                sourceCode = Regex.Replace(sourceCode, @"\d+[fF]", m => m.Value.Replace("f", ""));

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                SyntaxNode root = syntaxTree.GetRoot();

                Compilation = Compilation.AddSyntaxTrees(syntaxTree);
                SemanticModel semanticModel = Compilation.GetSemanticModel(syntaxTree);

                ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(semanticModel, depth);
                root = syntaxRewriter.Visit(root);

                string shaderSource = root.ToFullString();

                // TODO: See why the System namespace in System.Math is not present in UWP projects.
                shaderSource = shaderSource.Replace("Math.Max", "max");
                shaderSource = shaderSource.Replace("Math.Pow", "pow");

                // Indent every line
                string indent = "";

                for (int i = 0; i < writer.Indent; i++)
                {
                    indent += IndentedTextWriter.DefaultTabString;
                }

                shaderSource = shaderSource.Replace(Environment.NewLine, Environment.NewLine + indent).TrimEnd(' ');

                writer.WriteLine(shaderSource);
            }
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
                { typeof(Numerics.Vector4).FullName, "float4" },
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
                { "System.Math.Pow", "pow" },
                { "System.Math.PI", "3.1415926535897931" },
                { "System.MathF.PI", "3.14159274f" },

                { "System.Numerics.Vector3.X", ".x" },
                { "System.Numerics.Vector3.Y", ".y" },
                { "System.Numerics.Vector3.Z", ".z" },
                { "System.Numerics.Vector3.Cross", "cross" },
                { "System.Numerics.Vector3.Dot", "dot" },
                { "System.Numerics.Vector3.Lerp", "lerp" },
                { "System.Numerics.Vector3.Transform", "mul" },
                { "System.Numerics.Vector3.TransformNormal", "mul" },
                { "System.Numerics.Vector3.Normalize", "normalize" },
                { "System.Numerics.Vector3.Zero", "(float3)0" },
                { "System.Numerics.Vector3.One", "float3(1.0f, 1.0f, 1.0f)" },
                { "System.Numerics.Vector3.UnitX", "float3(1.0f, 0.0f, 0.0f)" },
                { "System.Numerics.Vector3.UnitY", "float3(0.0f, 1.0f, 0.0f)" },
                { "System.Numerics.Vector3.UnitZ", "float3(0.0f, 0.0f, 1.0f)" },

                { "System.Numerics.Vector4.X", ".x" },
                { "System.Numerics.Vector4.Y", ".y" },
                { "System.Numerics.Vector4.Z", ".z" },
                { "System.Numerics.Vector4.W", ".w" },
                { "System.Numerics.Vector4.Lerp", "lerp" },
                { "System.Numerics.Vector4.Transform", "mul" },
                { "System.Numerics.Vector4.Normalize", "normalize" },
                { "System.Numerics.Vector4.Zero", "(float4)0" },
                { "System.Numerics.Vector4.One", "float4(1.0f, 1.0f, 1.0f, 1.0f)" },

                { "System.Numerics.Matrix4x4.Multiply", "mul" },
                { "System.Numerics.Matrix4x4.Transpose", "transpose" },
                { "System.Numerics.Matrix4x4.Translation", "[3].xyz" },
                { "System.Numerics.Matrix4x4.M11", "[0][0]" },
                { "System.Numerics.Matrix4x4.M12", "[0][1]" },
                { "System.Numerics.Matrix4x4.M13", "[0][2]" },
                { "System.Numerics.Matrix4x4.M14", "[0][3]" },
                { "System.Numerics.Matrix4x4.M21", "[1][0]" },
                { "System.Numerics.Matrix4x4.M22", "[1][1]" },
                { "System.Numerics.Matrix4x4.M23", "[1][2]" },
                { "System.Numerics.Matrix4x4.M24", "[1][3]" },
                { "System.Numerics.Matrix4x4.M31", "[2][0]" },
                { "System.Numerics.Matrix4x4.M32", "[2][1]" },
                { "System.Numerics.Matrix4x4.M33", "[2][2]" },
                { "System.Numerics.Matrix4x4.M34", "[2][3]" },
                { "System.Numerics.Matrix4x4.M41", "[3][0]" },
                { "System.Numerics.Matrix4x4.M42", "[3][1]" },
                { "System.Numerics.Matrix4x4.M43", "[3][2]" },
                { "System.Numerics.Matrix4x4.M44", "[3][3]" }
            };

            public static string? GetMappedName(ISymbol containingMemberSymbol, ISymbol memberSymbol)
            {
                string fullTypeName = containingMemberSymbol.IsStatic ? containingMemberSymbol.ToString() : memberSymbol.ContainingType.ToString();

                if (knownMethods.TryGetValue(fullTypeName + Type.Delimiter + memberSymbol.Name, out string mapped))
                {
                    if (!memberSymbol.IsStatic)
                    {
                        return containingMemberSymbol.Name + mapped;
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
            public int StaticResource { get; set; }
        }

        private class ResourceDefinition
        {
            public ResourceDefinition(Type memberType, ShaderResourceAttribute resourceType)
            {
                MemberType = memberType;
                ResourceType = resourceType;
            }

            public Type MemberType { get; }
            public ShaderResourceAttribute ResourceType { get; }
        }

        private class FakeMemberInfo : MemberInfo
        {
            public FakeMemberInfo(string name)
            {
                Name = name;
            }

            public override Type DeclaringType => throw new NotImplementedException();

            public override MemberTypes MemberType => MemberTypes.Field;

            public override string Name { get; }

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit)
            {
                return Array.Empty<object>();
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return Array.Empty<object>();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                return false;
            }
        }
    }
}
