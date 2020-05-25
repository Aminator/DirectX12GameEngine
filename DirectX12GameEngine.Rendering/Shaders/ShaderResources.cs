using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using DirectX12GameEngine.Graphics;
using DirectX12GameEngine.Shaders;
using DirectX12GameEngine.Shaders.Numerics;

namespace DirectX12GameEngine.Rendering
{
    [ShaderType("SamplerState")]
    [Sampler]
    public class SamplerState : Sampler
    {
        public SamplerState(Sampler sampler) : base(sampler)
        {
        }
    }

    [ShaderType("SamplerComparisonState")]
    [Sampler]
    public class SamplerComparisonState : Sampler
    {
        public SamplerComparisonState(Sampler sampler) : base(sampler)
        {
        }
    }

    [ShaderType("Texture2D")]
    [ShaderResourceView]
    public class Texture2D<T> : ShaderResourceView where T : unmanaged
    {
        public Texture2D(ShaderResourceView shaderResourceView) : base(shaderResourceView)
        {
        }

        public T Sample(Sampler sampler, Vector2 textureCoordinate) => throw new NotImplementedException();
    }

    [ShaderType("Texture2D")]
    [ShaderResourceView]
    public class Texture2D : Texture2D<Vector4>
    {
        public Texture2D(ShaderResourceView shaderResourceView) : base(shaderResourceView)
        {
        }
    }

    [ShaderType("RWTexture2D")]
    [UnorderedAccessView]
    public class WriteableTexture2D<T> : UnorderedAccessView where T : unmanaged
    {
        public WriteableTexture2D(UnorderedAccessView unorderedAccessView) : base(unorderedAccessView)
        {
        }

        public T this[Int2 index]
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    [ShaderType("RWTexture2D")]
    [UnorderedAccessView]
    public class WriteableTexture2D : WriteableTexture2D<Vector4>
    {
        public WriteableTexture2D(UnorderedAccessView unorderedAccessView) : base(unorderedAccessView)
        {
        }
    }

    [ShaderType("StructuredBuffer")]
    [ShaderResourceView]
    public class StructuredBuffer<T> : ShaderResourceView where T : unmanaged
    {
        public StructuredBuffer(ShaderResourceView shaderResourceView) : base(shaderResourceView)
        {
        }

        public T this[int index] => Resource.GetData<T>(index * Unsafe.SizeOf<T>());
    }

    [ShaderType("RWStructuredBuffer")]
    [UnorderedAccessView]
    public class WriteableStructuredBuffer<T> : UnorderedAccessView where T : unmanaged
    {
        public WriteableStructuredBuffer(UnorderedAccessView unorderedAccessView) : base(unorderedAccessView)
        {
        }

        public T this[int index]
        {
            get => Resource.GetData<T>(index * Unsafe.SizeOf<T>());
            set => Resource.SetData(value, index * Unsafe.SizeOf<T>());
        }
    }

    [ShaderType("ConstantBuffer")]
    [ConstantBufferView]
    public class ConstantBuffer<T> : ConstantBufferView where T : unmanaged
    {
        public ConstantBuffer(ConstantBufferView constantBufferView) : base(constantBufferView)
        {
        }

        private T Value => Resource.GetData<T>();

        public static implicit operator T(ConstantBuffer<T> constantBuffer) => constantBuffer.Value;
    }
}
