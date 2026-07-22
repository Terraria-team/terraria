using NUnit.Framework;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести для <see cref="ChunkData"/> — контейнера блоків одного чанка.
    /// Покривають заповнення при створенні, доступ через Get/Set та індексатор,
    /// а також незалежність копії після <c>Clone()</c>.
    /// </summary>
    public class ChunkDataTests
    {
        private static readonly BlockID Stone = new BlockID(1);
        private static readonly BlockID Dirt = new BlockID(2);

        /// <summary>Порівнює два чанки поклітинно (структурна рівність не підходить: усередині масив).</summary>
        private static void AssertSameCells(ChunkData expected, ChunkData actual)
        {
            for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"Розбіжність у клітинці {i}");
            }
        }

        // Конструктор із початковим значенням заповнює ним усі клітинки чанка.
        [Test]
        public void Constructor_WithInitialValue_FillsEveryCell()
        {
            var chunk = new ChunkData(Stone);

            for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
            {
                Assert.That(chunk[i], Is.EqualTo(Stone));
            }
        }

        // Записане через Set значення читається назад тим самим Get.
        [Test]
        public void SetThenGet_ReturnsWrittenValue()
        {
            var chunk = new ChunkData(BlockID.Air);

            chunk.Set(3, 5, Stone);

            Assert.That(chunk.Get(3, 5), Is.EqualTo(Stone));
        }

        // Set у одну клітинку не зачіпає сусідні.
        [Test]
        public void Set_DoesNotAffectOtherCells()
        {
            var chunk = new ChunkData(BlockID.Air);

            chunk.Set(3, 5, Stone);

            Assert.That(chunk.Get(4, 5), Is.EqualTo(BlockID.Air));
            Assert.That(chunk.Get(3, 6), Is.EqualTo(BlockID.Air));
            Assert.That(chunk.Get(2, 5), Is.EqualTo(BlockID.Air));
        }

        // Індексатор і Get звертаються до тієї самої клітинки.
        [Test]
        public void Indexer_MatchesGetForSameCell()
        {
            var chunk = new ChunkData(BlockID.Air);
            const byte x = 7;
            const byte y = 11;

            chunk.Set(x, y, Dirt);

            Assert.That(chunk[ChunkUtils.ChunkCellIndex(x, y)], Is.EqualTo(Dirt));
        }

        // Clone повертає незалежну копію: зміна копії не змінює оригінал.
        [Test]
        public void Clone_ProducesIndependentCopy()
        {
            var original = new ChunkData(BlockID.Air);
            original.Set(1, 1, Stone);

            var clone = original.Clone();
            clone.Set(1, 1, Dirt);
            clone.Set(2, 2, Dirt);

            Assert.That(original.Get(1, 1), Is.EqualTo(Stone), "Оригінал не мав змінитися");
            Assert.That(original.Get(2, 2), Is.EqualTo(BlockID.Air), "Оригінал не мав змінитися");
            Assert.That(clone.Get(1, 1), Is.EqualTo(Dirt));
        }

        // Свіжий Clone поклітинно збігається з оригіналом.
        [Test]
        public void Clone_HasSameContentAsOriginal()
        {
            var original = new ChunkData(BlockID.Air);
            original.Set(0, 0, Stone);
            original.Set(63, 63, Dirt);

            AssertSameCells(original, original.Clone());
        }
    }
}
