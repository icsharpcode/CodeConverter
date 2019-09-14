using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ICSharpCode.CodeConverter.Shared
{
    internal class ConcurrentHashSet<T> : IEnumerable<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dictionary = new ConcurrentDictionary<T, byte>();

        public bool Add(T val)
        {
            return _dictionary.TryAdd(val, 0);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }
    }
}