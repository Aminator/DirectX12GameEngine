using System;

namespace DirectX12GameEngine.Shaders
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
    public abstract class ShaderSemanticAttribute : Attribute
    {
    }

    public abstract class ShaderSemanticWithIndexAttribute : ShaderSemanticAttribute
    {
        public ShaderSemanticWithIndexAttribute(int index = 0)
        {
            Index = index;
        }

        public int Index { get; }
    }

    [ShaderType("Position")]
    public class PositionSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public PositionSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("Normal")]
    public class NormalSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public NormalSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("TexCoord")]
    public class TextureCoordinateSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public TextureCoordinateSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("Color")]
    public class ColorSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public ColorSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("Tangent")]
    public class TangentSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public TangentSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("SV_Target")]
    public class SystemTargetSemanticAttribute : ShaderSemanticWithIndexAttribute
    {
        public SystemTargetSemanticAttribute(int index = 0) : base(index)
        {
        }
    }

    [ShaderType("SV_DispatchThreadId")]
    public class SystemDispatchThreadIdSemanticAttribute : ShaderSemanticAttribute
    {
    }

    [ShaderType("SV_InstanceId")]
    public class SystemInstanceIdSemanticAttribute : ShaderSemanticAttribute
    {
    }

    [ShaderType("SV_IsFrontFace")]
    public class SystemIsFrontFaceSemanticAttribute : ShaderSemanticAttribute
    {
    }

    [ShaderType("SV_Position")]
    public class SystemPositionSemanticAttribute : ShaderSemanticAttribute
    {
    }

    [ShaderType("SV_RenderTargetArrayIndex")]
    public class SystemRenderTargetArrayIndexSemanticAttribute : ShaderSemanticAttribute
    {
    }
}
