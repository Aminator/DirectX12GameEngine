using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class ShaderGenerator
    {
        private static readonly object compilationLock = new object();
        private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();
        private static readonly IEnumerable<PEFile> peFiles;
        private static readonly Dictionary<Type, (PEFile PEFile, TypeDefinition TypeDefinition)> typeDefinitions = new Dictionary<Type, (PEFile, TypeDefinition)>();

        private static Compilation compilation;

        private readonly OrderedDictionary collectedTypes = new OrderedDictionary();
        private readonly BindingFlags bindingAttr;
        private readonly HlslBindingTracker bindingTracker = new HlslBindingTracker();
        private readonly object shader;
        private readonly StringWriter stringWriter = new StringWriter();
        private readonly IndentedTextWriter writer;

        private ShaderGenerationResult? result;

        static ShaderGenerator()
        {
            IEnumerable<string> assemblyPaths;

            if (!string.IsNullOrEmpty(Assembly.GetEntryAssembly().Location))
            {
                assemblyPaths = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).Select(a => a.Location);
            }
            else
            {
                assemblyPaths = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(p =>
                {
                    try
                    {
                        PEFile peFile = new PEFile(p);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            var metadataReferences = assemblyPaths.Select(p => MetadataReference.CreateFromFile(p));
            peFiles = assemblyPaths.Select(p => new PEFile(p));

            compilation = CSharpCompilation.Create("ShaderAssembly").WithReferences(metadataReferences);
        }

        public ShaderGenerator(object shader, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        {
            this.shader = shader;
            this.bindingAttr = bindingAttr;

            writer = new IndentedTextWriter(stringWriter);
        }

        public void AddType(Type type)
        {
            CollectStructure(type, null);
        }

        public ShaderGenerationResult GenerateShaderSource()
        {
            if (result != null) return result;

            result = new ShaderGenerationResult();

            Type shaderType = shader.GetType();

            var memberInfos = shaderType.GetMembersInOrder(bindingAttr).Where(m => m.IsDefined(typeof(ShaderResourceAttribute)));

            // Collecting stage

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(shader);

                if (memberType != null)
                {
                    CollectStructure(memberType, memberInfo.GetMemberValue(shader));
                }

                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    CollectMethod(methodInfo);
                }
            }

            // Writing stage

            foreach (DictionaryEntry pair in collectedTypes)
            {
                WriteStructure((Type)pair.Key, ((ShaderTypeDefinition)pair.Value).Instance);
            }

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(shader);
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    WriteMethod(methodInfo);
                }
                else if (memberType != null && resourceType != null)
                {
                    WriteResource(memberInfo, memberType, resourceType);
                }
            }

            // Set shader entry points

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

        private void CollectStructure(Type type, object? obj)
        {
            type = type.GetElementOrDeclaredType();

            if (HlslKnownTypes.ContainsKey(type) || collectedTypes.Contains(type)) return;

            var shaderTypeDefinition = new ShaderTypeDefinition(obj);

            var memberInfos = type.GetMembersInOrder(bindingAttr).Where(m => m.IsDefined(typeof(ShaderResourceAttribute)));

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (memberType != null && memberType != type)
                {
                    CollectStructure(memberType, memberInfo.GetMemberValue(obj));

                    if (resourceType != null)
                    {
                        shaderTypeDefinition.ResourceDefinitions.Add(new ResourceDefinition(memberType, resourceType));
                    }
                }

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    CollectMethod(methodInfo);
                }
            }

            collectedTypes.Add(type, shaderTypeDefinition);
        }

        private void WriteStructure(Type type, object? obj)
        {
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

            var memberInfos = type.GetMembersInOrder(bindingAttr).Where(m => m.IsDefined(typeof(ShaderResourceAttribute)));

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                ShaderResourceAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (resourceType is ShaderMethodAttribute && memberInfo is MethodInfo methodInfo)
                {
                    WriteMethod(methodInfo);
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
                        WriteStructureField(memberInfo, memberType);
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
                    WriteStaticStructureField(memberInfo, memberType);
                }
            }
        }

        private void WriteStructureField(MemberInfo memberInfo, Type memberType)
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

        private void WriteStaticStructureField(MemberInfo memberInfo, Type memberType)
        {
            string declaringType = HlslKnownTypes.GetMappedName(memberInfo.DeclaringType);
            writer.WriteLine($"static {HlslKnownTypes.GetMappedName(memberType)} {declaringType}::{memberInfo.Name};");
            writer.WriteLine();
        }

        private void WriteResource(MemberInfo memberInfo, Type memberType, ShaderResourceAttribute resourceType)
        {
            switch (resourceType)
            {
                case ConstantBufferResourceAttribute _:
                    WriteConstantBuffer(memberInfo, memberType, bindingTracker.ConstantBuffer++);
                    break;
                case SamplerResourceAttribute _:
                    WriteSampler(memberInfo, memberType, bindingTracker.Sampler++);
                    break;
                case Texture2DResourceAttribute _:
                    WriteTexture2D(memberInfo, memberType, bindingTracker.Texture++);
                    break;
                case StaticResourceAttribute _:
                    WriteStaticResource(memberInfo, memberType);
                    break;
                default:
                    throw new NotSupportedException("This shader resource type is not supported.");
            }
        }

        private void WriteConstantBuffer(MemberInfo memberInfo, Type memberType, int binding)
        {
            int arrayCount = memberType.IsArray ? 2 : 0;

            writer.Write($"cbuffer {memberInfo.Name}Buffer");
            //writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.WriteLine($" : register(b{binding})");
            writer.WriteLine("{");
            writer.Indent++;
            writer.WriteLine($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}{GetArrayString(arrayCount)};");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        private void WriteSampler(MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            //writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(s{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteTexture2D(MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            //writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(t{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteStaticResource(MemberInfo memberInfo, Type memberType)
        {
            List<MemberInfo> generatedMemberInfos = new List<MemberInfo>();

            foreach (ResourceDefinition resourceDefinition in ((ShaderTypeDefinition)collectedTypes[memberType]).ResourceDefinitions)
            {
                MemberInfo generatedMemberInfo = new FakeMemberInfo($"__Generated__{bindingTracker.StaticResource++}__");
                generatedMemberInfos.Add(generatedMemberInfo);

                WriteResource(generatedMemberInfo, resourceDefinition.MemberType, resourceDefinition.ResourceType);
            }

            writer.Write($"static {HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            //writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));

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

        private void CollectMethod(MethodInfo methodInfo)
        {
            IList<MethodInfo> methodInfos = methodInfo.GetBaseMethods();

            for (int depth = methodInfos.Count - 1; depth >= 0; depth--)
            {
                MethodInfo currentMethodInfo = methodInfos[depth];
                GetSyntaxTree(currentMethodInfo, out SyntaxNode root, out SemanticModel semanticModel);

                ShaderSyntaxCollector syntaxCollector = new ShaderSyntaxCollector(this, semanticModel);
                syntaxCollector.Visit(root);
            }
        }

        private void WriteMethod(MethodInfo methodInfo)
        {
            IList<MethodInfo> methodInfos = methodInfo.GetBaseMethods();

            for (int depth = methodInfos.Count - 1; depth >= 0; depth--)
            {
                MethodInfo currentMethodInfo = methodInfos[depth];
                GetSyntaxTree(currentMethodInfo, out SyntaxNode root, out SemanticModel semanticModel);

                ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(this, semanticModel, depth);
                root = syntaxRewriter.Visit(root);

                string shaderSource = root.ToFullString();

                // TODO: See why the System namespace in System.Math is not present in UWP projects.
                shaderSource = shaderSource.Replace("Math.Max", "max");
                shaderSource = shaderSource.Replace("Math.Pow", "pow");
                shaderSource = shaderSource.Replace("Math.Sin", "sin");

                shaderSource = shaderSource.Replace("vector", "vec");
                shaderSource = Regex.Replace(shaderSource, @"\d+[fF]", m => m.Value.Replace("f", ""));

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

        private static void GetSyntaxTree(MethodInfo methodInfo, out SyntaxNode root, out SemanticModel semanticModel)
        {
            lock (compilationLock)
            {
                GetMethodHandle(methodInfo, out string assemblyPath, out EntityHandle methodHandle);

                if (!decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                {
                    decompiler = CreateDecompiler(assemblyPath);
                    decompilers.Add(assemblyPath, decompiler);
                }

                string sourceCode = decompiler.DecompileAsString(methodHandle);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                root = syntaxTree.GetRoot();

                compilation = compilation.AddSyntaxTrees(syntaxTree);
                semanticModel = compilation.GetSemanticModel(syntaxTree);
            }
        }

        private static void GetMethodHandle(MethodInfo methodInfo, out string assemblyPath, out EntityHandle methodHandle)
        {
            assemblyPath = methodInfo.DeclaringType.Assembly.Location;

            if (!string.IsNullOrEmpty(assemblyPath))
            {
                methodHandle = MetadataTokenHelpers.TryAsEntityHandle(methodInfo.MetadataToken) ?? throw new InvalidOperationException();
            }
            else
            {
                if (!typeDefinitions.TryGetValue(methodInfo.DeclaringType, out var tuple))
                {
                    foreach (PEFile peFile in peFiles)
                    {
                        TypeDefinitionHandle typeDefinitionHandle = peFile.Metadata.TypeDefinitions.FirstOrDefault(t => t.GetFullTypeName(peFile.Metadata).ToString() == methodInfo.DeclaringType.FullName);

                        if (!typeDefinitionHandle.IsNil)
                        {
                            tuple = (peFile, peFile.Metadata.GetTypeDefinition(typeDefinitionHandle));
                            typeDefinitions.Add(methodInfo.DeclaringType, tuple);

                            break;
                        }
                    }

                    if (tuple.PEFile is null) throw new InvalidOperationException();
                }

                PEFile peFileForMethod = tuple.PEFile;
                TypeDefinition typeDefinition = tuple.TypeDefinition;

                assemblyPath = peFileForMethod.FileName;

                methodHandle = typeDefinition.GetMethods()
                    .Where(m => peFileForMethod.Metadata.StringComparer.Equals(peFileForMethod.Metadata.GetMethodDefinition(m).Name, methodInfo.Name))
                    .First(m => peFileForMethod.Metadata.GetMethodDefinition(m).GetParameters().Count == methodInfo.GetParameters().Length);
            }
        }

        private static CSharpDecompiler CreateDecompiler(string assemblyPath)
        {
            UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard");

            DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)
            {
                UsingDeclarations = false
            };

            decompilerSettings.CSharpFormattingOptions.IndentationString = IndentedTextWriter.DefaultTabString;

            return new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);
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

            public static bool ContainsKey(Type type)
            {
                return knownTypes.ContainsKey(type.GetElementOrDeclaredType().FullName);
            }

            public static bool ContainsKey(string name)
            {
                return knownTypes.ContainsKey(name);
            }

            public static string GetMappedName(Type type)
            {
                string typeFullName = type.GetElementOrDeclaredType().FullName;
                string typeName = type.GetElementOrDeclaredType().Name;
                return knownTypes.TryGetValue(typeFullName, out string mapped) ? mapped : typeName;
            }

            public static string GetMappedName(string name)
            {
                return knownTypes.TryGetValue(name, out string mapped) ? mapped : Regex.Match(name, @"[^\.]+$").Value;
            }
        }

        internal static class HlslKnownMethods
        {
            private static readonly Dictionary<string, string> knownMethods = new Dictionary<string, string>()
            {
                { "System.Math.Cos", "cos" },
                { "System.MathF.Cos", "cos" },
                { "System.Math.Max", "max" },
                { "System.Math.Pow", "pow" },
                { "System.MathF.Pow", "pow" },
                { "System.Math.Sin", "sin" },
                { "System.MathF.Sin", "sin" },
                { "System.Math.PI", "3.1415926535897931" },
                { "System.MathF.PI", "3.14159274f" },

                { "DirectX12GameEngine.Rendering.Numerics.Vector2.Length", "length" },

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

            public static bool Contains(ISymbol containingMemberSymbol, ISymbol memberSymbol)
            {
                string fullTypeName = containingMemberSymbol.IsStatic ? containingMemberSymbol.ToString() : memberSymbol.ContainingType.ToString();

                if (knownMethods.ContainsKey(fullTypeName + Type.Delimiter + memberSymbol.Name))
                {
                    return true;
                }

                return false;
            }

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

        private class ShaderTypeDefinition
        {
            public ShaderTypeDefinition(object? instance)
            {
                Instance = instance;
            }

            public object? Instance { get; }

            public List<ResourceDefinition> ResourceDefinitions { get; } = new List<ResourceDefinition>();
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
