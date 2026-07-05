using UnityEngine;

public class ActionsSettings : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    public GameObject SwingPrefab;
    
    
    public static ActionsSettings Instance;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
