namespace DirectX12GameEngine.Rendering.Materials
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
