using System.Linq;
using NUnit.Framework;

namespace Core.Tests.EditMode
{
    /// <summary>
    /// Зонд: перевіряє, чи DataManager вдається ініціалізувати в EditMode (Addressables).
    /// Від цього залежить можливість тестувати генерацію світу, яка бере конфіги з DataManager.
    /// </summary>
    public class DataManagerProbeTests
    {
        // DataManager ініціалізується в EditMode, реєстри конфігів і біомів не порожні.
        [Test]
        public void Initialize_InEditMode_LoadsConfigsAndBiomes()
        {
            DataManager.Initialize();

            Assert.That(DataManager.IsInitialized, Is.True, "DataManager не ініціалізувався");
            Assert.That(DataManager.WorldConfigs.Any(), Is.True, "WorldConfigs порожній — Addressables не завантажились");
            Assert.That(DataManager.Biomes.Any(), Is.True, "Biomes порожній — Addressables не завантажились");
        }
    }
}
