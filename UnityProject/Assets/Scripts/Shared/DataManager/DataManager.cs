using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class DataManager
{
    public static ScriptableObjectRegistry<ItemData> Items { get; } = new();
    public static ScriptableObjectRegistry<BlockData> Blocks { get; } = new();
    public static bool IsInitialized { get; private set; } = false;

    // Automatically runs when the game starts up, before the first scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        LoadGroup(Items, "Item", x => x.id);
        LoadGroup(Blocks, "Block", x => x.id);
        
        IsInitialized = true;
    }

    private static void LoadGroup<T>(ScriptableObjectRegistry<T> output, 
        string key, Func<T, int> idSelector) where T : ScriptableObject
    {
        AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key);
        
        IList<T> loadedData = handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            output.Initialize(loadedData, idSelector);
        }
        else
        {
            Debug.LogError($"Failed to synchronously load Addressable data for key {key}");
        }
    }
}