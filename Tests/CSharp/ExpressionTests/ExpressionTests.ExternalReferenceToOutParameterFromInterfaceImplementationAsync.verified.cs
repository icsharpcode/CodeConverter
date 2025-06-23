using System.Collections;
using System.Collections.Generic;

internal abstract partial class TestClass : IReadOnlyDictionary<int, int>
{
    public bool TryGetValue(int key, out int value)
    {
        value = key;
        return default;
    }

    private void TestMethod()
    {
        var value = default(int);
        TryGetValue(5, out value);
    }

    public abstract bool ContainsKey(int key);
    public abstract IEnumerator<KeyValuePair<int, int>> GetEnumerator();
    public abstract IEnumerator IEnumerable_GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => IEnumerable_GetEnumerator();
    public abstract int this[int key] { get; }
    public abstract IEnumerable<int> Keys { get; }
    public abstract IEnumerable<int> Values { get; }
    public abstract int Count { get; }
}