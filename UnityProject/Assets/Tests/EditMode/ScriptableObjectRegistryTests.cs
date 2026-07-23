using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// Тести реєстру ScriptableObject-ассетів: наповнення, обробка дублікатів і null,
    /// пошук за ключем, перелічення та очищення.
    /// </summary>
    public class ScriptableObjectRegistryTests
    {
        private class TestEntry : ScriptableObject
        {
            public int id;
        }

        private static TestEntry Entry(int id)
        {
            var entry = ScriptableObject.CreateInstance<TestEntry>();
            entry.id = id;
            return entry;
        }

        private static ScriptableObjectRegistry<TestEntry, int> CreateRegistry(params TestEntry[] entries)
        {
            var registry = new ScriptableObjectRegistry<TestEntry, int>();
            registry.Initialize(entries, e => e.id);
            return registry;
        }

        // Зареєстрований об'єкт дістається назад за своїм ключем.
        [Test]
        public void Initialize_ThenGet_ReturnsRegisteredObject()
        {
            var first = Entry(1);
            var second = Entry(2);

            var registry = CreateRegistry(first, second);

            Assert.That(registry.Get(1), Is.SameAs(first));
            Assert.That(registry.Get(2), Is.SameAs(second));
        }

        // null у вхідній колекції пропускається без помилок, решта реєструється.
        [Test]
        public void Initialize_SkipsNullEntries()
        {
            var entry = Entry(1);

            var registry = new ScriptableObjectRegistry<TestEntry, int>();
            registry.Initialize(new List<TestEntry> { null, entry, null }, e => e.id);

            Assert.That(registry.Get(1), Is.SameAs(entry));
            Assert.That(registry.Count(), Is.EqualTo(1));
        }

        // Дублікат ключа: лишається перший об'єкт, про другий пишеться error-лог.
        [Test]
        public void Initialize_WithDuplicateKey_KeepsFirstAndLogsError()
        {
            var first = Entry(7);
            var duplicate = Entry(7);
            LogAssert.Expect(LogType.Error, "Duplicate ID found: 7 for type TestEntry");

            var registry = CreateRegistry(first, duplicate);

            Assert.That(registry.Get(7), Is.SameAs(first));
            Assert.That(registry.Count(), Is.EqualTo(1));
        }

        // Повторний Initialize повністю замінює попередній вміст.
        [Test]
        public void Initialize_CalledTwice_ReplacesContents()
        {
            var old = Entry(1);
            var fresh = Entry(2);
            var registry = CreateRegistry(old);

            registry.Initialize(new[] { fresh }, e => e.id);

            Assert.That(registry.Get(2), Is.SameAs(fresh));
            Assert.That(registry.Count(), Is.EqualTo(1));
        }

        // Невідомий ключ: повертається null і пишеться error-лог.
        [Test]
        public void Get_UnknownId_LogsErrorAndReturnsNull()
        {
            var registry = CreateRegistry(Entry(1));
            LogAssert.Expect(LogType.Error, "ID 99 not found in TestEntry registry.");

            var result = registry.Get(99);

            Assert.That(result, Is.Null);
        }

        // Індексатор повертає те саме, що Get.
        [Test]
        public void Indexer_MatchesGet()
        {
            var entry = Entry(5);
            var registry = CreateRegistry(entry);

            Assert.That(registry[5], Is.SameAs(registry.Get(5)));
        }

        // Перелічення віддає всі зареєстровані об'єкти.
        [Test]
        public void Enumeration_YieldsAllRegisteredValues()
        {
            var first = Entry(1);
            var second = Entry(2);
            var registry = CreateRegistry(first, second);

            var values = registry.ToList();

            Assert.That(values, Has.Count.EqualTo(2));
            Assert.That(values, Does.Contain(first));
            Assert.That(values, Does.Contain(second));
        }

        // Після Clear реєстр порожній.
        [Test]
        public void Clear_EmptiesRegistry()
        {
            var registry = CreateRegistry(Entry(1), Entry(2));

            registry.Clear();

            Assert.That(registry.Count(), Is.EqualTo(0));
        }
    }
}
