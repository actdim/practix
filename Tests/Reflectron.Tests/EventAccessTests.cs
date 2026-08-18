using System;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
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
        public void GetEventAdderAndRemover_UntypedDelegates_AddsAndRemovesHandler()
        {
            var publisher = new EventPublisher();
            var eventInfo = typeof(EventPublisher).GetEvent(nameof(EventPublisher.SomethingHappened));
            Assert.NotNull(eventInfo);

            var adder = Reflectron.GetEventAdder(eventInfo);
            var remover = Reflectron.GetEventRemover(eventInfo);

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
        public void GetEventAdderAndRemover_TypedDelegates_AddsAndRemovesHandler()
        {
            var publisher = new EventPublisher();
            var eventInfo = typeof(EventPublisher).GetEvent(nameof(EventPublisher.SomethingHappened));
            Assert.NotNull(eventInfo);

            var adder = Reflectron.GetEventAdder<EventPublisher, EventHandler<string>>(eventInfo);
            var remover = Reflectron.GetEventRemover<EventPublisher, EventHandler<string>>(eventInfo);

            int count = 0;
            EventHandler<string> handler = (sender, msg) => count++;

            adder(publisher, handler);
            publisher.Raise("Test");
            Assert.Equal(1, count);

            remover(publisher, handler);
            publisher.Raise("Test");
            Assert.Equal(1, count);
        }

        [Fact]
        public void GetEventAdder_NullEventInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Reflectron.GetEventAdder(null));
        }

        [Fact]
        public void GetEventRemover_NullEventInfo_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Reflectron.GetEventRemover(null));
        }
    }
}
