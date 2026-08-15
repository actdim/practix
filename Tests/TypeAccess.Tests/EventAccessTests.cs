using ActDim.Practix.TypeAccess.Reflection;
using System;
using Xunit;

namespace ActDim.Practix.TypeAccess.Tests
{
    public class EventAccessTests
    {
        public class EventPublisher
        {
            public event EventHandler<string> SomethingHappened;

            public void Raise(string message)
            {
                SomethingHappened?.Invoke(this, message);
            }
        }

        [Fact]
        public void CanAddAndRemoveEventHandlersDynamically()
        {
            var publisher = new EventPublisher();
            var eventInfo = typeof(EventPublisher).GetEvent(nameof(EventPublisher.SomethingHappened));
            Assert.NotNull(eventInfo);

            var adder = TypeAccessor.GetEventAdder(eventInfo);
            var remover = TypeAccessor.GetEventRemover(eventInfo);

            string receivedMessage = null;
            EventHandler<string> handler = (sender, msg) => receivedMessage = msg;

            adder(publisher, handler);
            publisher.Raise("Hello Event");
            Assert.Equal("Hello Event", receivedMessage);

            receivedMessage = null;
            remover(publisher, handler);
            publisher.Raise("Hello Again");
            Assert.Null(receivedMessage);
        }

        [Fact]
        public void CanAddAndRemoveTypedEventHandlers()
        {
            var publisher = new EventPublisher();
            var eventInfo = typeof(EventPublisher).GetEvent(nameof(EventPublisher.SomethingHappened));
            Assert.NotNull(eventInfo);

            var adder = TypeAccessor.GetEventAdder<EventPublisher, EventHandler<string>>(eventInfo);
            var remover = TypeAccessor.GetEventRemover<EventPublisher, EventHandler<string>>(eventInfo);

            int count = 0;
            EventHandler<string> handler = (sender, msg) => count++;

            adder(publisher, handler);
            publisher.Raise("Test");
            Assert.Equal(1, count);

            remover(publisher, handler);
            publisher.Raise("Test");
            Assert.Equal(1, count);
        }
    }
}
