using UnityEngine;

public class ActionsSettings : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    public GameObject SwingPrefab;
    public GameObject DroppedItemPrefab;
    
    public static ActionsSettings Instance;
    
    void Awake()
    {
        Instance = this;
    }
}
