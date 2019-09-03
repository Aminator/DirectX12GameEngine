using System;
using Vortice.Dxc;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderCompiler
    {
        static ShaderCompiler()
        {
            Dxil.LoadLibrary();
        }

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName = "")
        {
            return Compile(shaderStage, source, entryPoint, sourceName, DxcShaderModel.Model6_3);
        }

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName, DxcShaderModel shaderModel)
        {
            return Compile(shaderStage, source, entryPoint, sourceName, new DxcCompilerOptions { ShaderModel = shaderModel, PackMatrixInRowMajor = true });
        }

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName, DxcCompilerOptions options)
        {
            IDxcOperationResult result = DxcCompiler.Compile(shaderStage, source, entryPoint, sourceName, options);

            if (result.GetStatus() == 0)
            {
                IDxcBlob blob = result.GetResult();
                return Dxc.GetBytesFromBlob(blob);
            }
            else
            {
                string resultText = Dxc.GetStringFromBlob(DxcCompiler.Library, result.GetErrors());
                throw new Exception(resultText);
            }
        }
    }
}
