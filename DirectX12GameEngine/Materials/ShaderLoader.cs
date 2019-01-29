using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using DotNetDxc;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ShaderGen;
using SharpDX.D3DCompiler;

namespace DirectX12GameEngine
{
    internal static class ShaderLoader
    {
        static ShaderLoader()
        {
            HlslDxcLib.DxcCreateInstanceFn = DefaultDxcLib.GetDxcCreateInstanceFn();
        }

        public static byte[] CompileShaderFile(string filePath, ShaderVersion? version = null)
        {
            string shaderSource = File.ReadAllText(filePath);

            return CompileShader(shaderSource, version, filePath);
        }

        public static byte[] CompileShader(string shaderSource, ShaderVersion? version = null, string filePath = "")
        {
            IDxcCompiler compiler = HlslDxcLib.CreateDxcCompiler();
            IDxcLibrary library = HlslDxcLib.CreateDxcLibrary();

            const uint CP_UTF16 = 1200;
            IDxcBlobEncoding sourceBlob = library.CreateBlobWithEncodingOnHeapCopy(shaderSource, (uint)(shaderSource.Length * 2), CP_UTF16);

            IDxcOperationResult result = compiler.Compile(sourceBlob, filePath, GetDefaultEntryPoint(version), $"{GetShaderProfile(version)}_6_3", new[] { "-Zpr" }, 1, null, 0, library.CreateIncludeHandler());

            if (result.GetStatus() != 0)
            {
                IDxcBlob blob = result.GetResult();
                byte[] bytecode = GetBytesFromBlob(blob);

                return bytecode;
            }
            else
            {
                throw new Exception();
            }
        }

        public static byte[] CompileShaderLegacy(string shaderSource, ShaderVersion? version = null, string? entryPoint = null)
        {
            return ShaderBytecode.Compile(shaderSource, entryPoint ?? GetDefaultEntryPoint(version), $"{GetShaderProfile(version)}_5_1", ShaderFlags.PackMatrixRowMajor);
        }

        public static unsafe byte[] GetBytesFromBlob(IDxcBlob blob)
        {
            byte* pMem = (byte*)blob.GetBufferPointer();
            uint size = blob.GetBufferSize();
            byte[] result = new byte[size];

            fixed (byte* pTarget = result)
            {
                for (uint i = 0; i < size; i++)
                {
                    pTarget[i] = pMem[i];
                }
            }

            return result;
        }

        public static ShaderGenerationResult GenerateShaderSource(object shader, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
        {
            StringWriter stringWriter = new StringWriter();
            IndentedTextWriter writer = new IndentedTextWriter(stringWriter);

            ShaderGenerationResult result = new ShaderGenerationResult();

            Type shaderType = shader.GetType();

            HlslBindingTracker tracker = new HlslBindingTracker();

            foreach (Type nestedType in shaderType.GetNestedTypes(bindingAttr).Concat(shaderType.BaseType.GetNestedTypes(bindingAttr)))
            {
                WriteStructure(writer, nestedType, bindingAttr);
                writer.WriteLine();
            }

            foreach (FieldInfo fieldInfo in shaderType.GetFields(bindingAttr))
            {
                Type fieldType = fieldInfo.GetValue(shader)?.GetType() ?? fieldInfo.FieldType;

                if (!HlslKnownTypes.ContainsKey(fieldType))
                {
                    WriteStructure(writer, fieldType, bindingAttr | BindingFlags.DeclaredOnly);
                    writer.WriteLine();
                }

                WriteResource(writer, fieldInfo, fieldInfo.FieldType, ref tracker);
                writer.WriteLine();
            }

            foreach (PropertyInfo propertyInfo in shaderType.GetProperties(bindingAttr))
            {
                Type propertyType = propertyInfo.GetValue(shader)?.GetType() ?? propertyInfo.PropertyType;

                if (!HlslKnownTypes.ContainsKey(propertyType))
                {
                    WriteStructure(writer, propertyType, bindingAttr | BindingFlags.DeclaredOnly);
                    writer.WriteLine();
                }

                WriteResource(writer, propertyInfo, propertyType, ref tracker);
                writer.WriteLine();
            }

            foreach (MethodInfo methodInfo in shaderType.GetMethods(bindingAttr))
            {
                if (methodInfo.GetCustomAttribute<ShaderMethodAttribute>() != null && !methodInfo.IsSpecialName)
                {
                    ShaderAttribute shaderAttribute = methodInfo.GetCustomAttribute<ShaderAttribute>();

                    if (shaderAttribute != null)
                    {
                        result.SetShader(shaderAttribute.Name, methodInfo);
                    }

                    WriteMethod(writer, methodInfo);
                    writer.WriteLine();
                }
            }

            (writer.InnerWriter as StringWriter)?.GetStringBuilder().TrimEnd();
            writer.WriteLine();

            result.ShaderSource = stringWriter.ToString();

            return result;
        }

        private static void WriteMethod(IndentedTextWriter writer, MethodInfo methodInfo)
        {
            CSharpDecompiler decompiler = CSharpDecompilers.GetMappedDecompiler(methodInfo.DeclaringType.Assembly.Location);

            EntityHandle methodHandle = MetadataTokenHelpers.TryAsEntityHandle(methodInfo.MetadataToken) ?? throw new Exception();
            string sourceCode = decompiler.DecompileAsString(methodHandle);

            sourceCode = sourceCode.Replace("vector", "vec");
            sourceCode = Regex.Replace(sourceCode, @"\d+[fF]", m => m.Value.Replace("f", ""));

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = syntaxTree.GetRoot();

            root = new ShaderMethodDeclarationRewriter().Visit(root);

            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter();
            root = syntaxRewriter.Visit(root);

            string shaderSource = root.ToFullString();

            string indent = "";

            for (int i = 0; i < writer.Indent; i++)
            {
                indent += IndentedTextWriter.DefaultTabString;
            }

            shaderSource = shaderSource.Replace(Environment.NewLine, Environment.NewLine + indent).TrimEnd(' ');

            writer.Write(shaderSource);
        }

        private static void WriteStructure(IndentedTextWriter writer, Type type, BindingFlags bindingAttr)
        {
            writer.WriteLine($"struct {type.Name}");
            writer.WriteLine("{");
            writer.Indent++;

            HlslSemanticTracker tracker = new HlslSemanticTracker();
            bool insertNewLine = false;

            foreach (FieldInfo fieldInfo in type.GetFields(bindingAttr))
            {
                WriteStructureField(writer, fieldInfo, fieldInfo.FieldType, ref tracker);
                insertNewLine = true;
            }

            foreach (PropertyInfo propertyInfo in type.GetProperties(bindingAttr))
            {
                WriteStructureField(writer, propertyInfo, propertyInfo.PropertyType, ref tracker);
                insertNewLine = true;
            }

            if (insertNewLine)
            {
                writer.Indent--;
                writer.WriteLine();
                writer.Indent++;
            }

            foreach (MethodInfo methodInfo in type.GetMethods(bindingAttr | BindingFlags.DeclaredOnly))
            {
                if (methodInfo.GetCustomAttribute<ShaderMethodAttribute>() != null && !methodInfo.IsSpecialName)
                {
                    WriteMethod(writer, methodInfo);
                    writer.WriteLine();
                }
            }

            (writer.InnerWriter as StringWriter)?.GetStringBuilder().TrimEnd();

            writer.Indent--;
            writer.WriteLine();
            writer.WriteLine("};");
        }

        private static void WriteStructureField(IndentedTextWriter writer, MemberInfo memberInfo, Type type, ref HlslSemanticTracker tracker)
        {
            writer.Write(HlslKnownTypes.GetMappedName(type));
            writer.Write(' ');
            writer.Write(memberInfo.Name);

            int arrayCount = type.IsArray ? 2 : 0;
            writer.Write(WriteArray(arrayCount));

            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<VertexSemanticAttribute>(), ref tracker));
            writer.WriteLine(';');
        }

        private static void WriteResource(IndentedTextWriter writer, MemberInfo memberInfo, Type type, ref HlslBindingTracker tracker)
        {
            ShaderResourceKind resourceKind = type switch
            {
                _ when type == typeof(SamplerResource) => ShaderResourceKind.Sampler,
                _ when type == typeof(Texture2DResource) => ShaderResourceKind.Texture2D,
                _ => type.GetCustomAttribute<ResourceAttribute>()?.Kind ?? ShaderResourceKind.Uniform
            };

            switch (resourceKind)
            {
                case ShaderResourceKind.Uniform:
                    WriteUniform(writer, memberInfo, type, tracker.Uniform++);
                    break;
                case ShaderResourceKind.Texture2D:
                    WriteTexture2D(writer, memberInfo, type, tracker.Texture++);
                    break;
                case ShaderResourceKind.Sampler:
                    WriteSampler(writer, memberInfo, type, tracker.Sampler++);
                    break;
                default:
                    throw new NotSupportedException("This shader resource kind is not supported.");
            }
        }

        private static void WriteSampler(IndentedTextWriter writer, MemberInfo memberInfo, Type type, int binding)
        {
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(type)} {memberInfo.Name} : register(s{binding});");
        }

        private static void WriteTexture2D(IndentedTextWriter writer, MemberInfo memberInfo, Type type, int binding)
        {
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(type)} {memberInfo.Name} : register(t{binding});");
        }

        private static void WriteUniform(IndentedTextWriter writer, MemberInfo memberInfo, Type type, int binding)
        {
            int arrayCount = type.IsArray ? 2 : 0;

            writer.WriteLine($"cbuffer {memberInfo.Name}Buffer : register(b{binding})");
            writer.WriteLine("{");
            writer.WriteLine($"    {HlslKnownTypes.GetMappedName(type)} {memberInfo.Name}{WriteArray(arrayCount)};");
            writer.WriteLine("}");
        }

        private static string WriteArray(int arrayCount)
        {
            return arrayCount > 0 ? $"[{arrayCount}]" : "";
        }

        private static Type GetElementOrDeclaredType(this Type type) => type.IsArray ? type.GetElementType() : type;

        private static string GetHlslSemantic(VertexSemanticAttribute semanticType, ref HlslSemanticTracker tracker) => semanticType switch
        {
            PositionSemanticAttribute _ => " : Position" + tracker.Position++,
            NormalSemanticAttribute _ => " : Normal" + tracker.Normal++,
            TextureCoordinateSemanticAttribute _ => " : TexCoord" + tracker.TexCoord++,
            ColorSemanticAttribute _ => " : Color" + tracker.Color++,
            TangentSemanticAttribute _ => " : Tangent" + tracker.Tangent++,
            SystemPositionSemanticAttribute _ => " : SV_Position",
            SystemInstanceIdSemanticAttribute _ => " : SV_InstanceId",
            SystemRenderTargetArrayIndexSemanticAttribute _ => " : SV_RenderTargetArrayIndex",
            ColorTargetSemanticAttribute _ => " : SV_Target" + tracker.ColorTarget++,
            _ => ""
        };

        private static string GetDefaultEntryPoint(ShaderVersion? version) => version switch
        {
            ShaderVersion.VertexShader => "VSMain",
            ShaderVersion.HullShader => "HSMain",
            ShaderVersion.DomainShader => "DSMain",
            ShaderVersion.GeometryShader => "GSMain",
            ShaderVersion.PixelShader => "PSMain",
            ShaderVersion.ComputeShader => "CSMain",
            _ => ""
        };

        private static string GetShaderProfile(ShaderVersion? version) => version switch
        {
            ShaderVersion.VertexShader => "vs",
            ShaderVersion.HullShader => "hs",
            ShaderVersion.DomainShader => "ds",
            ShaderVersion.GeometryShader => "gs",
            ShaderVersion.PixelShader => "ps",
            ShaderVersion.ComputeShader => "cs",
            _ => "lib"
        };

        internal static class CSharpDecompilers
        {
            private static readonly Dictionary<string, CSharpDecompiler> s_decompilers = new Dictionary<string, CSharpDecompiler>();

            public static CSharpDecompiler GetMappedDecompiler(string assemblyPath)
            {
                if (!s_decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                {
                    UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard2.0");

                    DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)
                    {
                        UsingDeclarations = false
                    };

                    decompilerSettings.CSharpFormattingOptions.IndentationString = IndentedTextWriter.DefaultTabString;

                    decompiler = new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);

                    s_decompilers.Add(assemblyPath, decompiler);
                }

                return decompiler;
            }
        }

        internal static class HlslKnownTypes
        {
            private static readonly Dictionary<string, string> s_knownTypes = new Dictionary<string, string>()
            {
                { "System.UInt32", "uint" },
                { "System.Int32", "int" },
                { "System.Single", "float" },
                { "System.Numerics.Vector2", "float2" },
                { "System.Numerics.Vector3", "float3" },
                { "System.Numerics.Vector4", "float4" },
                { "System.Numerics.Matrix4x4", "float4x4" },
                { "System.Void", "void" },
                { "ShaderGen.SamplerResource", "SamplerState" },
                { "ShaderGen.SamplerComparisonResource", "SamplerComparisonState" },
                { "ShaderGen.Texture2DResource", "Texture2D" },
                { "ShaderGen.Texture2DArrayResource", "Texture2DArray" },
                { "ShaderGen.TextureCubeResource", "TextureCube" },
                { "ShaderGen.DepthTexture2DResource", "Texture2D" },
                { "ShaderGen.DepthTexture2DArrayResource", "Texture2DArray" },
                { "System.Boolean", "bool" },
                { "ShaderGen.UInt2", "uint2" },
                { "ShaderGen.UInt3", "uint3" },
                { "ShaderGen.UInt4", "uint4" },
                { "ShaderGen.Int2", "int2" },
                { "ShaderGen.Int3", "int3" },
                { "ShaderGen.Int4", "int4" },
                { "ShaderGen.AtomicBufferUInt32", "RWStructuredBuffer<uint>" },
                { "ShaderGen.AtomicBufferInt32", "RWStructuredBuffer<int>" },
            };

            public static string GetMappedName(Type type)
            {
                return s_knownTypes.TryGetValue(type.GetElementOrDeclaredType().FullName.Replace('+', '.'), out string mapped) ? mapped : type.Name;
            }

            public static string GetMappedName(string name)
            {
                return s_knownTypes.TryGetValue(name, out string mapped) ? mapped : Regex.Match(name, @"[^\.]+$").Value;
            }

            public static bool ContainsKey(Type type)
            {
                return s_knownTypes.ContainsKey(type.GetElementOrDeclaredType().FullName.Replace('+', '.'));
            }

            public static bool ContainsKey(string name)
            {
                return s_knownTypes.ContainsKey(name);
            }
        }

        internal static class HlslKnownMethods
        {
            private static readonly Dictionary<string, string> s_knownMethod = new Dictionary<string, string>()
            {
                { "System.Numerics.Vector4.Transform", "mul" },
                { "System.Numerics.Vector4.Zero", "float4(0.0f, 0.0f, 0.0f, 0.0f)" }
            };

            public static string GetMappedName(string name)
            {
                return s_knownMethod.TryGetValue(name, out string mapped) ? mapped : name;
            }
        }

        private struct HlslBindingTracker
        {
            public int Sampler;
            public int Texture;
            public int Uniform;
        };

        private struct HlslSemanticTracker
        {
            public int Position;
            public int TexCoord;
            public int Normal;
            public int Tangent;
            public int Color;
            public int ColorTarget;
        }
    }
}
