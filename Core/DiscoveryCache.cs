using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HardModePatch.Core
{
    // An empty completed search is distinct from a cache that still needs seeding. Reference identity
    // avoids invoking engine equality on destroyed objects. The engine supplies the liveness predicate.
    public sealed class DiscoveryCache<T> where T : class
    {
        sealed class Identity : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) { return ReferenceEquals(x, y); }
            public int GetHashCode(T value) { return RuntimeHelpers.GetHashCode(value); }
        }

        readonly HashSet<T> known = new HashSet<T>(new Identity());
        readonly List<T> items = new List<T>();
        T[] snapshot;
        public bool Ready { get; private set; }
        public int Count { get { return items.Count; } }

        public void Reset()
        {
            Ready = false;
            known.Clear();
            items.Clear();
            snapshot = null;
        }

        public void Seed(IEnumerable<T> values)
        {
            Reset();
            Ready = true;
            foreach (T value in values) Track(value);
        }

        public bool Track(T value)
        {
            if (!Ready || ReferenceEquals(value, null) || !known.Add(value)) return false;
            items.Add(value);
            snapshot = null;
            return true;
        }

        public void Remove(T value)
        {
            if (ReferenceEquals(value, null) || !known.Remove(value)) return;
            for (int i = items.Count - 1; i >= 0; i--)
                if (ReferenceEquals(items[i], value)) { items.RemoveAt(i); break; }
            snapshot = null;
        }

        public T[] Snapshot(Predicate<T> alive)
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (!alive(items[i]))
                {
                    known.Remove(items[i]);
                    items.RemoveAt(i);
                    snapshot = null;
                }
            return snapshot ?? (snapshot = items.ToArray());
        }
    }
}
