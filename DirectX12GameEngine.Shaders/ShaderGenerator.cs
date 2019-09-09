using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerator
    {
        private const string DelegateEntryPointName = "Main";

        private static readonly Dictionary<string, CSharpDecompiler> decompilers = new Dictionary<string, CSharpDecompiler>();
        private static readonly Dictionary<Type, SyntaxTree> decompiledTypes = new Dictionary<Type, SyntaxTree>();

        private static Compilation compilation;

        private readonly object shader;
        private readonly Delegate? action;

        private readonly List<ShaderTypeDefinition> collectedTypes = new List<ShaderTypeDefinition>();
        private readonly HlslBindingTracker bindingTracker = new HlslBindingTracker();

        private readonly StringWriter stringWriter = new StringWriter();
        private readonly IndentedTextWriter writer;

        private ShaderGeneratorResult? result;

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

            var metadataReferences = assemblyPaths.Select(p => MetadataReference.CreateFromFile(p)).ToArray();

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

        public ShaderGenerator(object shader, params Attribute[] entryPointAttributes) : this(shader, new ShaderGeneratorSettings(entryPointAttributes))
        {
        }

        public ShaderGenerator(object shader, ShaderGeneratorSettings settings)
        {
            this.shader = shader;

            EntryPointAttributes = settings.EntryPointAttributes;

            BindingFlagsWithContract = settings.BindingFlagsWithContract;
            BindingFlagsWithoutContract = settings.BindingFlagsWithoutContract;

            writer = new IndentedTextWriter(stringWriter);
        }

        public ShaderGenerator(Delegate action, params Attribute[] entryPointAttributes) : this(action, new ShaderGeneratorSettings(entryPointAttributes))
        {
        }

        public ShaderGenerator(Delegate action, ShaderGeneratorSettings settings) : this(action.Target, settings)
        {
            this.action = action;
        }

        public bool IsGenerated => result != null;

        public IEnumerable<Attribute> EntryPointAttributes { get; }

        public BindingFlags BindingFlagsWithContract { get; }

        public BindingFlags BindingFlagsWithoutContract { get; }

        public void AddType(Type type)
        {
            CollectStructure(type, null);
        }

        public BindingFlags GetBindingFlagsForType(Type type)
        {
            return type.IsDefined(typeof(ShaderContractAttribute)) ? BindingFlagsWithContract : BindingFlagsWithoutContract;
        }

        public ShaderGeneratorResult GenerateShader()
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

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
                {
                    CollectTopLevelMethod(methodInfo);
                }
            }

            if (action != null)
            {
                CollectTopLevelMethod(action.Method);
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

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
                {
                    WriteTopLevelMethod(methodInfo, EntryPointAttributes);
                }
                else if (memberType != null && resourceType != null)
                {
                    WriteResource(memberInfo.Name, memberType, resourceType);
                }
            }

            if (action != null)
            {
                WriteTopLevelMethod(action.Method, EntryPointAttributes, DelegateEntryPointName);
            }

            stringWriter.GetStringBuilder().TrimEnd();
            writer.WriteLine();

            result = new ShaderGeneratorResult(stringWriter.ToString());

            GetEntryPoints(result, shaderType, GetBindingFlagsForType(shaderType));

            ShaderAttribute? shaderAttribute = EntryPointAttributes.OfType<ShaderAttribute>().FirstOrDefault();

            if (shaderAttribute != null)
            {
                result.EntryPoints[shaderAttribute.Name] = DelegateEntryPointName;
            }

            return result;
        }

        public static void GetEntryPoints(ShaderGeneratorResult result, Type shaderType, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
        {
            foreach (MethodInfo shaderMethodInfo in shaderType.GetMethods(bindingFlags).Where(m => m.IsDefined(typeof(ShaderAttribute))))
            {
                ShaderAttribute shaderAttribute = shaderMethodInfo.GetCustomAttribute<ShaderAttribute>();
                result.EntryPoints[shaderAttribute.Name] = shaderMethodInfo.Name;
            }
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

            while (parentType != null && parentType != typeof(object) && parentType != typeof(ValueType))
            {
                CollectStructure(parentType, obj);
                parentType = parentType.BaseType;
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                CollectStructure(interfaceType, obj);
            }

            var memberInfos = type.GetMembersInOrder(GetBindingFlagsForType(type) | BindingFlags.DeclaredOnly);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                ShaderMemberAttribute? resourceType = memberInfo.GetResourceAttribute(memberType);

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
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

            BindingFlags fieldBindingFlags = GetBindingFlagsForType(type) | BindingFlags.DeclaredOnly;

            if (type.IsEnum)
            {
                fieldBindingFlags &= ~BindingFlags.Instance;
            }

            var fieldAndPropertyInfos = type.GetMembersInOrder(fieldBindingFlags).Where(m => m is FieldInfo || m is PropertyInfo);
            var methodInfos = type.GetMembersInTypeHierarchyInOrder(GetBindingFlagsForType(type)).Where(m => m is MethodInfo);
            var memberInfos = fieldAndPropertyInfos.Concat(methodInfos);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
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

            if (memberType.IsArray) writer.Write("[2]");

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

        private void WriteResource(string memberName, Type memberType, ShaderMemberAttribute resourceType)
        {
            switch (resourceType)
            {
                case ConstantBufferViewAttribute _:
                    WriteConstantBufferView(memberName, memberType, bindingTracker.ConstantBuffer++);
                    break;
                case ShaderResourceViewAttribute _:
                    WriteShaderResourceView(memberName, memberType, bindingTracker.ShaderResourceView++);
                    break;
                case UnorderedAccessViewAttribute _:
                    WriteUnorderedAccessView(memberName, memberType, bindingTracker.UnorderedAccessView++);
                    break;
                case SamplerAttribute _:
                    WriteSampler(memberName, memberType, bindingTracker.Sampler++);
                    break;
                case StaticResourceAttribute _:
                    WriteStaticResource(memberName, memberType);
                    break;
                default:
                    throw new NotSupportedException("This shader resource type is not supported.");
            }
        }

        private void WriteConstantBufferView(string memberName, Type memberType, int binding)
        {
            writer.Write($"cbuffer {memberName}Buffer");
            writer.WriteLine($" : register(b{binding})");
            writer.WriteLine("{");
            writer.Indent++;
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            if (memberType.IsArray) writer.Write("[2]");
            writer.WriteLine(";");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        private void WriteShaderResourceView(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(t{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteUnorderedAccessView(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(u{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteSampler(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(s{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteStaticResource(string memberName, Type memberType)
        {
            List<string> generatedMemberNames = new List<string>();

            foreach (ResourceDefinition resourceDefinition in collectedTypes.First(d => d.Type == memberType).ResourceDefinitions)
            {
                string generatedMemberName = $"__Generated__{bindingTracker.StaticResource++}__";
                generatedMemberNames.Add(generatedMemberName);

                WriteResource(generatedMemberName, resourceDefinition.MemberType, resourceDefinition.ResourceType);
            }

            writer.Write($"static {HlslKnownTypes.GetMappedName(memberType)} {memberName}");

            if (generatedMemberNames.Count > 0)
            {
                writer.Write(" = { ");

                foreach (string generatedMemberName in generatedMemberNames)
                {
                    writer.Write(generatedMemberName);
                    writer.Write(", ");
                }

                stringWriter.GetStringBuilder().Length -= 2;

                writer.Write(" }");
            }

            writer.WriteLine(";");
            writer.WriteLine();
        }

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

        private bool CanWriteMethod(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(ShaderMemberAttribute)) || methodInfo.DeclaringType.IsInterface;
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
            SyntaxTree syntaxTree = GetSyntaxTree(methodInfo.DeclaringType);
            MethodDeclarationSyntax methodNode = GetMethodDeclaration(methodInfo, syntaxTree);

            ShaderSyntaxCollector syntaxCollector = new ShaderSyntaxCollector(compilation, this);
            syntaxCollector.Visit(methodNode.Body);
        }

        private static MethodDeclarationSyntax GetMethodDeclaration(MethodInfo methodInfo, SyntaxTree syntaxTree)
        {
            SyntaxNode root = syntaxTree.GetRoot();

            MethodDeclarationSyntax methodNode = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .First(n => (n.Identifier.ValueText == methodInfo.Name || n.Identifier.ValueText == DelegateEntryPointName)
                    && n.ParameterList.Parameters.Count == methodInfo.GetParameters().Length);

            return methodNode;
        }

        private void WriteTopLevelMethod(MethodInfo methodInfo, IEnumerable<Attribute>? attributes = null, string? explicitMethodName = null)
        {
            IList<MethodInfo> methodInfos = methodInfo.GetBaseMethods();

            for (int depth = methodInfos.Count - 1; depth >= 0; depth--)
            {
                MethodInfo currentMethodInfo = methodInfos[depth];
                WriteMethod(currentMethodInfo, attributes, depth, explicitMethodName);
            }
        }

        private void WriteMethod(MethodInfo methodInfo, IEnumerable<Attribute>? attributes = null, int depth = 0, string? explicitMethodName = null)
        {
            var methodAttributes = methodInfo.GetCustomAttributes();

            if (attributes != null)
            {
                methodAttributes = methodAttributes.Concat(attributes);
            }

            foreach (Attribute attribute in methodAttributes)
            {
                if (depth == 0 || !(attribute is ShaderAttribute))
                {
                    WriteAttribute(attribute);
                }
            }

            if (methodInfo.IsStatic) writer.Write("static ");

            string methodName = explicitMethodName ?? methodInfo.Name;
            methodName = depth > 0 ? $"Base_{depth}_{methodName}" : methodName;

            writer.Write(HlslKnownTypes.GetMappedName(methodInfo.ReturnType));
            writer.Write(" ");
            writer.Write(methodName);

            WriteParameters(methodInfo);

            if (methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(ShaderSemanticAttribute), true).FirstOrDefault(a => HlslKnownSemantics.ContainsKey(a.GetType())) is ShaderSemanticAttribute returnTypeAttribute)
            {
                writer.Write(GetHlslSemantic(returnTypeAttribute));
            }

            if (methodInfo.GetMethodBody() != null)
            {
                SyntaxTree syntaxTree = GetSyntaxTree(methodInfo.DeclaringType);
                MethodDeclarationSyntax methodNode = GetMethodDeclaration(methodInfo, syntaxTree);

                string methodBody = GetMethodBody(methodNode, depth);

                writer.WriteLine();
                writer.WriteLine(methodBody);
            }
            else
            {
                writer.WriteLine(";");
            }
        }

        private void WriteAttribute(Attribute attribute)
        {
            Type attributeType = attribute.GetType();

            if (HlslKnownAttributes.ContainsKey(attributeType))
            {
                writer.Write("[");
                writer.Write(HlslKnownAttributes.GetMappedName(attributeType));

                var fieldAndPropertyInfos = attributeType.GetMembersInOrder(GetBindingFlagsForType(attributeType) | BindingFlags.DeclaredOnly).Where(m => m is FieldInfo || m is PropertyInfo);
                IEnumerable<object> attributeMemberValues = fieldAndPropertyInfos.Where(m => m.GetMemberValue(attribute) != null).Select(m => m.GetMemberValue(attribute))!;

                if (attributeMemberValues.Count() > 0)
                {
                    writer.Write("(");

                    foreach (object memberValue in attributeMemberValues)
                    {
                        string valueString = memberValue is string ? $"\"{memberValue}\"" : memberValue.ToString();

                        writer.Write(valueString);
                        writer.Write(", ");
                    }

                    stringWriter.GetStringBuilder().Length -= 2;

                    writer.Write(")");
                }

                writer.WriteLine("]");
            }
        }

        private void WriteParameters(MethodInfo methodInfo)
        {
            writer.Write("(");

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            if (parameterInfos.Length > 0)
            {
                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    if (parameterInfo.ParameterType.IsByRef)
                    {
                        string refString = parameterInfo.IsIn ? "in" : parameterInfo.IsOut ? "out" : "inout";
                        writer.Write(refString);
                        writer.Write(" ");
                    }

                    writer.Write($"{HlslKnownTypes.GetMappedName(parameterInfo.ParameterType)} {parameterInfo.Name}");

                    if (parameterInfo.GetCustomAttributes<ShaderSemanticAttribute>().FirstOrDefault(a => HlslKnownSemantics.ContainsKey(a.GetType())) is ShaderSemanticAttribute parameterAttribute)
                    {
                        writer.Write(GetHlslSemantic(parameterAttribute));
                    }

                    writer.Write(", ");
                }

                stringWriter.GetStringBuilder().Length -= 2;
            }

            writer.Write(")");
        }

        private string GetMethodBody(MethodDeclarationSyntax methodNode, int depth = 0)
        {
            ShaderSyntaxRewriter syntaxRewriter = new ShaderSyntaxRewriter(compilation, this, true, depth);
            SyntaxNode newBody = syntaxRewriter.Visit(methodNode.Body);

            string shaderSource = newBody.ToFullString();

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

            return shaderSource.Replace(Environment.NewLine + IndentedTextWriter.DefaultTabString, Environment.NewLine + indent).TrimEnd(' ');
        }

        private static readonly Regex ClosureTypeDeclarationRegex = new Regex(@"(?<=private sealed class )<\w*>[\w_]+", RegexOptions.Compiled);

        private static readonly Regex LambdaMethodDeclarationRegex = new Regex(@"(private|internal) void <\w+>[\w_|]+(?=\()", RegexOptions.Compiled);

        private static SyntaxTree GetSyntaxTree(Type type)
        {
            lock (decompiledTypes)
            {
                if (!decompiledTypes.TryGetValue(type, out SyntaxTree syntaxTree))
                {
                    EntityHandle handle = MetadataTokenHelpers.TryAsEntityHandle(type.MetadataToken) ?? throw new InvalidOperationException();
                    string assemblyPath = type.Assembly.Location;

                    if (!decompilers.TryGetValue(assemblyPath, out CSharpDecompiler decompiler))
                    {
                        decompiler = CreateDecompiler(assemblyPath);
                        decompilers.Add(assemblyPath, decompiler);
                    }

                    string sourceCode = decompiler.DecompileAsString(handle);

                    sourceCode = ClosureTypeDeclarationRegex.Replace(sourceCode, "Shader");
                    sourceCode = LambdaMethodDeclarationRegex.Replace(sourceCode, $"internal void {DelegateEntryPointName}");

                    syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, CSharpParseOptions.Default.WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp8));
                    compilation = compilation.AddSyntaxTrees(syntaxTree);

                    decompiledTypes.Add(type, syntaxTree);
                }

                return syntaxTree;
            }
        }

        private static CSharpDecompiler CreateDecompiler(string assemblyPath)
        {
            UniversalAssemblyResolver resolver = new UniversalAssemblyResolver(assemblyPath, false, "netstandard");

            DecompilerSettings decompilerSettings = new DecompilerSettings(ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp8_0)
            {
                ObjectOrCollectionInitializers = false,
                UsingDeclarations = false
            };

            decompilerSettings.CSharpFormattingOptions.IndentationString = IndentedTextWriter.DefaultTabString;

            return new CSharpDecompiler(assemblyPath, resolver, decompilerSettings);
        }

        private class HlslBindingTracker
        {
            public int ConstantBuffer { get; set; }

            public int Sampler { get; set; }

            public int ShaderResourceView { get; set; }

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
    }
}
