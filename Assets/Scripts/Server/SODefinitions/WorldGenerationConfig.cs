using Core.WorldGeneration;
using UnityEngine;

namespace Server.SODefinitions
{
    [CreateAssetMenu(fileName = "NewWorldGenerationConfig", menuName = "Terrain/WorldGenerationConfig")]
    public class WorldGenerationConfig : ScriptableObject, IWorldGenerationConfig
    {
        [SerializeField] private int width = 512;
        public int Width  => width;
        
        [SerializeField] private int height = 512;
        public int Height => height;

        [SerializeField] private int seed = 1337;
        public int Seed => seed;
    }
}