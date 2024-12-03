using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GMap.NET.WindowsForms.ObjectModel;

public delegate void NotifyCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e);

public interface INotifyCollectionChanged
{
    // Events
    event NotifyCollectionChangedEventHandler CollectionChanged;
}

public interface INotifyPropertyChanged
{
    // Events
    event PropertyChangedEventHandler PropertyChanged;
}

public enum NotifyCollectionChangedAction
{
    Add,
    Remove,
    Replace,
    Move,
    Reset
}

public class NotifyCollectionChangedEventArgs : EventArgs
{
    // Fields
    private NotifyCollectionChangedAction m_Action;
    private IList m_NewItems;
    private int m_NewStartingIndex;
    private IList m_OldItems;
    private int m_OldStartingIndex;

    // Methods
    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        InitializeAdd(action, null, -1);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Add && action != NotifyCollectionChangedAction.Remove &&
            action != NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", nameof(action));
        }

        if (action == NotifyCollectionChangedAction.Reset)
        {
            if (changedItems != null)
            {
                throw new ArgumentException("ResetActionRequiresNullItem", nameof(action));
            }

            InitializeAdd(action, null, -1);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(changedItems);

            InitializeAddOrRemove(action, changedItems, -1);
        }
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Add && action != NotifyCollectionChangedAction.Remove &&
            action != NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", nameof(action));
        }

        if (action == NotifyCollectionChangedAction.Reset)
        {
            if (changedItem != null)
            {
                throw new ArgumentException("ResetActionRequiresNullItem", nameof(action));
            }

            InitializeAdd(action, null, -1);
        }
        else
        {
            InitializeAddOrRemove(action, new[] { changedItem }, -1);
        }
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Replace)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        ArgumentNullException.ThrowIfNull(newItems);
        ArgumentNullException.ThrowIfNull(oldItems);

        InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems,
        int startingIndex)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Add
            && action != NotifyCollectionChangedAction.Remove
            && action != NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", nameof(action));
        }

        if (action == NotifyCollectionChangedAction.Reset)
        {
            if (changedItems != null)
            {
                throw new ArgumentException("ResetActionRequiresNullItem", nameof(action));
            }

            if (startingIndex != -1)
            {
                throw new ArgumentException("ResetActionRequiresIndexMinus1", nameof(action));
            }

            InitializeAdd(action, null, -1);
        }
        else
        {
            ArgumentNullException.ThrowIfNull(changedItems);
            if (startingIndex < -1)
            {
                throw new ArgumentException("IndexCannotBeNegative", nameof(startingIndex));
            }

            InitializeAddOrRemove(action, changedItems, startingIndex);
        }
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Add && action != NotifyCollectionChangedAction.Remove &&
            action != NotifyCollectionChangedAction.Reset)
        {
            throw new ArgumentException("MustBeResetAddOrRemoveActionForCtor", nameof(action));
        }

        if (action == NotifyCollectionChangedAction.Reset)
        {
            if (changedItem != null)
            {
                throw new ArgumentException("ResetActionRequiresNullItem", nameof(action));
            }

            if (index != -1)
            {
                throw new ArgumentException("ResetActionRequiresIndexMinus1", nameof(action));
            }

            InitializeAdd(action, null, -1);
        }
        else
        {
            InitializeAddOrRemove(action, new[] { changedItem }, index);
        }
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Replace)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        InitializeMoveOrReplace(action, new[] { newItem }, new[] { oldItem }, -1, -1);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList newItems, IList oldItems,
        int startingIndex)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Replace)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        ArgumentNullException.ThrowIfNull(newItems);
        ArgumentNullException.ThrowIfNull(oldItems);

        InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, IList changedItems, int index,
        int oldIndex)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Move)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        if (index < 0)
        {
            throw new ArgumentException("IndexCannotBeNegative", nameof(index));
        }

        InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object changedItem, int index,
        int oldIndex)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Move)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        if (index < 0)
        {
            throw new ArgumentException("IndexCannotBeNegative", nameof(index));
        }

        object[] newItems = [changedItem];
        InitializeMoveOrReplace(action, newItems, newItems, index, oldIndex);
    }

    public NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction action, object newItem, object oldItem,
        int index)
    {
        m_NewStartingIndex = -1;
        m_OldStartingIndex = -1;
        if (action != NotifyCollectionChangedAction.Replace)
        {
            throw new ArgumentException("WrongActionForCtor", nameof(action));
        }

        InitializeMoveOrReplace(action, new[] { newItem }, new[] { oldItem }, index, index);
    }

    private void InitializeAdd(NotifyCollectionChangedAction action, IList newItems, int newStartingIndex)
    {
        m_Action = action;
        m_NewItems = newItems == null ? null : ArrayList.ReadOnly(newItems);
        m_NewStartingIndex = newStartingIndex;
    }

    private void InitializeAddOrRemove(NotifyCollectionChangedAction action, IList changedItems, int startingIndex)
    {
        if (action == NotifyCollectionChangedAction.Add)
        {
            InitializeAdd(action, changedItems, startingIndex);
        }
        else if (action == NotifyCollectionChangedAction.Remove)
        {
            InitializeRemove(action, changedItems, startingIndex);
        }
        else
        {
            throw new ArgumentException(string.Format("InvariantFailure, Unsupported action: {0}",
                action.ToString()));
        }
    }

    private void InitializeMoveOrReplace(NotifyCollectionChangedAction action, IList newItems, IList oldItems,
        int startingIndex, int oldStartingIndex)
    {
        InitializeAdd(action, newItems, startingIndex);
        InitializeRemove(action, oldItems, oldStartingIndex);
    }

    private void InitializeRemove(NotifyCollectionChangedAction action, IList oldItems, int oldStartingIndex)
    {
        m_Action = action;
        m_OldItems = oldItems == null ? null : ArrayList.ReadOnly(oldItems);
        m_OldStartingIndex = oldStartingIndex;
    }

    // Properties
    public NotifyCollectionChangedAction Action => m_Action;

    public IList NewItems => m_NewItems;

    public int NewStartingIndex => m_NewStartingIndex;

    public IList OldItems => m_OldItems;

    public int OldStartingIndex => m_OldStartingIndex;
}

[Serializable]
public class ObservableCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    // Fields
    protected Collection<T> _inner;

    protected object _lock = new();

    private readonly SimpleMonitor m_Monitor;
    private const string CountString = "Count";
    private const string IndexerName = "Item[]";

    // Events
    [field: NonSerialized] public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

    [field: NonSerialized] protected event PropertyChangedEventHandler PropertyChanged;

    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _inner.Count;
            }
        }
    }

    public bool IsReadOnly => false;

    // Methods
    public ObservableCollection()
    {
        m_Monitor = new SimpleMonitor();
        _inner = [];
    }

    public ObservableCollection(IEnumerable<T> collection)
    {
        m_Monitor = new SimpleMonitor();
        ArgumentNullException.ThrowIfNull(collection);
        _inner = [];

        CopyFrom(collection);
    }

    public ObservableCollection(List<T> list)
    {
        m_Monitor = new SimpleMonitor();
        _inner = new Collection<T>(list != null ? new List<T>(list.Count) : list);

        CopyFrom(list);
    }

    protected IDisposable BlockReentrancy()
    {
        m_Monitor.Enter();
        return m_Monitor;
    }

    protected void CheckReentrancy()
    {
        if (m_Monitor.Busy && CollectionChanged != null &&
            CollectionChanged.GetInvocationList().Length > 1)
        {
            throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed");
        }
    }

    public T this[int index]
    {
        get
        {
            lock (_lock)
            {
                return _inner[index];
            }
        }
        set => SetItem(index, value);
    }

    protected void ClearItems()
    {
        lock (_lock)
        {
            CheckReentrancy();
            _inner.Clear();
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionReset();
        }
    }

    private void CopyFrom(IEnumerable<T> collection)
    {
        if (collection is null)
        {
            // Exit early.
            return;
        }

        lock (_lock)
        {
            using var enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext())
            {
                AddItem(enumerator.Current);
            }
        }
    }

    protected void AddItem(T item)
    {
        lock (_lock)
        {
            CheckReentrancy();
            _inner.Add(item);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, _inner.Count - 1);
        }
    }
    protected void InsertItem(int index, T item)
    {
        lock (_lock)
        {
            CheckReentrancy();
            _inner.Insert(index, item);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
    }

    public void Move(int oldIndex, int newIndex)
    {
        MoveItem(oldIndex, newIndex);
    }

    protected virtual void MoveItem(int oldIndex, int newIndex)
    {
        lock (_lock)
        {
            CheckReentrancy();
            var item = _inner[oldIndex];
            _inner.RemoveAt(oldIndex);
            _inner.Insert(newIndex, item);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex);
        }
    }
    protected void RemoveItem(int index)
    {
        lock (_lock)
        {
            CheckReentrancy();
            var item = _inner[index];
            _inner.RemoveAt(index);
            OnPropertyChanged(CountString);
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
        }
    }

    protected void SetItem(int index, T item)
    {
        lock (_lock)
        {
            CheckReentrancy();
            var oldItem = _inner[index];
            _inner[index] = item;
            OnPropertyChanged(IndexerName);
            OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
        }
    }

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (CollectionChanged is null)
        {
            // Exit early.
            return;
        }

        using (BlockReentrancy())
        {
            CollectionChanged(this, e);
        }
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem,
        int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
    }

    private void OnCollectionReset()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    private void OnPropertyChanged(string propertyName)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }


    public void Add(T item) => AddItem(item);

    public void Clear() => ClearItems();

    public bool Contains(T item)
    {
        lock (_lock)
        {
            return _inner.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        lock (_lock)
        {
            _inner.CopyTo(array, arrayIndex);
        }
    }

    public bool Remove(T item)
    {
        RemoveItem(_inner.IndexOf(item));
        return true;
    }

    public void RemoveAt(int index) => RemoveItem(index);

    public void Insert(int index, T item)
    {
        InsertItem(index, item);
    }

    public int IndexOf(T item)
    {
        lock (_lock)
        {
            return _inner.IndexOf(item);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        // instead of returning an unsafe enumerator,
        // we wrap it into our thread-safe class
        lock (_lock)
        {
            return new ThreadSafeEnumerator<T>(_inner.GetEnumerator(), _lock);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Nested Types
    [Serializable]
    private class SimpleMonitor : IDisposable
    {
        // Fields
        private int m_BusyCount;

        // Methods
        public void Dispose()
        {
            m_BusyCount--;
        }

        public void Enter()
        {
            m_BusyCount++;
        }

        // Properties
        public bool Busy => m_BusyCount > 0;
    }
}
