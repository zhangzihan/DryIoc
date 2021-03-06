﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ImTools;

namespace DryIoc.Playground
{
    [TestFixture]
    public class HashTreeOfArrayTests
    {
        [Test]
        public void Can_combine_arrays_into_one_AND_get_it_by_key()
        {
            var tree = HashTreeX<int, string[]>.Using(ConcatArrays);
            var list = new List<string[]>(100);
            for (var i = 0; i < 100; i++)
            {
                tree = tree.AddOrUpdate(i, new[] { i.ToString() });
                list.Add(new[] { i.ToString() });
            }

            Assert.That(tree.Select(kv => kv.Value), Is.EqualTo(list));
        }

        [Test]
        public void Can_concat_arrays()
        {
            var tree = HashTreeX<int, string[]>.Using(ConcatArrays)
                .AddOrUpdate(0, new[] {"a"})
                .AddOrUpdate(0, new[] {"b"})
                .AddOrUpdate(0, new[] {"c"});

            Assert.That(tree.TryGet(0), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        private static V[] ConcatArrays<V>(V[] old, V[] added)
        {
            var result = new V[old.Length + added.Length];
            Array.Copy(old, 0, result, 0, old.Length);
            if (added.Length == 1) // expected case.
                result[old.Length] = added[0];
            else
                Array.Copy(added, 0, result, old.Length, added.Length);
            return result;
        }
    }

    public sealed class HashTreeX<K, V> : IEnumerable<KV<K, V>>
    {
        public static readonly HashTreeX<K, V> Empty = new HashTreeX<K, V>(IntTree<KV<K, V>>.Empty, null);

        public static HashTreeX<K, V> Using(Func<V, V, V> updateValue = null)
        {
            return updateValue == null ? Empty : new HashTreeX<K, V>(IntTree<KV<K, V>>.Empty, updateValue);
        }

        public HashTreeX<K, V> AddOrUpdate(K key, V value)
        {
            return new HashTreeX<K, V>(_tree.AddOrUpdate(key.GetHashCode(), new KV<K, V>(key, value), UpdateConflicts), _updateValue);
        }

        public V TryGet(K key)
        {
            var item = _tree.GetValueOrDefault(key.GetHashCode());
            return item != null && (ReferenceEquals(key, item.Key) || key.Equals(item.Key)) ? item.Value : TryGetConflicted(item, key);
        }

        public IEnumerator<KV<K, V>> GetEnumerator()
        {
            foreach (var node in _tree.Enumerate())
            {
                yield return node.Value;
                if (node.Value is KVWithConflicts)
                {
                    var conflicts = ((KVWithConflicts)node.Value).Conflicts;
                    for (var i = 0; i < conflicts.Length; i++)
                        yield return conflicts[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region Implementation

        private HashTreeX(IntTree<KV<K, V>> tree, Func<V, V, V> updateValue)
        {
            _tree = tree;
            _updateValue = updateValue;
        }

        private readonly IntTree<KV<K, V>> _tree;
        private readonly Func<V, V, V> _updateValue;

        private KV<K, V> UpdateConflicts(KV<K, V> existing, KV<K, V> added)
        {
            var conflicts = existing is KVWithConflicts ? ((KVWithConflicts)existing).Conflicts : null;
            if (ReferenceEquals(existing.Key, added.Key) || existing.Key.Equals(added.Key))
                return conflicts == null ? UpdateValue(existing, added)
                     : new KVWithConflicts(UpdateValue(existing, added), conflicts);
            
            if (conflicts == null)
                return new KVWithConflicts(existing, new[] { added });

            var i = conflicts.Length - 1;
            while (i >= 0 && !Equals(conflicts[i].Key, added.Key)) --i;
            if (i != -1) added = UpdateValue(existing, added);
            return new KVWithConflicts(existing, conflicts.AppendOrUpdate(added, i));
        }

        private KV<K, V> UpdateValue(KV<K, V> existing, KV<K, V> added)
        {
            return _updateValue == null ? added : new KV<K, V>(existing.Key, _updateValue(existing.Value, added.Value));
        }

        private static V TryGetConflicted(KV<K, V> item, K key)
        {
            var conflicts = item is KVWithConflicts ? ((KVWithConflicts)item).Conflicts : null;
            if (conflicts != null)
                for (var i = 0; i < conflicts.Length; i++)
                    if (Equals(conflicts[i].Key, key))
                        return conflicts[i].Value;
            return default(V);
        }

        private sealed class KVWithConflicts : KV<K, V>
        {
            public readonly KV<K, V>[] Conflicts;

            public KVWithConflicts(KV<K, V> kv, KV<K, V>[] conflicts)
                : base(kv.Key, kv.Value)
            {
                Conflicts = conflicts;
            }
        }

        #endregion
    }

    public class KV<K, V>
    {
        public readonly K Key;
        public readonly V Value;

        public KV(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }
}
