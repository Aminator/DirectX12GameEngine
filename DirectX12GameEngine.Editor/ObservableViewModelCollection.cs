using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

#nullable enable

namespace DirectX12GameEngine.Editor
{
    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>
    {
        private readonly IList<TModel> source;
        private readonly Func<TViewModel, TModel> modelFactory;
        private readonly Func<TModel, TViewModel> viewModelFactory;

        public ObservableViewModelCollection(IList<TModel> source, Func<TViewModel, TModel> modelFactory, Func<TModel, TViewModel> viewModelFactory)
            : base(source.Select(model => viewModelFactory(model)))
        {
            this.source = source;
            this.modelFactory = modelFactory;
            this.viewModelFactory = viewModelFactory;

            if (source is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += OnSourceCollectionChanged;
            }
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnSourceCollectionChanged(e);
        }

        protected virtual TViewModel CreateViewModel(TModel model)
        {
            return viewModelFactory(model);
        }

        protected virtual TModel CreateModel(TViewModel viewModel)
        {
            return modelFactory(viewModel);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            INotifyCollectionChanged? notifyCollection = source as INotifyCollectionChanged;

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged -= OnSourceCollectionChanged;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        source.Insert(e.NewStartingIndex + i, CreateModel((TViewModel)e.NewItems[i]));
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldItems.Count == 1 && source is ObservableCollection<TModel> observableCollection)
                    {
                        observableCollection.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else
                    {
                        List<TModel> items = source.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToList();

                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            source.RemoveAt(e.OldStartingIndex);
                        }

                        for (int i = 0; i < items.Count; i++)
                        {
                            source.Insert(e.NewStartingIndex + i, items[i]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        source.RemoveAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        source.RemoveAt(e.OldStartingIndex);
                    }

                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Reset:
                    source.Clear();

                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        source.Add(CreateModel((TViewModel)e.NewItems[i]));
                    }
                    break;
            }

            if (notifyCollection != null)
            {
                notifyCollection.CollectionChanged += OnSourceCollectionChanged;
            }
        }

        protected virtual void OnSourceCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        Insert(e.NewStartingIndex + i, CreateViewModel((TModel)e.NewItems[i]));
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

                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        Add(CreateViewModel((TModel)e.NewItems[i]));
                    }
                    break;
            }
        }
    }
}
