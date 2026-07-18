namespace Core.WorldGeneration
{
    public interface IBiomeGenerationConfig
    {
        BiomeType BiomeType { get; }
        
        int BaseSurfaceLevel { get; }
        FastNoiseLite.NoiseType NoiseType { get; }
        FastNoiseLite.FractalType FractalType { get; }
        float FractalLacunarity { get; }
        float FractalGain { get; }

        // Landscape Y
        int AmplitudeY { get; }
        float FrequencyY { get; }
        int OctavesY { get; }

        // Landscape X
        int AmplitudeX { get; }
        float FrequencyX { get; }
        int OctavesX { get; }

        // Caves
        float CaveFrequency { get; }
        float TunnelThickness { get; }
        int CaveSeedOffset1 { get; }
        int CaveSeedOffset2 { get; }
        
        
        //float FudgeFactor { get;}
        //float Exponent { get;}
    }
}
