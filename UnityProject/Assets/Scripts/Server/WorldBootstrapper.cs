using System.Collections.Generic;
using Core.WorldGeneration;
using Mirror;
using Server.SODefinitions;
using UnityEngine;

namespace Server
{
    public class WorldBootstrapper : MonoBehaviour
    {
        public WorldGenerationConfig worldConfig; 
        public BiomeGenerationConfig forestConfig;  

        void Start()
        {
            if (Application.isBatchMode || NetworkServer.active)
            {
                GenerateAndInjectWorldToChunkManager();
            }
        }

        private void GenerateAndInjectWorldToChunkManager()
        {
            var biomeConfigs = new Dictionary<BiomeType, IBiomeGenerationConfig>
            {
                { BiomeType.Forest, forestConfig }
            };
            
            MapGenerator generator = new MapGenerator(worldConfig, biomeConfigs);
            BlockType[,] rawMap = generator.Generate();
            
            Dictionary<Vector2Int, ChunkData> chunkDictionary = SliceMapIntoChunks(rawMap);
            
            ChunkManager.Instance.InjectWorldData(chunkDictionary);
        }
        
        private Dictionary<Vector2Int, ChunkData> SliceMapIntoChunks(BlockType[,] rawMap)
        {
            var result = new Dictionary<Vector2Int, ChunkData>();

            int worldHeightInChunks = worldConfig.Height / ChunkUtils.ChunkSize;
            int worldWidthInChunks = worldConfig.Width / ChunkUtils.ChunkSize;

            for (int chunkX = 0; chunkX < worldWidthInChunks; chunkX++)
            {
                for (int chunkY = 0; chunkY < worldHeightInChunks; chunkY++)
                {
                    ChunkData newChunk = new ChunkData(new BlockID(0), false);
                    
                    for (byte localX = 0; localX < 64; localX++)
                    {
                        for (byte localY = 0; localY < 64; localY++)
                        {
   
                            int globalX = chunkX * 64 + localX;
                            int globalY = chunkY * 64 + localY;
                            
                            ushort blockValue = (ushort)rawMap[globalX, globalY];
                            newChunk.Set(localX, localY, new BlockID(blockValue));
                        }
                    }
                    result.Add(new Vector2Int(chunkX, chunkY), newChunk);
                }
            }
            return result;
        }
    }
}

