namespace DirectX12GameEngine.Graphics
{
    public struct InputElementDescription
    {
        public const int AppendAligned = -1;

        public string SemanticName;

        public int SemanticIndex;

        public PixelFormat Format;

        public int Slot;

        public int AlignedByteOffset;

        public InputClassification Classification;

        public int InstanceDataStepRate;

        public InputElementDescription(string semanticName, int semanticIndex, PixelFormat format, int slot) : this(semanticName, semanticIndex, format, 0, slot)
        {
        }

        public InputElementDescription(string semanticName, int semanticIndex, PixelFormat format, int offset, int slot) : this(semanticName, semanticIndex, format, offset, slot, InputClassification.PerVertexData, 0)
        {
        }

        public InputElementDescription(string semanticName, int semanticIndex, PixelFormat format, int offset, int slot, InputClassification classification, int instanceDataStepRate)
        {
            SemanticName = semanticName;
            SemanticIndex = semanticIndex;
            Format = format;
            Slot = slot;
            AlignedByteOffset = offset;
            Classification = classification;
            InstanceDataStepRate = instanceDataStepRate;
        }
    }
}
