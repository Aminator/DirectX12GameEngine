using System;
using System.Collections.Specialized;
using System.ComponentModel;
using DirectX12GameEngine.Engine;

#nullable enable

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityViewModel : ViewModelBase<Entity>
    {
        private EntityViewModel? parent;
        private bool isSelected;

        public EntityViewModel(Entity model) : base(model)
        {
            Children = new ObservableViewModelCollection<EntityViewModel, Entity>(Model.Children, vm => vm.Model, (m, i) => new EntityViewModel(m));
            Components = new ObservableViewModelCollection<EntityComponentViewModel, EntityComponent>(Model.Components, vm => vm.Model,(m, i) => new EntityComponentViewModel(m));

            foreach (EntityViewModel child in Children)
            {
                child.Parent = this;
            }

            Children.CollectionChanged += Children_CollectionChanged;
            Model.PropertyChanged += Model_PropertyChanged;
        }

        public EntityViewModel? Parent
        {
            get => parent;
            private set => Set(ref parent, value);
        }

        public bool IsSelected
        {
            get => isSelected;
            set => Set(ref isSelected, value);
        }

        public Guid Id
        {
            get => Model.Id;
            set => Set(Model.Id, value, () => Model.Id = value);
        }

        public string Name
        {
            get => Model.Name;
            set => Set(Model.Name, value, () => Model.Name = value);
        }

        public ObservableViewModelCollection<EntityViewModel, Entity> Children { get; }

        public ObservableViewModelCollection<EntityComponentViewModel, EntityComponent> Components { get; }

        private void AddInternal(EntityViewModel entity)
        {
            if (entity.Parent != null)
            {
                throw new InvalidOperationException("This entity already has parent.");
            }

            entity.Parent = this;
        }

        private void RemoveInternal(EntityViewModel entity)
        {
            if (entity.Parent != this)
            {
                throw new InvalidOperationException("This entity is not a child of the expected parent.");
            }

            entity.Parent = null;
        }

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (EntityViewModel entity in e.NewItems)
                    {
                        AddInternal(entity);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (EntityViewModel entity in e.OldItems)
                    {
                        RemoveInternal(entity);
                    }
                    break;
            }
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Id):
                    NotifyPropertyChanged(nameof(Id));
                    break;
                case nameof(Name):
                    NotifyPropertyChanged(nameof(Name));
                    break;
            }
        }
    }
}
