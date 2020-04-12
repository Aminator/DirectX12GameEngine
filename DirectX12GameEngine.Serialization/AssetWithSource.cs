namespace DirectX12GameEngine.Serialization
{
    public abstract class AssetWithSource : Asset
    {
        public string Source { get; set; } = "";

        public override string? MainSource => Source;
    }
}
