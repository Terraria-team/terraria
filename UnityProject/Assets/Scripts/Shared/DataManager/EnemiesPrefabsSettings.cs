using System.Collections.Generic;
using Server.AI;
using UnityEngine;

public class EnemiesPrefabsSettings : MonoBehaviour
{
    [Header("Common enemies")]
    [SerializeField] private GameObject[] enemies;
    private readonly Dictionary<string, GameObject> indexedEnemies = new();

    [Header("Bosses")] 
    public GameObject eyeBoss;
    public GameObject beeBoss;
    public GameObject slimeBoss;
    
    public static EnemiesPrefabsSettings Instance;

    void Awake()
    {
        if (Instance != null)
            Debug.LogError("More than one EnemiesAssetsSettings instance found!");
        
        foreach (var enemy in enemies)
        {
            var id = enemy.GetComponent<ServerEnemyController>().Data.enemyName;

            indexedEnemies[id] = enemy;
        }
        
        Instance = this;
    }

    public GameObject GetEnemy(string enemyName)
    {
        return indexedEnemies[enemyName];
    }
}
