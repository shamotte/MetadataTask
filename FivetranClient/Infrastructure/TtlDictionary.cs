using System.Threading.Tasks;

namespace FivetranClient.Infrastructure;

public class TtlDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, (TValue, DateTime)> _dictionary = new();

    private SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);

    public async Task<TValue> GetOrAdd(TKey key, Func<TValue> valueFactory, TimeSpan ttl)
    {
        await _semaphore.WaitAsync();

        if (_dictionary.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Item2)
            {
                return entry.Item1;
            }

            _dictionary.Remove(key);
        }

        var value = valueFactory();
        _dictionary[key] = (value, DateTime.UtcNow.Add(ttl));
        _semaphore.Release();
        return value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_dictionary.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.Item2)
        {
            value = entry.Item1;
            return true;
        }

        value = default!;
        return false;
    }
}