using System;
using System.Collections.Generic;
using Shared.DataDefinitions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class DataManager
{
    public static ScriptableObjectRegistry<ItemData, int> Items { get; } = new();
    public static ScriptableObjectRegistry<BlockData, int> Blocks { get; } = new();
    public static ScriptableObjectRegistry<BiomeGenerationData, BiomeType> Biomes { get; } = new();
    public static ScriptableObjectRegistry<WorldGenerationConfig, int> WorldConfigs { get; } = new();
    public static ScriptableObjectRegistry<EnemyData, string> Enemies { get; } = new();
    public static ScriptableObjectRegistry<CraftData, int> Crafts { get; } = new();
    public static bool IsInitialized { get; private set; } = false;

    // Automatically runs when the game starts up, before the first scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (IsInitialized)
            return;
        
        LoadGroup(Items, "Item", x => x.id);
        LoadGroup(Blocks, "Block", x => x.id);
        LoadGroup(Biomes, "Biome", x => x.BiomeType);
        LoadGroup(WorldConfigs, "WorldConfig", x => 0);
        LoadGroup(Enemies, "Enemy", x => x.enemyName);
        LoadGroup(Crafts, "Craft", GetCraftIndex);
        
        IsInitialized = true;
    }

    private static int _craftIndex;

    private static int GetCraftIndex(CraftData data)
    {
        _craftIndex++;

        data.generatedID = _craftIndex;
        return _craftIndex;
    }
    
    private static void LoadGroup<T, TKey>(ScriptableObjectRegistry<T, TKey> output, 
        string key, Func<T, TKey> idSelector) where T : ScriptableObject
    {
        AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(key);
        
        IList<T> loadedData = handle.WaitForCompletion();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var sortedData = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(loadedData, x => x.name));
            output.Initialize(sortedData, idSelector);
        }
        else
        {
            Debug.LogError($"Failed to synchronously load Addressable data for key {key}");
        }
    }
}