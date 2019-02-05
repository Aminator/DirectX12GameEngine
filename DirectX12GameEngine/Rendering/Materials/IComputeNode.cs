namespace DirectX12GameEngine.Rendering.Materials
{
    public interface IComputeNode
    {
        void Visit(Material material);
    }
}
