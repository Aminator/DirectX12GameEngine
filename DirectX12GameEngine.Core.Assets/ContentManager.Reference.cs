using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Core.Assets
{
    public partial class ContentManager
    {
        private Reference? FindDeserializedObject(string path, Type type)
        {
            if (loadedAssetPaths.TryGetValue(path, out Reference? reference))
            {
                while (reference != null && !type.IsAssignableFrom(reference.Object.GetType()))
                {
                    reference = reference.Next;
                }
            }

            return reference;
        }

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
                ReleaseReference(reference);

                foreach (Reference childReference in reference.References)
                {
                    DecrementReference(childReference, false);
                }
            }
        }

        private void AddReference(Reference reference)
        {
            lock (loadedAssetPaths)
            {
                if (loadedAssetPaths.TryGetValue(reference.Path, out Reference previousReference))
                {
                    reference.Next = previousReference.Next;
                    reference.Previous = previousReference;

                    if (previousReference.Next != null)
                    {
                        previousReference.Next.Previous = reference;
                    }

                    previousReference.Next = reference;
                }
                else
                {
                    loadedAssetPaths[reference.Path] = reference;
                }

                loadedAssetReferences[reference.Object] = reference;
            }
        }

        private void ReleaseReference(Reference reference)
        {
            //if (reference.Object is IDisposable disposable)
            //{
            //    disposable.Dispose();
            //}

            Reference? previous = reference.Previous;
            Reference? next = reference.Next;

            if (previous != null)
            {
                previous.Next = next;
            }

            if (next != null)
            {
                next.Previous = previous;
            }

            if (previous is null)
            {
                if (next is null)
                {
                    loadedAssetPaths.Remove(reference.Path);
                }
                else
                {
                    loadedAssetPaths[reference.Path] = next;
                }
            }

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

            public string Path { get; }

            public object Object { get; }

            public int PublicReferenceCount { get; set; }

            public int PrivateReferenceCount { get; set; }

            public Task? DeserializationTask { get; set; }

            public HashSet<Reference> References { get; set; } = new HashSet<Reference>();

            public Reference? Next { get; set; }

            public Reference? Previous { get; set; }
        }
    }
}
