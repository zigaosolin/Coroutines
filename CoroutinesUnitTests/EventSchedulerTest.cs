using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public static class EventQueueExtensions
    {
        public static ICoroutineEvent DequeueEvent(this EventQueue eventQueue)
        {
            Assert.True(eventQueue.TryDequeue(out IEvent ev));
            Assert.IsAssignableFrom<ICoroutineEvent>(ev);
            return ev as ICoroutineEvent;
        }
    }

    public class EventSchedulerTest
    {
        public class NextFrameCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return null;
            }
        }

        [Fact]
        public void NextFrame()
        {
            var eventQueue = new EventQueue();
            var scheduler = new EventCoroutineScheduler(eventQueue);

            var coroutine = new NextFrameCoroutine();
            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);
            scheduler.Execute(coroutine);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            // Start event
            Assert.Equal(1, eventQueue.Count);
            scheduler.Update(eventQueue.DequeueEvent());
            Assert.Equal(0, eventQueue.CountCurrentFrame);
            Assert.Equal(1, eventQueue.Count);

            scheduler.NewFrame(0.1f);
            eventQueue.NewFrame();
            scheduler.Update(eventQueue.DequeueEvent());
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }


    }
}
