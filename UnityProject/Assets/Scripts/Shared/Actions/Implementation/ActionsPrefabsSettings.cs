using UnityEngine;

public class ActionsPrefabsSettings : MonoBehaviour
{
    public GameObject projectilePrefab;
    public GameObject swingPrefab;
    public GameObject droppedItemPrefab;
    
    public static ActionsPrefabsSettings Instance;
    
    void Awake()
    {
        if (Instance != null)
            Debug.LogError("More than one ActionsPrefabsSettings instance found!");
        
        Instance = this;
    }
}
