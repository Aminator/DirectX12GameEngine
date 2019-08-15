using System;
using System.IO;
using DotNetDxc;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderCompiler
    {
        static ShaderCompiler()
        {
            HlslDxcLib.DxcCreateInstanceFn = DefaultDxcLib.GetDxcCreateInstanceFn();
        }

        public static byte[] CompileShaderFile(string filePath, ShaderProfile profile, ShaderModel model = ShaderModel.Model6_1, string? entryPoint = null)
        {
            string shaderSource = File.ReadAllText(filePath);
            return CompileShader(shaderSource, profile, model, entryPoint, filePath);
        }

        public static byte[] CompileShader(string shaderSource, ShaderProfile profile, ShaderModel model = ShaderModel.Model6_1, string? entryPoint = null, string filePath = "")
        {
            IDxcCompiler compiler = HlslDxcLib.CreateDxcCompiler();
            IDxcLibrary library = HlslDxcLib.CreateDxcLibrary();

            const uint CP_UTF16 = 1200;

            IDxcBlobEncoding sourceBlob = library.CreateBlobWithEncodingOnHeapCopy(shaderSource, (uint)(shaderSource.Length * 2), CP_UTF16);
            IDxcOperationResult result = compiler.Compile(sourceBlob, filePath, entryPoint ?? GetDefaultEntryPoint(profile), $"{GetShaderProfile(profile)}_{GetShaderModel(model)}", new[] { "-Zpr" }, 1, null, 0, library.CreateIncludeHandler());

            if (result.GetStatus() == 0)
            {
                IDxcBlob blob = result.GetResult();
                byte[] bytecode = GetBytesFromBlob(blob);

                return bytecode;
            }
            else
            {
                string resultText = GetStringFromBlob(library, result.GetErrors());
                throw new Exception(resultText);
            }
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

        public static unsafe string GetStringFromBlob(IDxcLibrary library, IDxcBlob blob)
        {
            blob = library.GetBlobAstUf16(blob);
            return new string(blob.GetBufferPointer(), 0, (int)(blob.GetBufferSize() / 2));
        }

        private static string GetDefaultEntryPoint(ShaderProfile profile) => profile switch
        {
            ShaderProfile.ComputeShader => "CSMain",
            ShaderProfile.VertexShader => "VSMain",
            ShaderProfile.PixelShader => "PSMain",
            ShaderProfile.HullShader => "HSMain",
            ShaderProfile.DomainShader => "DSMain",
            ShaderProfile.GeometryShader => "GSMain",
            _ => ""
        };

        private static string GetShaderProfile(ShaderProfile profile) => profile switch
        {
            ShaderProfile.ComputeShader => "cs",
            ShaderProfile.VertexShader => "vs",
            ShaderProfile.PixelShader => "ps",
            ShaderProfile.HullShader => "hs",
            ShaderProfile.DomainShader => "ds",
            ShaderProfile.GeometryShader => "gs",
            ShaderProfile.Library => "lib",
            _ => throw new NotSupportedException()
        };

        private static string GetShaderModel(ShaderModel model) => model switch
        {
            ShaderModel.Model6_0 => "6_0",
            ShaderModel.Model6_1 => "6_1",
            ShaderModel.Model6_2 => "6_2",
            ShaderModel.Model6_3 => "6_3",
            ShaderModel.Model6_4 => "6_4",
            _ => throw new NotSupportedException()
        };
    }
}
