using Core.WorldGeneration;
using NUnit.Framework;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести детермінізму <see cref="MapGenerator"/>.
    /// Однаковий seed мусить давати ідентичний світ — це фундамент мультиплеєра:
    /// сервер і клієнт генерують ту саму карту незалежно, без пересилання її цілком.
    /// </summary>
    public class MapGeneratorDeterminismTests
    {
        // Два незалежні генератори з тим самим seed дають поклітинно ідентичні світи.
        [Test]
        public void Generate_WithSameSeed_ProducesIdenticalWorlds()
        {
            var world = new FakeWorldConfig { Width = 64, Height = 64, Seed = 20260721 };
            var biome = new FakeBiomeConfig();

            BlockType[,] first = GeneratorFactory.Create(world, biome).Generate();
            BlockType[,] second = GeneratorFactory.Create(world, biome).Generate();

            Assert.That(second.GetLength(0), Is.EqualTo(first.GetLength(0)));
            Assert.That(second.GetLength(1), Is.EqualTo(first.GetLength(1)));

            for (int x = 0; x < first.GetLength(0); x++)
            {
                for (int y = 0; y < first.GetLength(1); y++)
                {
                    Assert.That(second[x, y], Is.EqualTo(first[x, y]),
                        $"Розбіжність у клітинці ({x},{y}) при однаковому seed");
                }
            }
        }
    }
}
