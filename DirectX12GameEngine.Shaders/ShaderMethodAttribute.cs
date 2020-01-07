using System;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ShaderMethodAttribute : Attribute
    {
        public ShaderMethodAttribute()
        {
        }

        public ShaderMethodAttribute(string? shaderSource, params Type[]? dependentTypes)
        {
            ShaderSource = shaderSource;
            DependentTypes = dependentTypes;
        }

        public string? ShaderSource { get; }

        public Type[]? DependentTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GlobalShaderMethodAttribute : ShaderMethodAttribute
    {
        public GlobalShaderMethodAttribute(Type declaringType, string methodName, Type[] parameterTypes, string shaderSource, params Type[] dependentTypes)
            : base(shaderSource, dependentTypes)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
            ParameterTypes = parameterTypes;
        }

        public Type DeclaringType { get; }

        public string MethodName { get; }

        public Type[] ParameterTypes { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AnonymousShaderMethodAttribute : ShaderMethodAttribute
    {
        public AnonymousShaderMethodAttribute(int anonymousMethodIndex) : this(anonymousMethodIndex, null, null)
        {
        }

        public AnonymousShaderMethodAttribute(int anonymousMethodIndex, string? shaderSource, params Type[]? dependentTypes) : base(shaderSource, dependentTypes)
        {
            AnonymousMethodIndex = anonymousMethodIndex;
        }

        public int AnonymousMethodIndex { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GlobalAnonymousShaderMethodAttribute : GlobalShaderMethodAttribute
    {
        public GlobalAnonymousShaderMethodAttribute(int anonymousMethodIndex, Type declaringType, string methodName, Type[] parameterTypes, string shaderSource, params Type[] dependentTypes)
            : base(declaringType, methodName, parameterTypes, shaderSource, dependentTypes)
        {
            AnonymousMethodIndex = anonymousMethodIndex;
        }

        public int AnonymousMethodIndex { get; }
    }
}
