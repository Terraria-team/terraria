using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectRegistry<TValue> where TValue : ScriptableObject
{
    private readonly Dictionary<int, TValue> _registryMap = new ();

    public void Initialize(IEnumerable<TValue> objects, System.Func<TValue, int> keySelector)
    {
        _registryMap.Clear();
        foreach (var obj in objects)
        {
            if (obj == null) continue;
            
            int key = keySelector(obj);
            if (!_registryMap.TryAdd(key, obj))
            {
                Debug.LogError($"Duplicate ID found: {key} for type {typeof(TValue).Name}");
            }
        }
    }

    public TValue Get(int id)
    {
        if (_registryMap.TryGetValue(id, out var obj))
        {
            return obj;
        }
        Debug.LogError($"ID {id} not found in {typeof(TValue).Name} registry.");
        return null;
    }
}