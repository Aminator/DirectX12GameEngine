using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DirectX12GameEngine.Engine
{
    internal sealed class TransformCollectionWrapper : IList<Entity>, IList
    {
        private readonly IList<TransformComponent> collection;

        public TransformCollectionWrapper(IList<TransformComponent> collection)
        {
            this.collection = collection;
        }

        public Entity this[int index] { get => collection[index].Entity; set => collection[index] = value.Transform; }

        public int Count => collection.Count;

        public bool IsReadOnly => collection.IsReadOnly;

        public void Add(Entity item) => collection.Add(item.Transform);

        public void Clear() => collection.Clear();

        public bool Contains(Entity item) => collection.Contains(item.Transform);

        public void CopyTo(Entity[] array, int arrayIndex) => throw new NotImplementedException();

        public IEnumerator<Entity> GetEnumerator() => collection.Select(c => c.Entity).GetEnumerator();

        public int IndexOf(Entity item) => collection.IndexOf(item.Transform);

        public void Insert(int index, Entity item) => collection.Insert(index, item.Transform);

        public bool Remove(Entity item) => collection.Remove(item.Transform);

        public void RemoveAt(int index) => collection.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        object IList.this[int index] { get => this[index]; set => this[index] = (Entity)value; }

        bool IList.IsFixedSize => ((IList)collection).IsFixedSize;

        bool ICollection.IsSynchronized => ((IList)collection).IsSynchronized;

        object ICollection.SyncRoot => ((IList)collection).SyncRoot;

        int IList.Add(object value) => ((IList)collection).Add(((Entity)value).Transform);

        bool IList.Contains(object value) => Contains((Entity)value);

        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();

        int IList.IndexOf(object value) => IndexOf((Entity)value);

        void IList.Insert(int index, object value) => Insert(index, (Entity)value);

        void IList.Remove(object value) => Remove((Entity)value);
    }
}
