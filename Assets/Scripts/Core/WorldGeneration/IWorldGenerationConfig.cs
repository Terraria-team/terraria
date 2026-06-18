namespace Core.WorldGeneration
{
    public interface IWorldGenerationConfig
    {
        public int Width { get;}
        public int Height { get; }
        public int Seed { get; }
    }
}

