using UnityEngine;

[CreateAssetMenu(fileName = "NewWorldGenerationConfig", menuName = "Terrain/WorldGenerationConfig")]
public class WorldGenerationConfig : ScriptableObject
{
    [SerializeField] private int width = 8;
    public int Width  => width;
        
    [SerializeField] private int height = 8;
    public int Height => height;

    [SerializeField] private int seed = 1337;
    public int Seed => seed;
}
