using System.Collections.Generic;
using NUnit.Framework;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести для <see cref="SparseChunkDelta"/> — дельти змін блоків чанка,
    /// що передається мережею. Ключовий інваріант: обчислити дельту між двома чанками
    /// й накласти її на вихідний — має вийти оновлений чанк.
    /// Також перевіряють, що накладання не мутує вихідний чанк.
    /// </summary>
    public class SparseChunkDeltaTests
    {
        private static readonly BlockID Stone = new BlockID(1);
        private static readonly BlockID Dirt = new BlockID(2);

        private static void AssertSameCells(ChunkData expected, ChunkData actual)
        {
            for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"Розбіжність у клітинці {i}");
            }
        }

        // Між двома однаковими чанками дельта порожня.
        [Test]
        public void Delta_BetweenIdenticalChunks_IsEmpty()
        {
            var original = new ChunkData(BlockID.Air);
            var updated = new ChunkData(BlockID.Air);

            var delta = new SparseChunkDelta(original, updated);

            Assert.That(delta.IsEmpty, Is.True);
            Assert.That(delta.Deltas, Is.Empty);
        }

        // Одна змінена клітинка дає рівно один запис із коректними індексом і значенням.
        [Test]
        public void Delta_WithSingleChange_ContainsExactlyThatChange()
        {
            const byte x = 3;
            const byte y = 5;
            var original = new ChunkData(BlockID.Air);
            var updated = original.Clone();
            updated.Set(x, y, Stone);

            var delta = new SparseChunkDelta(original, updated);

            Assert.That(delta.Deltas.Count, Is.EqualTo(1));
            Assert.That(delta.Deltas[0].Index, Is.EqualTo(ChunkUtils.ChunkCellIndex(x, y)));
            Assert.That(delta.Deltas[0].Value, Is.EqualTo(Stone));
        }

        // Головний інваріант: накладання дельти на вихідний чанк відтворює оновлений.
        [Test]
        public void ApplyDelta_ReproducesUpdatedChunk()
        {
            var original = new ChunkData(BlockID.Air);
            var updated = original.Clone();
            updated.Set(0, 0, Stone);
            updated.Set(10, 20, Dirt);
            updated.Set(63, 63, Stone);

            var delta = new SparseChunkDelta(original, updated);
            var result = delta.Apply(original);

            AssertSameCells(updated, result);
        }

        // Накладання дельти не мутує вихідний чанк — Apply працює на копії.
        [Test]
        public void Apply_DoesNotMutateSourceChunk()
        {
            var original = new ChunkData(BlockID.Air);
            var updated = original.Clone();
            updated.Set(7, 7, Stone);
            var delta = new SparseChunkDelta(original, updated);

            delta.Apply(original);

            Assert.That(original.Get(7, 7), Is.EqualTo(BlockID.Air));
        }

        // Повністю змінений чанк дає запис на кожну клітинку.
        [Test]
        public void Delta_WhenEveryCellChanged_ContainsEntryPerCell()
        {
            var original = new ChunkData(BlockID.Air);
            var updated = new ChunkData(Stone);

            var delta = new SparseChunkDelta(original, updated);

            Assert.That(delta.Deltas.Count, Is.EqualTo(ChunkUtils.ChunkMaxIndex));
            AssertSameCells(updated, delta.Apply(original));
        }

        // Порожня дельта лишає вміст чанка без змін.
        [Test]
        public void Apply_WithEmptyDelta_LeavesChunkUnchanged()
        {
            var original = new ChunkData(BlockID.Air);
            original.Set(4, 4, Dirt);
            var delta = new SparseChunkDelta(new List<ChunkDeltaEntry>());

            var result = delta.Apply(original);

            AssertSameCells(original, result);
        }

        // Дельта, зібрана вручну зі списку записів, накладається коректно.
        [Test]
        public void Apply_WithManuallyBuiltDelta_WritesExpectedCells()
        {
            var original = new ChunkData(BlockID.Air);
            var entries = new List<ChunkDeltaEntry>
            {
                new ChunkDeltaEntry(ChunkUtils.ChunkCellIndex(1, 2), Stone),
                new ChunkDeltaEntry(ChunkUtils.ChunkCellIndex(3, 4), Dirt)
            };
            var delta = new SparseChunkDelta(entries);

            var result = delta.Apply(original);

            Assert.That(result.Get(1, 2), Is.EqualTo(Stone));
            Assert.That(result.Get(3, 4), Is.EqualTo(Dirt));
            Assert.That(result.Get(0, 0), Is.EqualTo(BlockID.Air));
        }

        // Прибирання блоку (заміна на «повітря») фіксується так само, як і будь-яка інша зміна
        [Test]
        public void Delta_WhenBlockRemoved_RecordsAirValue()
        {
            var original = new ChunkData(Stone);
            var updated = original.Clone();
            updated.Set(2, 2, BlockID.Air);

            var delta = new SparseChunkDelta(original, updated);
            var result = delta.Apply(original);

            Assert.That(delta.Deltas.Count, Is.EqualTo(1));
            Assert.That(delta.Deltas[0].Value, Is.EqualTo(BlockID.Air));
            Assert.That(result.Get(2, 2), Is.EqualTo(BlockID.Air));
        }
    }
}
