using NUnit.Framework;
using UnityEngine;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// Тести перетворення світової позиції в координати чанка.
    /// </summary>
    public class ChunkCoordsTests
    {
        // Початок координат належить чанку (0,0).
        [Test]
        public void AtOrigin_ReturnsChunkZero()
        {
            var chunk = ChunkUtils.ChunkCoordsAtWorldPosition(new Vector2(0f, 0f));

            Assert.That(chunk, Is.EqualTo(new Vector2Int(0, 0)));
        }

        // Дробова позиція всередині першого чанка не зсуває результат.
        [Test]
        public void InsideFirstChunk_ReturnsChunkZero()
        {
            var chunk = ChunkUtils.ChunkCoordsAtWorldPosition(new Vector2(10.5f, 63.9f));

            Assert.That(chunk, Is.EqualTo(new Vector2Int(0, 0)));
        }

        // Позиція рівно на межі належить наступному чанку.
        [Test]
        public void AtChunkBoundary_ReturnsNextChunk()
        {
            var chunk = ChunkUtils.ChunkCoordsAtWorldPosition(
                new Vector2(ChunkUtils.ChunkSize, 2 * ChunkUtils.ChunkSize));

            Assert.That(chunk, Is.EqualTo(new Vector2Int(1, 2)));
        }

        // Віддалена позиція потрапляє в правильний чанк.
        [Test]
        public void FarChunk_ComputedCorrectly()
        {
            var chunk = ChunkUtils.ChunkCoordsAtWorldPosition(new Vector2(200f, 70f));

            Assert.That(chunk, Is.EqualTo(new Vector2Int(3, 1)));
        }

        // Від'ємні позиції мають округлюватись вниз (floor) до від'ємного чанка.
        [Test]
        public void NegativeCoordinates_FloorToNegativeChunk()
        {
            var nearOrigin = ChunkUtils.ChunkCoordsAtWorldPosition(new Vector2(-10f, -10f));
            var beyondOneChunk = ChunkUtils.ChunkCoordsAtWorldPosition(new Vector2(-70f, -70f));

            Assert.That(nearOrigin, Is.EqualTo(new Vector2Int(-1, -1)),
                "Позиція (-10,-10) має належати чанку (-1,-1)");
            Assert.That(beyondOneChunk, Is.EqualTo(new Vector2Int(-2, -2)),
                "Позиція (-70,-70) має належати чанку (-2,-2)");
        }
    }
}
