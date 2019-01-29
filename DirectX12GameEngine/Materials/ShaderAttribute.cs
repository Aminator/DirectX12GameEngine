namespace DirectX12GameEngine
{
    public class ShaderAttribute : ShaderMethodAttribute
    {
        public ShaderAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
