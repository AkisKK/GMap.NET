using System.Linq;

namespace GMap.NET.WindowsForms.ObjectModel;

public class ObservableCollectionThreadSafe<T> : ObservableCollection<T>
{
    NotifyCollectionChangedEventHandler m_CollectionChanged;

    public override event NotifyCollectionChangedEventHandler CollectionChanged
    {
        add => m_CollectionChanged += value;
        remove => m_CollectionChanged -= value;
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // Be nice - use BlockReentrancy like MSDN said
        using (BlockReentrancy())
        {
            if (m_CollectionChanged != null)
            {
                var delegates = m_CollectionChanged.GetInvocationList();

                // Walk through invocation list.
                foreach (var handler in delegates.Cast<NotifyCollectionChangedEventHandler>())
                {
                    // If the subscriber is a DispatcherObject and different thread
                    if (handler.Target is System.Windows.Forms.Control dispatcherObject && dispatcherObject.InvokeRequired)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcherObject.Invoke(handler, this, e);
                    }
                    else // Execute handler as is 
                    {
                        m_CollectionChanged(this, e);
                    }
                }
            }
        }
    }
}
