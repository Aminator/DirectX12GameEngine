using System;
using System.Collections.Specialized;
using System.ComponentModel;
using DirectX12GameEngine.Engine;
using Microsoft.Toolkit.Mvvm.ObjectModel;
using DirectX12GameEngine.Mvvm.Collections;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class EntityViewModel : ObservableObject
    {
        private EntityViewModel? parent;
        private bool isSelected;

        public EntityViewModel(Entity model)
        {
            Model = model;

            Children = new ObservableViewModelCollection<EntityViewModel, Entity>(Model.Children, vm => vm.Model, (m, i) => new EntityViewModel(m));
            Components = new ObservableViewModelCollection<EntityComponentViewModel, EntityComponent>(Model.Components, vm => vm.Model,(m, i) => new EntityComponentViewModel(m));

            foreach (EntityViewModel child in Children)
            {
                child.Parent = this;
            }

            Children.CollectionChanged += OnChildrenCollectionChanged;
            Model.PropertyChanged += OnModelPropertyChanged;
        }

        public Entity Model { get; }

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

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Name):
                    OnPropertyChanged(nameof(Name));
                    break;
            }
        }
    }
}
