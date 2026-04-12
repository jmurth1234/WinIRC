using System.Collections.ObjectModel;

namespace WinIRC.Core.Irc;

public sealed class BoundedObservableCollection<T> : ObservableCollection<T>
{
    private readonly int maxItems;

    public BoundedObservableCollection(int maxItems)
    {
        this.maxItems = maxItems;
    }

    protected override void InsertItem(int index, T item)
    {
        while (Count >= maxItems)
        {
            RemoveAt(0);
            index = Math.Max(0, index - 1);
        }

        index = Math.Min(index, Count);
        base.InsertItem(index, item);
    }
}
