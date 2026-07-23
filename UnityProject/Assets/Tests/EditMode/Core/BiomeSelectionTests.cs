using Core.WorldGeneration;
using NUnit.Framework;
using UnityEngine;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// Тести вибору біому за координатами чанка (GetBiomeTypeAt).
    /// Координати будуються відносно реальних розмірів світу з DataManager,
    /// тож тести не залежать від конкретних чисел у конфізі.
    /// </summary>
    public class BiomeSelectionTests
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

        // Координата чанка, що дає приблизно заданий відсоток по ширині.
        private static int XAt(float percent) => Mathf.Clamp(Mathf.RoundToInt(percent * _width), 0, _width - 1);

        // Координата чанка, що дає приблизно заданий відсоток по висоті.
        private static int YAt(float percent) => Mathf.Clamp(Mathf.RoundToInt(percent * _height), 1, _height - 1);

        private static BiomeType BiomeAt(int x, int y) =>
            MapGenerator.GetBiomeTypeAt(new Vector2Int(x, y));

        // Найнижчий ряд чанків (y == 0) — це Lava.
        [Test]
        public void BottomRow_IsLava()
        {
            Assert.That(BiomeAt(XAt(0.5f), 0), Is.EqualTo(BiomeType.Lava));
        }

        // Печерна зона (нижня частина), ліва сторона — Caves.
        [Test]
        public void CaveRegion_LeftSide_IsCaves()
        {
            Assert.That(BiomeAt(XAt(0.3f), YAt(0.3f)), Is.EqualTo(BiomeType.Caves));
        }

        // Печерна зона, права сторона — JungleCaves.
        [Test]
        public void CaveRegion_RightSide_IsJungleCaves()
        {
            Assert.That(BiomeAt(XAt(0.85f), YAt(0.3f)), Is.EqualTo(BiomeType.JungleCaves));
        }

        // Поверхнева зона, права сторона — Jungle.
        [Test]
        public void Surface_RightSide_IsJungle()
        {
            Assert.That(BiomeAt(XAt(0.85f), YAt(0.8f)), Is.EqualTo(BiomeType.Jungle));
        }

        // Поверхнева зона, крайня ліва — Ocean.
        [Test]
        public void Surface_FarLeft_IsOcean()
        {
            Assert.That(BiomeAt(XAt(0.02f), YAt(0.8f)), Is.EqualTo(BiomeType.Ocean));
        }

        // Поверхнева зона, ліва смуга — Desert.
        [Test]
        public void Surface_LeftBand_IsDesert()
        {
            Assert.That(BiomeAt(XAt(0.2f), YAt(0.8f)), Is.EqualTo(BiomeType.Desert));
        }

        // Поверхнева зона, середина — Plains.
        [Test]
        public void Surface_Middle_IsPlains()
        {
            Assert.That(BiomeAt(XAt(0.5f), YAt(0.8f)), Is.EqualTo(BiomeType.Plains));
        }

        // y == 0 має пріоритет: навіть там, де інакше були б Caves, повертається Lava.
        [Test]
        public void LavaPriority_OverridesCaveRegion()
        {
            Assert.That(BiomeAt(XAt(0.3f), 0), Is.EqualTo(BiomeType.Lava));
        }
    }
}
