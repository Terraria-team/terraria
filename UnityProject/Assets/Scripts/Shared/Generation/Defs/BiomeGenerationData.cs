using System.Collections.Generic;
using UnityEngine;
using Core.WorldGeneration;
using Shared.DataDefinitions;

[CreateAssetMenu(fileName = "NewBiomeGenerationConfig", menuName = "Terrain/BiomeGenerationConfig")]
public class BiomeGenerationData : ScriptableObject
{
    [SerializeField] private BiomeType biomeType = BiomeType.Plains;
    public BiomeType BiomeType => biomeType;

    public Color associatedColor;
    
    [Header("Enemies")]
    public EnemyData[] allowedEnemies;
    
    [Header("Blocks")]
    public BlockData[] blocks;
    public BlockData[] ores;
    public float oresFrequency;
    public float oresSpread;

    [Header("Surface")]
    public bool usesSurface = false;

    public float baseSurfaceWeight = 1.0f;
    public float noiseSurfaceWeight = 1.0f;
    
    public bool usesGrass = false;
    public int grassDepth = 1;

    [Header("Caves")] 
    public bool usesCaves = true;
    public int cavesThreshold = 15;
    
    [Header("Basic")]
    [SerializeField] private int baseSurfaceLevel = 220;
    public int BaseSurfaceLevel => baseSurfaceLevel;

    [SerializeField] private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.Perlin;
    public FastNoiseLite.NoiseType NoiseType => noiseType;

    [SerializeField] private FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;
    public FastNoiseLite.FractalType FractalType => fractalType;

    [SerializeField] private float fractalLacunarity = 2f;
    public float FractalLacunarity => fractalLacunarity;

    [SerializeField] private float fractalGain = 0.5f;
    public float FractalGain => fractalGain;
    
    [Header("Landscape Y")]
    [SerializeField] private int amplitudeY = 15;
    public int AmplitudeY => amplitudeY;

    [SerializeField] private float frequencyY = 0.01f; 
    public float FrequencyY => frequencyY;

    [SerializeField] private int octavesY = 3; // Pulled from your hardcoded old logic
    public int OctavesY => octavesY;
    
    [Header("Landscape X")]
    [SerializeField] private int amplitudeX = 3;
    public int AmplitudeX => amplitudeX;

    [SerializeField] private float frequencyX = 0.01f;
    public float FrequencyX => frequencyX;

    [SerializeField] private int octavesX = 7;
    public int OctavesX => octavesX;

    [Header("Caves")]
    [SerializeField] private float caveFrequency = 0.07f;
    public float CaveFrequency => caveFrequency;

    [SerializeField] private float tunnelThickness = 0.2f;
    public float TunnelThickness => tunnelThickness;

    [SerializeField] private int caveSeedOffset1 = 100;
    public int CaveSeedOffset1 => caveSeedOffset1;

    [SerializeField] private int caveSeedOffset2 = 200;
    public int CaveSeedOffset2 => caveSeedOffset2;
    

    // [SerializeField] private float fudgeFactor = 0f;
    // public float FudgeFactor => fudgeFactor;
    //
    // [SerializeField] private float exponent = 1f;
    // public float Exponent => exponent;
}