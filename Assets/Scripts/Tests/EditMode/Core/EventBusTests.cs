using GameLike.Core.Events;
using NUnit.Framework;

namespace GameLike.Tests.EditMode.Core
{
    /// <summary>
    /// Reference test suite for the EventBus. New event-driven systems should
    /// follow this same shape: publish, assert the handler ran with the right
    /// payload, unsubscribe, assert it stops reacting.
    /// </summary>
    [TestFixture]
    public sealed class EventBusTests
    {
        [TearDown]
        public void TearDown()
        {
            // EventBus is static and shared across tests — always reset state
            // after each test to avoid handlers leaking between cases.
            EventBus.Clear();
        }

        [Test]
        public void Publish_InvokesSubscribedHandler()
        {
            var received = 0;
            void Handler(WeekTickEvent e) => received = e.Week;

            EventBus.Subscribe<WeekTickEvent>(Handler);
            EventBus.Publish(new WeekTickEvent(week: 5, year: 2));

            Assert.AreEqual(5, received);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new WeekTickEvent(1, 1)));
        }

        [Test]
        public void Unsubscribe_StopsHandlerFromReceivingFurtherEvents()
        {
            var callCount = 0;
            void Handler(WeekTickEvent e) => callCount++;

            EventBus.Subscribe<WeekTickEvent>(Handler);
            EventBus.Publish(new WeekTickEvent(1, 1));

            EventBus.Unsubscribe<WeekTickEvent>(Handler);
            EventBus.Publish(new WeekTickEvent(2, 1));

            Assert.AreEqual(1, callCount, "Handler should not run after unsubscribing.");
        }

        [Test]
        public void Publish_NotifiesMultipleSubscribersOfTheSameEvent()
        {
            var firstCalled = false;
            var secondCalled = false;

            EventBus.Subscribe<WeekTickEvent>(_ => firstCalled = true);
            EventBus.Subscribe<WeekTickEvent>(_ => secondCalled = true);

            EventBus.Publish(new WeekTickEvent(1, 1));

            Assert.IsTrue(firstCalled);
            Assert.IsTrue(secondCalled);
        }

        [Test]
        public void Subscribers_OfDifferentEventTypes_DoNotInterfereWithEachOther()
        {
            var weekTickReceived = false;
            var moneyChangedReceived = false;

            EventBus.Subscribe<WeekTickEvent>(_ => weekTickReceived = true);
            EventBus.Subscribe<MoneyChangedEvent>(_ => moneyChangedReceived = true);

            EventBus.Publish(new WeekTickEvent(1, 1));

            Assert.IsTrue(weekTickReceived);
            Assert.IsFalse(moneyChangedReceived, "Publishing one event type must not trigger handlers of another type.");
        }
    }
}
