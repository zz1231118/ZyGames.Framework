using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ZyGames.Framework.Services.Collections
{
    internal sealed class CachedReadConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private const int CacheMissesBeforeCaching = 10;
        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
        private readonly IEqualityComparer<TKey> comparer;
        private int cacheMissReads;
        private Dictionary<TKey, TValue> readCache;

        public CachedReadConcurrentDictionary()
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        public CachedReadConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(collection);
        }

        public CachedReadConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        public CachedReadConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            this.dictionary = new ConcurrentDictionary<TKey, TValue>(collection, comparer);
        }

        public TValue this[TKey key]
        {
            get { return GetReadDictionary()[key]; }
            set
            {
                dictionary[key] = value;
                InvalidateCache();
            }
        }

        public ICollection<TKey> Keys => GetReadDictionary().Keys;

        public ICollection<TValue> Values => GetReadDictionary().Values;

        public int Count => GetReadDictionary().Count;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IDictionary<TKey, TValue> GetReadDictionary()
        {
            return readCache ?? GetWithoutCache();
        }

        private IDictionary<TKey, TValue> GetWithoutCache()
        {
            if (Interlocked.Increment(ref cacheMissReads) < CacheMissesBeforeCaching)
            {
                return dictionary;
            }

            cacheMissReads = 0;
            return readCache = new Dictionary<TKey, TValue>(dictionary, comparer);
        }

        private void InvalidateCache()
        {
            cacheMissReads = 0;
            readCache = null;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (GetReadDictionary().TryGetValue(key, out TValue value))
            {
                return value;
            }

            value = dictionary.GetOrAdd(key, valueFactory);
            InvalidateCache();
            return value;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (dictionary.TryAdd(key, value))
            {
                InvalidateCache();
                return true;
            }

            return false;
        }

        public bool ContainsKey(TKey key)
        {
            return GetReadDictionary().ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            var result = ((IDictionary<TKey, TValue>)dictionary).Remove(key);
            if (result) InvalidateCache();
            return result;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return GetReadDictionary().TryGetValue(key, out value);
        }

        public void Clear()
        {
            dictionary.Clear();
            InvalidateCache();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return GetReadDictionary().GetEnumerator();
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)dictionary).Add(key, value);
            InvalidateCache();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)dictionary).Add(item);
            InvalidateCache();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return GetReadDictionary().Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            GetReadDictionary().CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = ((IDictionary<TKey, TValue>)dictionary).Remove(item);
            if (result) InvalidateCache();
            return result;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetReadDictionary().GetEnumerator();
        }
    }
}
