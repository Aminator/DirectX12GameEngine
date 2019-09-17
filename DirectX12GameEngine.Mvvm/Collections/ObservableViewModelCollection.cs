using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace DirectX12GameEngine.Mvvm.Collections
{
    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>
    {
        private readonly TaskScheduler originalTaskScheduler;

        private readonly IList<TModel> modelCollection;
        private readonly Func<TViewModel, TModel> modelFactory;
        private readonly Func<TModel, int, TViewModel> viewModelFactory;

        public ObservableViewModelCollection(IList<TModel> modelCollection, Func<TViewModel, TModel> modelFactory, Func<TModel, int, TViewModel> viewModelFactory)
            : base(modelCollection.Select((model, i) => viewModelFactory(model, i)))
        {
            originalTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            this.modelCollection = modelCollection;
            this.modelFactory = modelFactory;
            this.viewModelFactory = viewModelFactory;

            CollectionChanged += OnViewModelCollectionChanged;

            if (modelCollection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnModelCollectionChanged;
            }
        }

        private void OnViewModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnViewModelCollectionChanged(e);
        }

        private void OnModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnModelCollectionChanged(e);
        }

        protected virtual TViewModel CreateViewModel(TModel model, int index)
        {
            return viewModelFactory(model, index);
        }

        protected virtual TModel CreateModel(TViewModel viewModel)
        {
            return modelFactory(viewModel);
        }

        protected virtual void OnViewModelCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            INotifyCollectionChanged? notifyCollection = modelCollection as INotifyCollectionChanged;

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged -= OnModelCollectionChanged;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        modelCollection.Insert(e.NewStartingIndex + i, CreateModel((TViewModel)e.NewItems[i]));
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count == 1 && modelCollection is ObservableCollection<TModel> observableCollection)
                    {
                        observableCollection.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        List<TModel> items = modelCollection.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            modelCollection.RemoveAt(e.OldStartingIndex);
                        }

                        for (int i = 0; i < items.Count; i++)
                        {
                            modelCollection.Insert(e.NewStartingIndex + i, items[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        modelCollection.RemoveAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        modelCollection.RemoveAt(e.OldStartingIndex);
                    }

                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Reset:
                    modelCollection.Clear();
                    break;
            }

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged += OnModelCollectionChanged;
            }
        }

        protected virtual void OnModelCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged -= OnViewModelCollectionChanged;

            Task collectionChangeTask = Task.Factory.StartNew(() =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            Insert(e.NewStartingIndex + i, CreateViewModel((TModel)e.NewItems[i], e.NewStartingIndex + i));
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        if (e.OldItems.Count == 1)
                        {
                            Move(e.OldStartingIndex, e.NewStartingIndex);
                        }
                        else
                        {
                            List<TViewModel> items = this.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

                            for (int i = 0; i < e.OldItems.Count; i++)
                            {
                                RemoveAt(e.OldStartingIndex);
                            }

                            for (int i = 0; i < items.Count; i++)
                            {
                                Insert(e.NewStartingIndex + i, items[i]);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            RemoveAt(e.OldStartingIndex);
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            RemoveAt(e.OldStartingIndex);
                        }

                        goto case NotifyCollectionChangedAction.Add;
                    case NotifyCollectionChangedAction.Reset:
                        Clear();
                        break;
                }
            }, default, TaskCreationOptions.None, originalTaskScheduler);

            collectionChangeTask.Wait();

            CollectionChanged += OnViewModelCollectionChanged;
        }
    }

    public class ObservableViewModelCollection<TViewModel> : ObservableCollection<TViewModel>
    {
        private readonly IList modelCollection;
        private readonly Func<TViewModel, object?> modelFactory;
        private readonly Func<object?, int, TViewModel> viewModelFactory;

        public ObservableViewModelCollection(IList modelCollection, Func<TViewModel, object?> modelFactory, Func<object?, int, TViewModel> viewModelFactory)
            : base(modelCollection.Cast<object?>().Select((model, i) => viewModelFactory(model, i)))
        {
            this.modelCollection = modelCollection;
            this.modelFactory = modelFactory;
            this.viewModelFactory = viewModelFactory;

            CollectionChanged += OnViewModelCollectionChanged;

            if (modelCollection is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnModelCollectionChanged;
            }
        }

        private void OnViewModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnViewModelCollectionChanged(e);
        }

        private void OnModelCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnModelCollectionChanged(e);
        }

        protected virtual TViewModel CreateViewModel(object? model, int index)
        {
            return viewModelFactory(model, index);
        }

        protected virtual object? CreateModel(TViewModel viewModel)
        {
            return modelFactory(viewModel);
        }

        protected virtual void OnViewModelCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            INotifyCollectionChanged? notifyCollection = modelCollection as INotifyCollectionChanged;

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged -= OnModelCollectionChanged;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        modelCollection.Insert(e.NewStartingIndex + i, CreateModel((TViewModel)e.NewItems[i]));
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count == 1 && modelCollection is ObservableCollection<object> observableCollection)
                    {
                        observableCollection.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        List<object> items = modelCollection.Cast<object>().Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            modelCollection.RemoveAt(e.OldStartingIndex);
                        }

                        for (int i = 0; i < items.Count; i++)
                        {
                            modelCollection.Insert(e.NewStartingIndex + i, items[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        modelCollection.RemoveAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        modelCollection.RemoveAt(e.OldStartingIndex);
                    }

                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Reset:
                    modelCollection.Clear();
                    break;
            }

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged += OnModelCollectionChanged;
            }
        }

        protected virtual void OnModelCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged -= OnViewModelCollectionChanged;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        Insert(e.NewStartingIndex + i, CreateViewModel(e.NewItems[i], e.NewStartingIndex + i));
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count == 1)
                    {
                        Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        List<TViewModel> items = this.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            RemoveAt(e.OldStartingIndex);
                        }

                        for (int i = 0; i < items.Count; i++)
                        {
                            Insert(e.NewStartingIndex + i, items[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        RemoveAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        RemoveAt(e.OldStartingIndex);
                    }

                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
            }

            CollectionChanged += OnViewModelCollectionChanged;
        }
    }
}
