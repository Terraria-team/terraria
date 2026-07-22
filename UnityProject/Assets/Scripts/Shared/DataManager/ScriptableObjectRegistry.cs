using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectRegistry<TValue, TKey> : IEnumerable<TValue> where TValue : ScriptableObject
{
    private readonly Dictionary<TKey, TValue> _registryMap = new ();
    
    public void Initialize(IEnumerable<TValue> objects, System.Func<TValue, TKey> keySelector)
    {
        _registryMap.Clear();
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            
            TKey key = keySelector(obj);
            if (!_registryMap.TryAdd(key, obj))
            {
                Debug.LogError($"Duplicate ID found: {key} for type {typeof(TValue).Name}");
            }
        }
    }

    public TValue Get(TKey id)
    {
        if (_registryMap.TryGetValue(id, out var obj))
        {
            return obj;
        }
        Debug.LogError($"ID {id} not found in {typeof(TValue).Name} registry.");
        return null;
    }

    public IEnumerator<TValue> GetEnumerator()
    {
        return _registryMap.Values.GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public TValue this[TKey index] => Get(index);

    public void Clear()
    {
        _registryMap.Clear();
    }
}