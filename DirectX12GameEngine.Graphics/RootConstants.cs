namespace DirectX12GameEngine.Graphics
{
    public struct RootConstants
    {
        public int ShaderRegister;

        public int RegisterSpace;

        public int Num32BitValues;

        public RootConstants(int shaderRegister, int registerSpace, int num32BitValues)
        {
            ShaderRegister = shaderRegister;
            RegisterSpace = registerSpace;
            Num32BitValues = num32BitValues;
        }
    }
}
