using System.Collections.Generic;
using Core.WorldGeneration;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// Тестова реалізація IWorldGenerationConfig — замінює
    /// ScriptableObject-конфіг звичайним об'єктом, який можна створити в тесті.
    /// </summary>
    public sealed class FakeWorldConfig : IWorldGenerationConfig
    {
        public int Width { get; set; } = 64;
        public int Height { get; set; } = 64;
        public int Seed { get; set; } = 1337;
    }

    /// <summary>
    /// Тестова реалізація IBiomeGenerationConfig. За замовчуванням —
    /// «реалістичний» біом із ненульовими амплітудами, щоб seed реально впливав на світ.
    /// Значення можна перевизначати через ініціалізатори (напр. AmplitudeY = 0 для пласкої поверхні).
    /// </summary>
    public sealed class FakeBiomeConfig : IBiomeGenerationConfig
    {
        public BiomeType BiomeType { get; set; } = BiomeType.Forest;
        public int BaseSurfaceLevel { get; set; } = 32;
        public FastNoiseLite.NoiseType NoiseType { get; set; } = FastNoiseLite.NoiseType.Perlin;
        public FastNoiseLite.FractalType FractalType { get; set; } = FastNoiseLite.FractalType.FBm;
        public float FractalLacunarity { get; set; } = 2.0f;
        public float FractalGain { get; set; } = 0.5f;

        public int AmplitudeY { get; set; } = 12;
        public float FrequencyY { get; set; } = 0.05f;
        public int OctavesY { get; set; } = 3;

        public int AmplitudeX { get; set; } = 4;
        public float FrequencyX { get; set; } = 0.05f;
        public int OctavesX { get; set; } = 3;

        public float CaveFrequency { get; set; } = 0.08f;
        public float TunnelThickness { get; set; } = 0.1f;
        public int CaveSeedOffset1 { get; set; } = 1000;
        public int CaveSeedOffset2 { get; set; } = 2000;
    }

    /// <summary>
    /// Допоміжні фабрики для збірки MapGenerator у тестах.
    /// </summary>
    public static class GeneratorFactory
    {
        public static Dictionary<BiomeType, IBiomeGenerationConfig> SingleBiome(IBiomeGenerationConfig biome) =>
            new() { { biome.BiomeType, biome } };

        public static MapGenerator Create(IWorldGenerationConfig world, IBiomeGenerationConfig biome) =>
            new(world, SingleBiome(biome));
    }
}
