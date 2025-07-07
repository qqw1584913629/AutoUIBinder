using UnityEngine;
using System;
using System.Collections.Generic;

namespace AutoUIBinder
{
    [Serializable]
    public class SerializableKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public SerializableKeyValuePair()
        {
        }

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<SerializableKeyValuePair<TKey, TValue>> pairs = new List<SerializableKeyValuePair<TKey, TValue>>();

        public void OnBeforeSerialize()
        {
            pairs.Clear();
            foreach (KeyValuePair<TKey, TValue> kvp in this)
            {
                pairs.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();

            foreach (var pair in pairs)
            {
                if (pair.Key != null && !ContainsKey(pair.Key))
                {
                    this.Add(pair.Key, pair.Value);
                }
            }
        }
    }
} 