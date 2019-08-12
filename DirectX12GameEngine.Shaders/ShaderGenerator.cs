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

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerator
    {
        private static readonly object compilationLock = new object();
        private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();

        private static Compilation compilation;

        private readonly List<ShaderTypeDefinition> collectedTypes = new List<ShaderTypeDefinition>();
        private readonly BindingFlags bindingFlagsWithContract;
        private readonly BindingFlags bindingFlagsWithoutContract;
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
                    try { PEFile peFile = new PEFile(p); return true; } catch { return false; }
                });
            }

            var metadataReferences = assemblyPaths.Select(p => MetadataReference.CreateFromFile(p));

            compilation = CSharpCompilation.Create("ShaderAssembly").WithReferences(metadataReferences);

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
        }

        private static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            if (!e.LoadedAssembly.IsDynamic)
            {
                PortableExecutableReference metadataReference = MetadataReference.CreateFromFile(e.LoadedAssembly.Location);
                compilation = compilation.AddReferences(metadataReference);
            }
        }

        public ShaderGenerator(object shader, BindingFlags bindingFlagsWithContract = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, BindingFlags bindingFlagsWithoutContract = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
        {
            this.shader = shader;
            this.bindingFlagsWithContract = bindingFlagsWithContract;
            this.bindingFlagsWithoutContract = bindingFlagsWithoutContract;

            writer = new IndentedTextWriter(stringWriter);
        }

        public bool IsGenerated => result != null;

        public void AddType(Type type)
        {
            CollectStructure(type, null);
        }

        public BindingFlags GetBindingFlagsForType(Type type)
        {
            return type.IsDefined(typeof(ShaderContractAttribute)) ? bindingFlagsWithContract : bindingFlagsWithoutContract;
        }

        public ShaderGenerationResult GenerateShader()
        {
            if (result != null) return result;

            Type shaderType = shader.GetType();

            var memberInfos = shaderType.GetMembersInTypeHierarchyInOrder(GetBindingFlagsForType(shaderType));

            // Collecting stage

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(shader);

                if (memberType != null)
                {
                    CollectStructure(memberType, memberInfo.GetMemberValue(shader));
                }

                if (memberInfo is MethodInfo methodInfo && memberInfo.IsDefined(typeof(ShaderMemberAttribute)))
                {
                    CollectTopLevelMethod(methodInfo);
                }
            }

            // Writing stage

            foreach (ShaderTypeDefinition type in collectedTypes)
            {
                WriteStructure(type.Type, type.Instance);
            }

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(shader);
                ShaderMemberAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (memberInfo is MethodInfo methodInfo && memberInfo.IsDefined(typeof(ShaderMemberAttribute)))
                {
                    WriteTopLevelMethod(methodInfo);
                }
                else if (memberType != null && resourceType != null)
                {
                    WriteResource(memberInfo, memberType, resourceType);
                }
            }

            stringWriter.GetStringBuilder().TrimEnd();
            writer.WriteLine();

            result = new ShaderGenerationResult(stringWriter.ToString());
            GetEntryPoints(result, shaderType, GetBindingFlagsForType(shaderType));

            return result;
        }

        public static ShaderGenerationResult GetEntryPoints(ShaderGenerationResult result, Type shaderType, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        {
            foreach (MethodInfo shaderMethodInfo in shaderType.GetMethods(bindingFlags).Where(m => m.IsDefined(typeof(ShaderAttribute))))
            {
                ShaderAttribute shaderAttribute = shaderMethodInfo.GetCustomAttribute<ShaderAttribute>();
                result.SetShader(shaderAttribute.Name, shaderMethodInfo.Name);
            }

            return result;
        }

        private void CollectStructure(Type type, object? obj)
        {
            type = type.GetElementOrDeclaredType();

            if (type.IsAssignableFrom(shader.GetType()) || HlslKnownTypes.ContainsKey(type) || collectedTypes.Any(d => d.Type == type)) return;

            ShaderTypeDefinition shaderTypeDefinition = new ShaderTypeDefinition(type, obj);

            if (type.IsEnum)
            {
                collectedTypes.Add(shaderTypeDefinition);
                return;
            }

            Type parentType = type.BaseType;

            while (parentType != null)
            {
                CollectStructure(parentType, obj);
                parentType = parentType.BaseType;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                CollectStructure(interfaceType, obj);
            }

            var memberInfos = type.GetMembersInOrder(GetBindingFlagsForType(type));

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                ShaderMemberAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (memberInfo is MethodInfo methodInfo && (memberInfo.IsDefined(typeof(ShaderMemberAttribute)) || type.IsInterface))
                {
                    CollectMethod(methodInfo);
                }
                else if (memberType != null && memberType != type)
                {
                    CollectStructure(memberType, memberInfo.GetMemberValue(obj));

                    if (resourceType != null)
                    {
                        shaderTypeDefinition.ResourceDefinitions.Add(new ResourceDefinition(memberType, resourceType));
                    }
                }
            }

            collectedTypes.Add(shaderTypeDefinition);
        }

        private void WriteStructure(Type type, object? obj)
        {
            string[] namespaces = type.Namespace.Split('.');

            for (int i = 0; i < namespaces.Length - 1; i++)
            {
                writer.Write($"namespace {namespaces[i]} {{ ");
            }

            writer.WriteLine($"namespace {namespaces[namespaces.Length - 1]}");
            writer.WriteLine("{");
            writer.Indent++;

            if (type.IsEnum)
            {
                writer.WriteLine($"enum class {type.Name}");
            }
            else if (type.IsInterface)
            {
                writer.WriteLine($"interface {type.Name}");
            }
            else
            {
                writer.Write($"struct {type.Name}");

                if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
                {
                    writer.Write($" : {HlslKnownTypes.GetMappedName(type.BaseType)}, ");

                    // NOTE: Types might no expose every interface method.

                    //foreach (Type interfaceType in type.GetInterfaces())
                    //{
                    //    writer.Write(interfaceType.Name + ", ");
                    //}

                    stringWriter.GetStringBuilder().Length -= 2;
                }

                writer.WriteLine();
            }

            writer.WriteLine("{");
            writer.Indent++;

            var fieldAndPropertyInfos = type.GetMembersInOrder(GetBindingFlagsForType(type) | BindingFlags.DeclaredOnly).Where(m => m is FieldInfo || m is PropertyInfo);
            var methodInfos = type.GetMembersInTypeHierarchyInOrder(GetBindingFlagsForType(type)).Where(m => m is MethodInfo);
            var memberInfos = fieldAndPropertyInfos.Concat(methodInfos);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);

                if (memberInfo is MethodInfo methodInfo && (memberInfo.IsDefined(typeof(ShaderMemberAttribute)) || type.IsInterface))
                {
                    WriteMethod(methodInfo);
                }
                else if (memberType != null)
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

            stringWriter.GetStringBuilder().TrimEnd();

            writer.Indent--;
            writer.WriteLine();
            writer.WriteLine("};");
            writer.Indent--;

            for (int i = 0; i < namespaces.Length - 1; i++)
            {
                writer.Write("}");
            }

            writer.WriteLine("}");
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

        private void WriteResource(MemberInfo memberInfo, Type memberType, ShaderMemberAttribute resourceType)
        {
            switch (resourceType)
            {
                case ConstantBufferResourceAttribute _:
                    WriteConstantBuffer(memberInfo, memberType, bindingTracker.ConstantBuffer++);
                    break;
                case SamplerResourceAttribute _:
                    WriteSampler(memberInfo, memberType, bindingTracker.Sampler++);
                    break;
                case TextureResourceAttribute _:
                    WriteTexture(memberInfo, memberType, bindingTracker.Texture++);
                    break;
                case UnorderedAccessViewResourceAttribute _:
                    WriteUnorderedAccessView(memberInfo, memberType, bindingTracker.UnorderedAccessView++);
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
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
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
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(s{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteTexture(MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(t{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteUnorderedAccessView(MemberInfo memberInfo, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");
            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.Write($" : register(u{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteStaticResource(MemberInfo memberInfo, Type memberType)
        {
            List<MemberInfo> generatedMemberInfos = new List<MemberInfo>();

            foreach (ResourceDefinition resourceDefinition in collectedTypes.First(d => d.Type == memberType).ResourceDefinitions)
            {
                MemberInfo generatedMemberInfo = new FakeMemberInfo($"__Generated__{bindingTracker.StaticResource++}__");
                generatedMemberInfos.Add(generatedMemberInfo);

                WriteResource(generatedMemberInfo, resourceDefinition.MemberType, resourceDefinition.ResourceType);
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

        private static string GetHlslSemantic(ShaderSemanticAttribute? semanticAttribute)
        {
            if (semanticAttribute is null) return "";

            Type semanticType = semanticAttribute.GetType();

            if (HlslKnownSemantics.ContainsKey(semanticType))
            {
                string semanticName = HlslKnownSemantics.GetMappedName(semanticType);

                return semanticAttribute is ShaderSemanticWithIndexAttribute semanticAttributeWithIndex
                    ? " : " + semanticName + semanticAttributeWithIndex.Index
                    : " : " + semanticName;
            }

            throw new NotSupportedException();
        }

        private void CollectTopLevelMethod(MethodInfo methodInfo)
        {
            IList<MethodInfo> methodInfos = methodInfo.GetBaseMethods();

            for (int depth = methodInfos.Count - 1; depth >= 0; depth--)
            {
                MethodInfo currentMethodInfo = methodInfos[depth];
                CollectMethod(currentMethodInfo);
            }
        }

        private void CollectMethod(MethodInfo methodInfo)
        {
            GetSyntaxTree(methodInfo, out SyntaxNode root, out SemanticModel semanticModel);

            ShaderSyntaxCollector syntaxCollector = new ShaderSyntaxCollector(this, semanticModel);
            syntaxCollector.Visit(root);
        }

        private void WriteTopLevelMethod(MethodInfo methodInfo)
        {
            IList<MethodInfo> methodInfos = methodInfo.GetBaseMethods();

            for (int depth = methodInfos.Count - 1; depth >= 0; depth--)
            {
                MethodInfo currentMethodInfo = methodInfos[depth];
                WriteMethod(currentMethodInfo, depth);
            }
        }

        private void WriteMethod(MethodInfo methodInfo, int depth = 0)
        {
            GetSyntaxTree(methodInfo, out SyntaxNode root, out SemanticModel semanticModel);

            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(this, semanticModel, true, depth);
            root = syntaxRewriter.Visit(root);

            string shaderSource = root.ToFullString();

            // TODO: See why the System namespace in System.Math is not present in UWP projects.
            shaderSource = shaderSource.Replace("Math.Max", "max");
            shaderSource = shaderSource.Replace("Math.Pow", "pow");
            shaderSource = shaderSource.Replace("Math.Sin", "sin");

            shaderSource = shaderSource.Replace("vector", "vec");
            shaderSource = Regex.Replace(shaderSource, @"\d+[fF]", m => m.Value.Replace("f", ""));

            shaderSource = shaderSource.TrimStart(' ');

            // Indent every line
            string indent = "";

            for (int i = 0; i < writer.Indent; i++)
            {
                indent += IndentedTextWriter.DefaultTabString;
            }

            shaderSource = shaderSource.Replace(Environment.NewLine + IndentedTextWriter.DefaultTabString, Environment.NewLine + indent).TrimEnd(' ');

            writer.WriteLine(shaderSource);
        }

        private static void GetSyntaxTree(MethodInfo methodInfo, out SyntaxNode root, out SemanticModel semanticModel)
        {
            lock (compilationLock)
            {
                EntityHandle handle = MetadataTokenHelpers.TryAsEntityHandle(methodInfo.DeclaringType.MetadataToken) ?? throw new InvalidOperationException();
                string assemblyPath = methodInfo.DeclaringType.Assembly.Location;

                if (!decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                {
                    decompiler = CreateDecompiler(assemblyPath);
                    decompilers.Add(assemblyPath, decompiler);
                }

                string sourceCode = decompiler.DecompileAsString(handle);

                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                root = syntaxTree.GetRoot();
                root = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().First(n => n.Identifier.ValueText == methodInfo.Name && n.ParameterList.Parameters.Count == methodInfo.GetParameters().Length);

                compilation = compilation.AddSyntaxTrees(syntaxTree);
                semanticModel = compilation.GetSemanticModel(syntaxTree);
            }
        }

        private static CSharpDecompiler CreateDecompiler(string assemblyPath)
        {
            UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard");

            DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)
            {
                ObjectOrCollectionInitializers = false,
                UsingDeclarations = false
            };

            decompilerSettings.CSharpFormattingOptions.IndentationString = IndentedTextWriter.DefaultTabString;

            return new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);
        }

        internal static class HlslKnownAttributes
        {
            private static readonly Dictionary<string, string> allowedAttributes = new Dictionary<string, string>()
            {
                { typeof(NumThreadsAttribute).FullName, "NumThreads" },
                { typeof(ShaderAttribute).FullName, "Shader" }
            };

            public static bool ContainsKey(string name)
            {
                return allowedAttributes.ContainsKey(name) || HlslKnownSemantics.ContainsKey(name);
            }

            public static string GetMappedName(string name)
            {
                if (!allowedAttributes.TryGetValue(name, out string mappedName))
                {
                    mappedName = HlslKnownSemantics.GetMappedName(name);
                }

                return mappedName;
            }
        }

        internal static class HlslKnownSemantics
        {
            private static readonly Dictionary<string, string> knownSemantics = new Dictionary<string, string>()
            {
                { typeof(PositionSemanticAttribute).FullName, "Position" },
                { typeof(NormalSemanticAttribute).FullName, "Normal" },
                { typeof(TextureCoordinateSemanticAttribute).FullName, "TexCoord" },
                { typeof(ColorSemanticAttribute).FullName, "Color" },
                { typeof(TangentSemanticAttribute).FullName, "Tangent" },

                { typeof(SystemTargetSemanticAttribute).FullName, "SV_Target" },
                { typeof(SystemDispatchThreadIdSemanticAttribute).FullName, "SV_DispatchThreadId" },
                { typeof(SystemIsFrontFaceSemanticAttribute).FullName, "SV_IsFrontFace" },
                { typeof(SystemInstanceIdSemanticAttribute).FullName, "SV_InstanceId" },
                { typeof(SystemPositionSemanticAttribute).FullName, "SV_Position" },
                { typeof(SystemRenderTargetArrayIndexSemanticAttribute).FullName, "SV_RenderTargetArrayIndex" }
            };

            public static bool ContainsKey(Type type)
            {
                return knownSemantics.ContainsKey(type.GetElementOrDeclaredType().FullName);
            }

            public static bool ContainsKey(string name)
            {
                return knownSemantics.ContainsKey(name);
            }

            public static string GetMappedName(Type type)
            {
                return knownSemantics[type.GetElementOrDeclaredType().FullName];
            }

            public static string GetMappedName(string name)
            {
                return knownSemantics[name];
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
                { typeof(Numerics.UInt2).FullName, "uint2" },
                { typeof(Numerics.UInt3).FullName, "uint3" },
                { typeof(Matrix4x4).FullName, "float4x4" },
                { typeof(SamplerResource).FullName, "SamplerState" },
                { typeof(SamplerComparisonResource).FullName, "SamplerComparisonState" },
                { typeof(Texture2DResource).FullName, "Texture2D" },
                { typeof(Texture2DArrayResource).FullName, "Texture2DArray" },
                { typeof(TextureCubeResource).FullName, "TextureCube" },
                { typeof(RWBufferResource<>).FullName, "RWBuffer" },
                { typeof(RWTexture2DResource<>).FullName, "RWTexture2D" },
            };

            public static bool ContainsKey(Type type)
            {
                type = type.GetElementOrDeclaredType();
                string typeFullName = type.Namespace + Type.Delimiter + type.Name;

                return knownTypes.ContainsKey(typeFullName);
            }

            public static bool ContainsKey(string name)
            {
                int indexOfOpenBracket = name.IndexOf('<');
                name = indexOfOpenBracket >= 0 ? name.Remove(indexOfOpenBracket) + "`1" : name;

                return knownTypes.ContainsKey(name);
            }

            public static string GetMappedName(Type type)
            {
                type = type.GetElementOrDeclaredType();
                string fullTypeName = type.Namespace + Type.Delimiter + type.Name;

                string mappedName = knownTypes.TryGetValue(fullTypeName, out string mapped) ? mapped : fullTypeName.Replace(".", "::");

                return type.IsGenericType ? mappedName + $"<{GetMappedName(type.GetGenericArguments()[0])}>" : mappedName;
            }

            public static string GetMappedName(string name)
            {
                int indexOfOpenBracket = name.IndexOf('<');

                string genericArguments = indexOfOpenBracket >= 0 ? name.Substring(indexOfOpenBracket) : "";
                name = indexOfOpenBracket >= 0 ? name.Remove(indexOfOpenBracket) + "`1" : name;

                string mappedName = knownTypes.TryGetValue(name, out string mapped) ? mapped : name.Replace(".", "::");

                return mappedName + genericArguments;
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

                { "DirectX12GameEngine.Shaders.Numerics.Vector2.Length", "length" },

                { "DirectX12GameEngine.Shaders.Numerics.UInt2.X", ".x" },
                { "DirectX12GameEngine.Shaders.Numerics.UInt2.Y", ".y" },

                { "DirectX12GameEngine.Shaders.Numerics.UInt3.X", ".x" },
                { "DirectX12GameEngine.Shaders.Numerics.UInt3.Y", ".y" },
                { "DirectX12GameEngine.Shaders.Numerics.UInt3.Z", ".z" },
                { "DirectX12GameEngine.Shaders.Numerics.UInt3.XY", ".xy" },

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

            public static bool ContainsKey(string name)
            {
                return knownMethods.ContainsKey(name);
            }

            public static string GetMappedName(string name)
            {
                return knownMethods[name];
            }
        }

        private class HlslBindingTracker
        {
            public int ConstantBuffer { get; set; }

            public int Sampler { get; set; }

            public int Texture { get; set; }

            public int UnorderedAccessView { get; set; }

            public int StaticResource { get; set; }
        }

        private class ShaderTypeDefinition
        {
            public ShaderTypeDefinition(Type type, object? instance)
            {
                Type = type;
                Instance = instance;
            }

            public object? Instance { get; }

            public Type Type { get; }

            public List<ResourceDefinition> ResourceDefinitions { get; } = new List<ResourceDefinition>();
        }

        private class ResourceDefinition
        {
            public ResourceDefinition(Type memberType, ShaderMemberAttribute resourceType)
            {
                MemberType = memberType;
                ResourceType = resourceType;
            }

            public Type MemberType { get; }

            public ShaderMemberAttribute ResourceType { get; }
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
