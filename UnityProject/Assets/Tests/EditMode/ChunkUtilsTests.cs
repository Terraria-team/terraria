using NUnit.Framework;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести для <see cref="ChunkUtils"/> — координатної математики чанків.
    /// Перевіряють розміри чанка, межові клітинки, унікальність індексів
    /// та взаємну оберненість перетворень (x,y) ↔ index.
    /// </summary>
    public class ChunkUtilsTests
    {
        // Максимальний індекс дорівнює площі чанка (64 × 64 = 4096).
        [Test]
        public void ChunkMaxIndex_EqualsChunkSizeSquared()
        {
            Assert.That(ChunkUtils.ChunkMaxIndex, Is.EqualTo(ChunkUtils.ChunkSize * ChunkUtils.ChunkSize));
        }

        // Початок координат (0,0) відповідає індексу 0.
        [Test]
        public void ChunkCellIndex_ForOrigin_ReturnsZero()
        {
            Assert.That(ChunkUtils.ChunkCellIndex(0, 0), Is.EqualTo(0));
        }

        // Остання клітинка (63,63) відповідає останньому індексу масиву.
        [Test]
        public void ChunkCellIndex_ForLastCell_ReturnsLastArrayIndex()
        {
            const byte last = ChunkUtils.ChunkSize - 1;

            Assert.That(ChunkUtils.ChunkCellIndex(last, last), Is.EqualTo(ChunkUtils.ChunkMaxIndex - 1));
        }

        // Кожна клітинка чанка має унікальний індекс, і разом вони покривають увесь масив.
        [Test]
        public void ChunkCellIndex_IsUniqueForEveryCell()
        {
            var seen = new bool[ChunkUtils.ChunkMaxIndex];

            for (byte x = 0; x < ChunkUtils.ChunkSize; x++)
            {
                for (byte y = 0; y < ChunkUtils.ChunkSize; y++)
                {
                    ushort index = ChunkUtils.ChunkCellIndex(x, y);

                    Assert.That(index, Is.LessThan(ChunkUtils.ChunkMaxIndex), $"Індекс поза межами для ({x},{y})");
                    Assert.That(seen[index], Is.False, $"Індекс {index} повторився на ({x},{y})");
                    seen[index] = true;
                }
            }

            Assert.That(seen, Has.None.False, "Деякі індекси масиву не відповідають жодній клітинці");
        }

        // Сусідні x відрізняються на 1, сусідні y — на розмір чанка.
        [Test]
        public void ChunkCellIndex_StepsByOneAlongXAndByChunkSizeAlongY()
        {
            ushort origin = ChunkUtils.ChunkCellIndex(5, 5);

            Assert.That(ChunkUtils.ChunkCellIndex(6, 5) - origin, Is.EqualTo(1));
            Assert.That(ChunkUtils.ChunkCellIndex(5, 6) - origin, Is.EqualTo(ChunkUtils.ChunkSize));
        }

        // ChunkCellCoordinates має бути оберненою до ChunkCellIndex для будь-якої клітинки.
        [Test]
        public void ChunkCellCoordinates_IsInverseOfChunkCellIndex()
        {
            for (byte x = 0; x < ChunkUtils.ChunkSize; x++)
            {
                for (byte y = 0; y < ChunkUtils.ChunkSize; y++)
                {
                    ushort index = ChunkUtils.ChunkCellIndex(x, y);
                    var coordinates = ChunkUtils.ChunkCellCoordinates(index);

                    Assert.That(coordinates.x, Is.EqualTo(x), $"x не збігся для ({x},{y}) → index {index}");
                    Assert.That(coordinates.y, Is.EqualTo(y), $"y не збігся для ({x},{y}) → index {index}");
                }
            }
        }

        // Індекс 0 розкладається в початок координат.
        [Test]
        public void ChunkCellCoordinates_ForZeroIndex_ReturnsOrigin()
        {
            var coordinates = ChunkUtils.ChunkCellCoordinates(0);

            Assert.That(coordinates.x, Is.EqualTo(0));
            Assert.That(coordinates.y, Is.EqualTo(0));
        }
    }
}
