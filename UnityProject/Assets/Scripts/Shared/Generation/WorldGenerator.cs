using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.WorldGeneration
{
    public class MapGenerator
    {
        private readonly Dictionary<BiomeType, FastNoiseLite> _landscapeYNoises = new ();
        private readonly Dictionary<BiomeType, FastNoiseLite> _landscapeXNoises = new ();
        private readonly Dictionary<BiomeType, FastNoiseLite> _caveNoises1 = new ();
        private readonly Dictionary<BiomeType, FastNoiseLite> _caveNoises2 = new ();
        private readonly Dictionary<BiomeType, FastNoiseLite> _oresNoise = new ();

        private FastNoiseLite _warpNoiseX;
        private FastNoiseLite _warpNoiseY;
        
        private static readonly WorldGenerationConfig WorldGenerationConfig = DataManager.WorldConfigs[0];
        private BlockID[,] _generatedWorld;

        public MapGenerator()
        {
            _generatedWorld = new BlockID[WorldGenerationConfig.Width * ChunkUtils.ChunkSize, WorldGenerationConfig.Height * ChunkUtils.ChunkSize];
            
            InitializeBiomeNoises();
        }

        private void InitializeBiomeNoises()
        {
            foreach (var biomeData in DataManager.Biomes)
            {
                BiomeType type = biomeData.BiomeType;
                
                // 1. Landscape Y Noise
                FastNoiseLite noiseY = new FastNoiseLite(WorldGenerationConfig.Seed);
                noiseY.SetNoiseType(biomeData.NoiseType);
                noiseY.SetFractalType(biomeData.FractalType);
                noiseY.SetFractalOctaves(biomeData.OctavesY); 
                noiseY.SetFrequency(biomeData.FrequencyY);
                noiseY.SetFractalLacunarity(biomeData.FractalLacunarity);
                noiseY.SetFractalGain(biomeData.FractalGain);
                _landscapeYNoises.Add(type, noiseY);

                // 2. Landscape X Noise
                FastNoiseLite noiseX = new FastNoiseLite(WorldGenerationConfig.Seed);
                noiseX.SetNoiseType(biomeData.NoiseType);
                noiseX.SetFractalType(biomeData.FractalType);
                noiseX.SetFractalOctaves(biomeData.OctavesX); 
                noiseX.SetFrequency(biomeData.FrequencyX);
                noiseX.SetFractalLacunarity(biomeData.FractalLacunarity);
                noiseX.SetFractalGain(biomeData.FractalGain);
                _landscapeXNoises.Add(type, noiseX);
                
                // 3. Cave Noise 1
                FastNoiseLite caveNoise1 = new FastNoiseLite(WorldGenerationConfig.Seed + biomeData.CaveSeedOffset1);
                caveNoise1.SetNoiseType(biomeData.NoiseType);
                caveNoise1.SetFrequency(biomeData.CaveFrequency); 
                _caveNoises1.Add(type, caveNoise1);
                
                // 4. Cave Noise 2
                FastNoiseLite caveNoise2 = new FastNoiseLite(WorldGenerationConfig.Seed + biomeData.CaveSeedOffset2);
                caveNoise2.SetNoiseType(biomeData.NoiseType);
                caveNoise2.SetFrequency(biomeData.CaveFrequency); 
                _caveNoises2.Add(type, caveNoise2);

                FastNoiseLite oresNoise = new FastNoiseLite(WorldGenerationConfig.Seed);
                oresNoise.SetNoiseType(biomeData.NoiseType);
                oresNoise.SetFrequency(biomeData.oresFrequency);
                _oresNoise.Add(type, oresNoise);
            }
            
            _warpNoiseX = new FastNoiseLite(WorldGenerationConfig.Seed);
            _warpNoiseX.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _warpNoiseX.SetFrequency(0.35f);
            _warpNoiseY = new FastNoiseLite(WorldGenerationConfig.Seed + 100);
            _warpNoiseY.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _warpNoiseY.SetFrequency(0.35f);
        }

        public static BiomeType GetBiomeTypeAt(Vector2Int coords)
        {
            const float caveThreshold = 0.6f;
            const float jungleThreshold = 0.7f;
            const float plainsThreshold = 0.3f;
            const float desertThreshold = 0.1f;
            
            float xPercentage = (float)coords.x / WorldGenerationConfig.Width;
            float yPercentage = (float)coords.y / WorldGenerationConfig.Height;

            if (coords.y == 0)
                return BiomeType.Lava;

            if (yPercentage < caveThreshold)
            {
                // Cave case

                if (xPercentage < jungleThreshold)
                    return BiomeType.Caves;
                else
                    return BiomeType.JungleCaves;
            }
            else
            {
                if (xPercentage > jungleThreshold)
                    return BiomeType.Jungle;
                
                if (xPercentage < desertThreshold)
                    return BiomeType.Ocean;
                
                if (xPercentage is > desertThreshold and < plainsThreshold)
                    return BiomeType.Desert;
                    
                return BiomeType.Plains;
            }
        }

        private void GenerateLandscape()
        {
            GenerateLandscapeY();
            GenerateLandscapeX();
        }

        private void GenerateLandscapeY()
        {
            for (int chunkX = 0; chunkX < WorldGenerationConfig.Width; chunkX++)
            {
                for (int chunkY = 0; chunkY < WorldGenerationConfig.Height; chunkY++)
                {
                    BiomeType biomeType = GetBiomeTypeAt(new Vector2Int(chunkX, chunkY));
                    BiomeGenerationData currentBiomeData = DataManager.Biomes[biomeType];
                    FastNoiseLite noiseY = _landscapeYNoises[biomeType];
                    
                    int startX = chunkX * ChunkUtils.ChunkSize;
                    int startY = chunkY * ChunkUtils.ChunkSize;
                    
                    for (byte localX = 0; localX < ChunkUtils.ChunkSize; localX++)
                    {
                        int x = startX + localX;
                        
                        for (byte localY = 0; localY < ChunkUtils.ChunkSize; localY++)
                        {
                            int y = startY + localY;

                            if (currentBiomeData.blocks.Length == 0)
                            {
                                Debug.LogError($"Blocks are not set for {biomeType}");
                                _generatedWorld[x, y] = new BlockID(0);
                                continue;
                            }

                            if (!currentBiomeData.usesSurface)
                            {
                                _generatedWorld[x, y] = new BlockID(currentBiomeData.blocks[0].id);
                                continue;
                            }
                            
                            float normalisedNoise = noiseY.GetNoise(x, 0);
                            int surfaceLevel = (int)(currentBiomeData.BaseSurfaceLevel * currentBiomeData.baseSurfaceWeight + normalisedNoise * currentBiomeData.AmplitudeY * currentBiomeData.noiseSurfaceWeight);

                            if (!currentBiomeData.usesGrass)
                            {
                                var usedBlock = y < surfaceLevel ? currentBiomeData.blocks[0].id : (ushort)0;

                                _generatedWorld[x, y] = new BlockID(usedBlock);
                            }
                            else
                            {
                                BlockID type = y switch
                                {
                                    _ when y < surfaceLevel - currentBiomeData.grassDepth => new BlockID(currentBiomeData.blocks[1].id),
                                    _ when y >= surfaceLevel - currentBiomeData.grassDepth && y < surfaceLevel => new BlockID(currentBiomeData.blocks[0].id),
                                    _ => new BlockID(0)
                                };

                                _generatedWorld[x, y] = type;
                            }
                        }
                    }
                }
            }
        }
        
        private void GenerateLandscapeX()
        {
            BlockID[,] newMap = new BlockID[WorldGenerationConfig.Width * ChunkUtils.ChunkSize, WorldGenerationConfig.Height * ChunkUtils.ChunkSize];
            
            for (int y = 0; y < WorldGenerationConfig.Height * ChunkUtils.ChunkSize; ++y)
            {
                for (int x = 0; x < WorldGenerationConfig.Width * ChunkUtils.ChunkSize; ++x)
                {
                    BiomeType biomeType = GetBiomeTypeAt(new Vector2Int(x, y));
                    BiomeGenerationData currentBiomeData = DataManager.Biomes[biomeType];
                    FastNoiseLite noiseX = _landscapeXNoises[biomeType];
                    
                    float normalisedNoise = noiseX.GetNoise(x, y);
                    int sampleBlockXCoord = (int)(x + normalisedNoise * currentBiomeData.AmplitudeX);
                    
                    newMap[x, y] = _generatedWorld[Math.Clamp(sampleBlockXCoord, 0, WorldGenerationConfig.Width * ChunkUtils.ChunkSize - 1), y];
                }
            }
            _generatedWorld = newMap;
        }
        
        private void GenerateCaves()
        {
            for (int chunkX = 0; chunkX < WorldGenerationConfig.Width; chunkX++)
            {
                for (int chunkY = 0; chunkY < WorldGenerationConfig.Height; chunkY++)
                {
                    BiomeType biomeType = GetBiomeTypeAt(new Vector2Int(chunkX, chunkY));
                    BiomeGenerationData currentBiomeData = DataManager.Biomes[biomeType];
                    FastNoiseLite caveNoise1 = _caveNoises1[biomeType];
                    FastNoiseLite caveNoise2 = _caveNoises2[biomeType];

                    if (!currentBiomeData.usesCaves)
                        continue;
                        
                    float currentThickness = currentBiomeData.TunnelThickness;

                    int startX = chunkX * ChunkUtils.ChunkSize;
                    int startY = chunkY * ChunkUtils.ChunkSize;

                    for (byte localX = 0; localX < ChunkUtils.ChunkSize; localX++)
                    {
                        int x = startX + localX;

                        for (byte localY = 0; localY < ChunkUtils.ChunkSize; localY++)
                        {
                            int y = startY + localY;

                            if (currentBiomeData.usesSurface)
                            {
                                if (currentBiomeData.BaseSurfaceLevel - currentBiomeData.cavesThreshold < y)
                                    continue;
                            }
                            
                            if (_generatedWorld[x, y] != BlockID.Air)
                            {
                                Vector2 warp = new Vector2(
                                    _warpNoiseX.GetNoise(x, y),
                                    _warpNoiseY.GetNoise(x, y)
                                ) * 1f;

                                float nx = x + warp.x;
                                float ny = y + warp.y;

                                float n1 = caveNoise1.GetNoise(nx, ny);
                                float n2 = caveNoise2.GetNoise(nx, ny);

                                if (Math.Abs(n1) < currentThickness &&
                                    Math.Abs(n2) < currentThickness)
                                {
                                    _generatedWorld[x, y] = BlockID.Air;
                                }
                            }
                        }
                    }
                }
            }
        }
        private void GenerateOres()
        {
            for (int chunkX = 0; chunkX < WorldGenerationConfig.Width; chunkX++)
            {
                for (int chunkY = 0; chunkY < WorldGenerationConfig.Height; chunkY++)
                {
                    BiomeType biomeType = GetBiomeTypeAt(new Vector2Int(chunkX, chunkY));
                    BiomeGenerationData currentBiomeData = DataManager.Biomes[biomeType];
                    if (currentBiomeData.ores.Length == 0)
                        continue;

                    FastNoiseLite oresNoise = _oresNoise[biomeType];
                    
                    int startX = chunkX * ChunkUtils.ChunkSize;
                    int startY = chunkY * ChunkUtils.ChunkSize;

                    for (byte localX = 0; localX < ChunkUtils.ChunkSize; localX++)
                    {
                        int x = startX + localX;

                        for (byte localY = 0; localY < ChunkUtils.ChunkSize; localY++)
                        {
                            int y = startY + localY;

                            if (_generatedWorld[x, y] != BlockID.Air)
                            {
                                float oreValue = oresNoise.GetNoise(x, y);
                                
                                if (Math.Abs(oreValue) < currentBiomeData.oresSpread)
                                {
                                    _generatedWorld[x, y] = new BlockID(currentBiomeData.ores[0].id);
                                }
                            }
                        }
                    }
                }
            }
        }
        private BlockID[,] GenerateBlockArray()
        {
            GenerateLandscape();
            GenerateCaves();
            GenerateOres();
            return _generatedWorld;
        }
        
        private Dictionary<Vector2Int, ChunkData> SliceMapIntoChunks(BlockID[,] rawMap)
        {
            var result = new Dictionary<Vector2Int, ChunkData>(
                WorldGenerationConfig.Width * WorldGenerationConfig.Height);

            for (int chunkX = 0; chunkX < WorldGenerationConfig.Width; chunkX++)
            {
                for (int chunkY = 0; chunkY < WorldGenerationConfig.Height; chunkY++)
                {
                    ChunkData newChunk = new ChunkData(new BlockID(0));
                
                    int startX = chunkX * ChunkUtils.ChunkSize;
                    int startY = chunkY * ChunkUtils.ChunkSize;
                    
                    for (byte localX = 0; localX < ChunkUtils.ChunkSize; localX++)
                    {
                        int globalX = startX + localX;
                        
                        for (byte localY = 0; localY < ChunkUtils.ChunkSize; localY++)
                        {
                            int globalY = startY + localY;
                        
                            BlockID blockValue = rawMap[globalX, globalY];
                            newChunk.Set(localX, localY, blockValue);
                        }
                    }
                    
                    result.Add(new Vector2Int(chunkX, chunkY), newChunk);
                }
            }
            return result;
        }

        public Dictionary<Vector2Int, ChunkData> GenerateMapChunks()
        {
            return SliceMapIntoChunks(
                GenerateBlockArray() 
            );
        }
    }
}