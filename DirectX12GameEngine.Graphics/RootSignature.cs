using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace DirectX12GameEngine.Graphics
{
    public sealed class RootSignature : IDisposable
    {
        public RootSignature(GraphicsDevice device, RootSignatureDescription description)
        {
            GraphicsDevice = device;

            RootSignatureDescription1 nativeDescription = new RootSignatureDescription1((Vortice.Direct3D12.RootSignatureFlags)description.Flags)
            {
                Parameters = description.Parameters?.Select(p => Unsafe.As<RootParameter, RootParameter1>(ref p)).ToArray(),
                StaticSamplers = description.StaticSamplers?.Select(s => Unsafe.As<StaticSamplerDescription, Vortice.Direct3D12.StaticSamplerDescription>(ref s)).ToArray()
            };

            NativeRootSignature = GraphicsDevice.NativeDevice.CreateRootSignature(new VersionedRootSignatureDescription(nativeDescription));
        }

        public GraphicsDevice GraphicsDevice { get; }

        internal ID3D12RootSignature NativeRootSignature { get; }

        public void Dispose()
        {
            NativeRootSignature.Dispose();
        }
    }

    public class RootSignatureDescription
    {
        public RootSignatureDescription()
        {
        }

        public RootSignatureDescription(RootSignatureFlags flags, RootParameter[]? parameters = null, StaticSamplerDescription[]? staticSamplers = null)
        {
            Flags = flags;
            Parameters = parameters;
            StaticSamplers = staticSamplers;
        }

        public RootParameter[]? Parameters { get; set; }

        public StaticSamplerDescription[]? StaticSamplers { get; set; }

        public RootSignatureFlags Flags { get; set; }
    }

    public struct StaticSamplerDescription
    {
        public Filter Filter;

        public TextureAddressMode AddressU;

        public TextureAddressMode AddressV;

        public TextureAddressMode AddressW;

        public float MipLodBias;

        public int MaxAnisotropy;

        public ComparisonFunction ComparisonFunction;

        public StaticBorderColor BorderColor;

        public float MinLod;

        public float MaxLod;

        public int ShaderRegister;

        public int RegisterSpace;

        public ShaderVisibility ShaderVisibility;

        public StaticSamplerDescription(ShaderVisibility shaderVisibility, int shaderRegister, int registerSpace)
        {
            Filter = Filter.MinMagMipLinear;
            AddressU = TextureAddressMode.Clamp;
            AddressV = TextureAddressMode.Clamp;
            AddressW = TextureAddressMode.Clamp;
            MipLodBias = 0.0f;
            MaxAnisotropy = 1;
            ComparisonFunction = ComparisonFunction.Never;
            BorderColor = StaticBorderColor.TransparentBlack;
            MinLod = float.MinValue;
            MaxLod = float.MaxValue;

            ShaderRegister = shaderRegister;
            RegisterSpace = registerSpace;
            ShaderVisibility = shaderVisibility;
        }
    }
}
