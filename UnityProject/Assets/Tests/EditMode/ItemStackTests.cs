using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Shared.Tests.EditMode
{
    /// <summary>
    /// EditMode-тести для ItemStack та NullableItemStack.
    /// Покривають створення (включно з захисним клемпом невалідної кількості), 
    /// інкремент/декремент та різницю
    /// «порожній слот / зайнятий слот». <c>IsFilled</c> свідомо не тестується:
    /// він тягне <c>ItemID.ItemData</c> → <c>DataManager</c> (Addressables).
    /// </summary>
    public class ItemStackTests
    {
        private static readonly ItemID SomeItem = new ItemID(1);

        // Короткий конструктор створює стек із одним предметом.
        [Test]
        public void Ctor_WithItemIdOnly_StartsWithCountOne()
        {
            var stack = new ItemStack(SomeItem);

            Assert.That(stack.Count, Is.EqualTo(1));
            Assert.That(stack.ItemID, Is.EqualTo(SomeItem));
        }

        // Валідна кількість зберігається як є.
        [Test]
        public void Ctor_WithValidCount_PreservesCount()
        {
            var stack = new ItemStack(SomeItem, 5);

            Assert.That(stack.Count, Is.EqualTo(5));
        }

        // Нульова кількість клемпиться до 1 (з error-логом).
        // Примітка: лог друкує «got: 1» — уже виправлене значення, а не передане (count
        // перезаписується до логування). Тест фіксує цю поточну поведінку.
        [Test]
        public void Ctor_WithZeroCount_ClampsToOne()
        {
            LogAssert.Expect(LogType.Error, "Stack count cannot be non positive, got: 1");

            var stack = new ItemStack(SomeItem, 0);

            Assert.That(stack.Count, Is.EqualTo(1));
        }

        // Від'ємна кількість теж клемпиться до 1 (з тим самим логом).
        [Test]
        public void Ctor_WithNegativeCount_ClampsToOne()
        {
            LogAssert.Expect(LogType.Error, "Stack count cannot be non positive, got: 1");

            var stack = new ItemStack(SomeItem, -3);

            Assert.That(stack.Count, Is.EqualTo(1));
        }

        // Incremented повертає стек із кількістю +1.
        [Test]
        public void Incremented_ReturnsStackWithCountPlusOne()
        {
            var stack = new ItemStack(SomeItem, 5);

            var result = stack.Incremented();

            Assert.That(result.Count, Is.EqualTo(6));
            Assert.That(result.ItemID, Is.EqualTo(SomeItem));
        }

        // Decremented повертає стек із кількістю −1.
        // Примітка: нижньої межі немає — з 1 можна зійти в 0 (і нижче). Тест фіксує
        // поточну поведінку; захист від від'ємних стеків має жити на рівні інвентаря.
        [Test]
        public void Decremented_ReturnsStackWithCountMinusOne()
        {
            var stack = new ItemStack(SomeItem, 1);

            var result = stack.Decremented();

            Assert.That(result.Count, Is.EqualTo(0));
        }

        // default(NullableItemStack) — це «порожній слот»: HasValue == false.
        [Test]
        public void Default_HasNoValue()
        {
            NullableItemStack slot = default;

            Assert.That(slot.HasValue, Is.False);
        }

        // Конструктор загортає стек і позначає слот зайнятим.
        [Test]
        public void Ctor_WrapsStackAndHasValue()
        {
            var stack = new ItemStack(SomeItem, 7);

            var slot = new NullableItemStack(stack);

            Assert.That(slot.HasValue, Is.True);
            Assert.That(slot.ItemStack.Count, Is.EqualTo(7));
            Assert.That(slot.ItemStack.ItemID, Is.EqualTo(SomeItem));
        }

        // Неявна конверсія ItemStack → NullableItemStack дає зайнятий слот.
        [Test]
        public void ImplicitConversion_ProducesFilledNullable()
        {
            var stack = new ItemStack(SomeItem, 3);

            NullableItemStack slot = stack;

            Assert.That(slot.HasValue, Is.True);
            Assert.That(slot.ItemStack.Count, Is.EqualTo(3));
        }
    }
}
