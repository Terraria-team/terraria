using System;
using System.Collections.Generic;

namespace Core.WorldGeneration
{
    public class MapGenerator
    {
        
        private readonly Dictionary<BiomeType,IBiomeGenerationConfig> _biomeGenerationConfigs;
        private readonly Dictionary<BiomeType, FastNoiseLite> _landscapeYNoises;
        private readonly Dictionary<BiomeType, FastNoiseLite> _landscapeXNoises;
        private readonly Dictionary<BiomeType, FastNoiseLite> _caveNoises1;
        private readonly Dictionary<BiomeType, FastNoiseLite> _caveNoises2;
        
        private readonly IWorldGenerationConfig _worldGenerationConfig;
        private BlockType[,] _generatedWorld;
        

        public MapGenerator(IWorldGenerationConfig config, Dictionary<BiomeType, IBiomeGenerationConfig> biomeConfigs)
        {
            _worldGenerationConfig = config;
            _biomeGenerationConfigs = biomeConfigs;
            _generatedWorld = new BlockType[_worldGenerationConfig.Width, _worldGenerationConfig.Height];
            
            _landscapeYNoises = new Dictionary<BiomeType, FastNoiseLite>();
            _landscapeXNoises = new Dictionary<BiomeType, FastNoiseLite>();
            _caveNoises1 = new Dictionary<BiomeType, FastNoiseLite>();
            _caveNoises2 = new Dictionary<BiomeType, FastNoiseLite>();
            
            InitializeBiomeNoises();
        }

        private void InitializeBiomeNoises()
        {
            foreach (var kvp in _biomeGenerationConfigs)
            {
                BiomeType type = kvp.Key;
                IBiomeGenerationConfig biomeConfig = kvp.Value;

                // 1. Landscape Y Noise
                FastNoiseLite noiseY = new FastNoiseLite(_worldGenerationConfig.Seed);
                noiseY.SetNoiseType(biomeConfig.NoiseType);
                noiseY.SetFractalType(biomeConfig.FractalType);
                noiseY.SetFractalOctaves(biomeConfig.OctavesY); 
                noiseY.SetFrequency(biomeConfig.FrequencyY);
                noiseY.SetFractalLacunarity(biomeConfig.FractalLacunarity);
                noiseY.SetFractalGain(biomeConfig.FractalGain);
                _landscapeYNoises.Add(type, noiseY);

                // 2. Landscape X Noise
                FastNoiseLite noiseX = new FastNoiseLite(_worldGenerationConfig.Seed);
                noiseX.SetNoiseType(biomeConfig.NoiseType);
                noiseX.SetFractalType(biomeConfig.FractalType);
                noiseX.SetFractalOctaves(biomeConfig.OctavesX); 
                noiseX.SetFrequency(biomeConfig.FrequencyX);
                noiseX.SetFractalLacunarity(biomeConfig.FractalLacunarity);
                noiseX.SetFractalGain(biomeConfig.FractalGain);
                _landscapeXNoises.Add(type, noiseX);
                
                // 3. Cave Noise 1
                FastNoiseLite caveNoise1 = new FastNoiseLite(_worldGenerationConfig.Seed + biomeConfig.CaveSeedOffset1);
                caveNoise1.SetNoiseType(biomeConfig.NoiseType);
                caveNoise1.SetFrequency(biomeConfig.CaveFrequency); 
                _caveNoises1.Add(type, caveNoise1);
                
                // 4. Cave Noise 2
                FastNoiseLite caveNoise2 = new FastNoiseLite(_worldGenerationConfig.Seed + biomeConfig.CaveSeedOffset2);
                caveNoise2.SetNoiseType(biomeConfig.NoiseType);
                caveNoise2.SetFrequency(biomeConfig.CaveFrequency); 
                _caveNoises2.Add(type, caveNoise2); 
            }
        }

        //TODO: add logic about biome picking (I am thinking of creating 2 similar sample as in FastNoiseLite lib github readme for temperature and water and assigning a climate for each 2)
        
        private BiomeType GetLandscapeBiomeType(int x) => BiomeType.Forest;
        private BiomeType GetBiomeType(int x, int y) => BiomeType.Forest;
        
        private void GenerateLandscape()
        {
            GenerateLandscapeY();
            GenerateLandscapeX();
        }

        private void GenerateLandscapeY()
        {
            for (int x = 0; x < _worldGenerationConfig.Width; ++x)
            {
                BiomeType biomeType = GetLandscapeBiomeType(x);
                IBiomeGenerationConfig currentBiomeConfig = _biomeGenerationConfigs[biomeType];
                FastNoiseLite noiseY = _landscapeYNoises[biomeType];
                
                float normalisedNoise = noiseY.GetNoise(x, 0);
                int grassBlockYCoord = (int)(currentBiomeConfig.BaseSurfaceLevel + normalisedNoise * currentBiomeConfig.AmplitudeY);
                
                for (int y = 0; y < _worldGenerationConfig.Height; ++y)
                {
                    BlockType type = y switch
                    {
                        _ when y < grassBlockYCoord => BlockType.Dirt,
                        _ when y == grassBlockYCoord => BlockType.Grass,
                        _ => BlockType.Air 
                    };

                    _generatedWorld[x, y] = type;
                }
            }
        }
        
        private void GenerateLandscapeX()
        {
            BlockType[,] newMap = new BlockType[_worldGenerationConfig.Width, _worldGenerationConfig.Height];
            
            for (int y = 0; y < _worldGenerationConfig.Height; ++y)
            {
                for (int x = 0; x < _worldGenerationConfig.Width; ++x)
                {
                    BiomeType biomeType = GetBiomeType(x, y);
                    IBiomeGenerationConfig currentBiomeConfig = _biomeGenerationConfigs[biomeType];
                    FastNoiseLite noiseX = _landscapeXNoises[biomeType];
                    
                    float normalisedNoise = noiseX.GetNoise(x, y);
                    int sampleBlockXCoord = (int)(x + normalisedNoise * currentBiomeConfig.AmplitudeX);
                    
                    newMap[x, y] = _generatedWorld[Math.Clamp(sampleBlockXCoord, 0, _worldGenerationConfig.Width - 1), y];
                }
            }
            _generatedWorld = newMap;
        }
        
        private void GenerateCaves()
        {
            for (int x = 0; x < _worldGenerationConfig.Width; ++x)
            {
                float horizon = 0;
                for (int y = 0; y < _worldGenerationConfig.Height; ++y)
                {
                    if (_generatedWorld[x, y] is BlockType.Air)
                    {
                        horizon = y - 1;
                        break;
                    }
                }
                for (int y = 0; y < _worldGenerationConfig.Height; ++y)
                {
                    BiomeType biomeType = GetBiomeType(x, y);
                    IBiomeGenerationConfig currentBiomeConfig = _biomeGenerationConfigs[biomeType];
                    FastNoiseLite caveNoise1 = _caveNoises1[biomeType];
                    FastNoiseLite caveNoise2 = _caveNoises2[biomeType];
                    
                    // float depth = horizon - y;
                    // float depthMultiplier = depth / horizon;
                    // System.Random prng = new System.Random(_worldGenerationConfig.Seed);
                    // this is mess (depth > 0 ? 1 : (float)prng.NextDouble() * 2 * depthMultiplier)
                    float currentThickness = currentBiomeConfig.TunnelThickness;
                    
                    if (_generatedWorld[x, y] != BlockType.Air)
                    {
                        float noise1 = caveNoise1.GetNoise(x, y);
                        float noise2 = caveNoise2.GetNoise(x, y);
                        
                        if (Math.Abs(noise1) < currentThickness && Math.Abs(noise2) < currentThickness)
                        {
                            _generatedWorld[x, y] = BlockType.Air;
                        }
                    }
                }
            }
        }

        public BlockType[,] Generate()
        {
            GenerateLandscape();
            GenerateCaves();
            return _generatedWorld;
        }
    }
}