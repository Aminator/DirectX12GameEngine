using System;
using System.Collections.Generic;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        private void IncrementReference(Reference reference, bool isPublicReference)
        {
            if (isPublicReference)
            {
                reference.PublicReferenceCount++;
            }
            else
            {
                reference.PrivateReferenceCount++;
            }
        }

        private void DecrementReference(Reference reference, bool isPublicReference)
        {
            if (isPublicReference)
            {
                if (reference.PublicReferenceCount <= 0)
                {
                    throw new InvalidOperationException();
                }

                reference.PublicReferenceCount--;
            }
            else
            {
                if (reference.PrivateReferenceCount <= 0)
                {
                    throw new InvalidOperationException();
                }

                reference.PrivateReferenceCount--;
            }

            int referenceCount = reference.PublicReferenceCount + reference.PrivateReferenceCount;

            if (referenceCount == 0)
            {
                ReleaseAsset(reference);

                foreach (Reference childReference in reference.References)
                {
                    DecrementReference(childReference, false);
                }
            }
        }

        private void ReleaseAsset(Reference reference)
        {
            if (reference.Object is IDisposable disposable)
            {
                disposable.Dispose();
            }

            loadedAssetPaths.Remove(reference.Path);
            loadedAssetReferences.Remove(reference.Object);
        }

        internal class Reference
        {
            public Reference(string path, object obj, bool isPublicReference)
            {
                Path = path;
                Object = obj;
                PublicReferenceCount = isPublicReference ? 1 : 0;
                PrivateReferenceCount = isPublicReference ? 0 : 1;
            }

            public bool IsDeserialized { get; set; }

            public string Path { get; }

            public object Object { get; }

            public int PublicReferenceCount { get; set; }

            public int PrivateReferenceCount { get; set; }

            public HashSet<Reference> References { get; set; } = new HashSet<Reference>();
        }
    }
}
