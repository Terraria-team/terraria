using UnityEngine;
using Core.WorldGeneration;
using Server.SODefinitions;
namespace Server
{
    public class WorldGenerationSettings : MonoBehaviour, IWorldGenerationConfig
    {
        [SerializeField] private int width = 500;
        [SerializeField] private int height = 300;
        [SerializeField] private int seed = 1337;

        public int Width => width;
        public int Height => height;
        public int Seed => seed;

        public BiomeGenerationConfig[] Biomes; 
    }
    
}

