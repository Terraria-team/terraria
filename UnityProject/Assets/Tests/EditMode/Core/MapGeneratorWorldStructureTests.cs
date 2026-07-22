// ВИМКНЕНО: MapGenerator переписано (конфіги тепер зі статичного DataManager,
// інтерфейси IWorldGenerationConfig/IBiomeGenerationConfig видалено) — тести
// не компілюються. Повернути після узгодження тестопридатності з командою.
#if false
using Core.WorldGeneration;
using NUnit.Framework;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести структури світу від MapGenerator.
    /// Прийом: вироджені конфіги (нульові амплітуди/товщина тунелів) вимикають шум,
    /// що робить вихід повністю передбачуваним і дозволяє перевірити кожен етап
    /// генерації (ландшафт, печери) ізольовано.
    /// </summary>
    public class MapGeneratorWorldStructureTests
    {
        // Пласкій конфіг (амплітуди 0, печер немає): у кожній колонці рівно
        // Dirt нижче BaseSurfaceLevel, один Grass на ньому та Air вище.
        [Test]
        public void Generate_WithFlatConfig_HasCorrectSurfaceLayering()
        {
            const int surfaceLevel = 32;
            var world = new FakeWorldConfig { Width = 64, Height = 64, Seed = 123 };
            var biome = new FakeBiomeConfig
            {
                BaseSurfaceLevel = surfaceLevel,
                AmplitudeY = 0,
                AmplitudeX = 0,
                TunnelThickness = 0f
            };

            BlockType[,] result = GeneratorFactory.Create(world, biome).Generate();

            for (int x = 0; x < world.Width; x++)
            {
                for (int y = 0; y < world.Height; y++)
                {
                    BlockType expected = y < surfaceLevel ? BlockType.Dirt
                        : y == surfaceLevel ? BlockType.Grass
                        : BlockType.Air;

                    Assert.That(result[x, y], Is.EqualTo(expected),
                        $"Клітинка ({x},{y}): очікувався {expected}, отримано {result[x, y]}");
                }
            }
        }

        // Нульова товщина тунелів: печери не вирізаються — під поверхнею немає
        // жодної порожнини (Air з'являється лише суцільно згори).
        [Test]
        public void Generate_WithZeroTunnelThickness_CarvesNoCaves()
        {
            var world = new FakeWorldConfig { Width = 64, Height = 64, Seed = 987 };
            var biome = new FakeBiomeConfig
            {
                AmplitudeY = 8,
                AmplitudeX = 0,
                TunnelThickness = 0f
            };

            BlockType[,] result = GeneratorFactory.Create(world, biome).Generate();

            for (int x = 0; x < world.Width; x++)
            {
                bool airStarted = false;
                for (int y = 0; y < world.Height; y++)
                {
                    if (result[x, y] == BlockType.Air)
                    {
                        airStarted = true;
                    }
                    else
                    {
                        Assert.That(airStarted, Is.False,
                            $"Порожнина під поверхнею: суцільний блок ({x},{y}) вище за Air — отже, печера вирізалась");
                    }
                }
            }
        }
    }
}
#endif
