public enum BiomeType
{
    Plains,
    Desert,
    Ocean,
    Caves,
    Jungle,
    JungleCaves,
    Lava
}

public static class BiomeTypeExtensions
{
    public static BiomeGenerationData GenerationData(this BiomeType biomeType)
        => DataManager.Biomes[biomeType];
}