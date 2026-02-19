public class ConcurrentArrayMap<TKey, TValue> where TKey : notnull
{
    readonly List<KeyValuePair<TKey, TValue>> list;

    public ConcurrentArrayMap(int capacity = 16)
    {
        list = new List<KeyValuePair<TKey, TValue>>(capacity);
    }

    public void Add(TKey key, TValue value)
    {
        lock (list)
        {
            list.Add(new KeyValuePair<TKey, TValue>(key, value));
        }
    }

    public TValue? this [TKey key]
    {
        get
        {
            lock (list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item.Key.Equals(key))
                    {
                        return item.Value;
                    }
                }
                return default(TValue);
            }
        }
        set
        {
            lock (list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    if (item.Key.Equals(key))
                    {
                        item = new KeyValuePair<TKey, TValue>(key, value);
                    }
                }
                list.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.Key.Equals(key))
                {
                    value = item.Value;
                    return true;
                }
            }
            value = default;
            return false;
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        lock (list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item.Key.Equals(key))
                {
                    value = item.Value;
                    list.RemoveAt(i);
                    return true;
                }
            }
            value = default;
            return false;
        }
    }

    public List<KeyValuePair<TKey, TValue>> ToList()
    {
        lock (list)
        {
            return list.ToList();
        }
    }

    public KeyValuePair<TKey, TValue>[] ToArray()
    {
        lock (list)
        {
            return list.ToArray();
        }
    }
}
