using System.Collections.Generic;
using Core.WorldGeneration;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// Тести повного конвеєра генерації світу (GenerateMapChunks) на реальних конфігах з DataManager:
    /// детермінізм, кількість і повнота сітки чанків, наявність суцільних блоків.
    /// </summary>
    public class MapGenerationTests
    {
        private static int _width;
        private static int _height;

        [OneTimeSetUp]
        public void LoadWorldDimensions()
        {
            DataManager.Initialize();
            var config = DataManager.WorldConfigs[0];
            _width = config.Width;
            _height = config.Height;
        }

        // Дві незалежні генерації дають поклітинно ідентичні світи (фіксований seed).
        [Test]
        public void GenerateMapChunks_IsDeterministic()
        {
            Dictionary<Vector2Int, ChunkData> first = new MapGenerator().GenerateMapChunks();
            Dictionary<Vector2Int, ChunkData> second = new MapGenerator().GenerateMapChunks();

            Assert.That(second.Count, Is.EqualTo(first.Count));

            foreach (var (coord, firstChunk) in first)
            {
                Assert.That(second.ContainsKey(coord), Is.True, $"У другій генерації немає чанка {coord}");
                ChunkData secondChunk = second[coord];

                for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex; i++)
                {
                    Assert.That(secondChunk[i], Is.EqualTo(firstChunk[i]),
                        $"Розбіжність у чанку {coord}, клітинка {i}");
                }
            }
        }

        // Словник містить рівно Width×Height чанків.
        [Test]
        public void GenerateMapChunks_ReturnsChunkPerGridCell()
        {
            Dictionary<Vector2Int, ChunkData> chunks = new MapGenerator().GenerateMapChunks();

            Assert.That(chunks.Count, Is.EqualTo(_width * _height));
        }

        // Кожна координата сітки 0..Width-1 × 0..Height-1 присутня ключем.
        [Test]
        public void GenerateMapChunks_ContainsEveryChunkCoordinate()
        {
            Dictionary<Vector2Int, ChunkData> chunks = new MapGenerator().GenerateMapChunks();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Assert.That(chunks.ContainsKey(new Vector2Int(x, y)), Is.True,
                        $"Немає чанка ({x},{y})");
                }
            }
        }

        // Згенерований світ не порожній — існує хоча б один не-Air блок.
        [Test]
        public void GenerateMapChunks_ProducesSolidBlocks()
        {
            Dictionary<Vector2Int, ChunkData> chunks = new MapGenerator().GenerateMapChunks();

            bool anySolid = false;
            foreach (var chunk in chunks.Values)
            {
                for (ushort i = 0; i < ChunkUtils.ChunkMaxIndex && !anySolid; i++)
                {
                    if (!chunk[i].IsAir)
                        anySolid = true;
                }
                if (anySolid)
                    break;
            }

            Assert.That(anySolid, Is.True, "Увесь згенерований світ складається з Air");
        }
    }
}
