using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Coroutines.Tests
{
    public class CoroutineTest
    {
        class RunCoroutineExecuteThrowsException : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                // This code has no yield statements and is not
                // a enumerator (just a normal function)
                throw new Exception();
            }
        }

        [Fact]
        public void RunCoroutine_ExecuteThrowsException()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineExecuteThrowsException();

            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);

            // This throws exception on Execute as there are no yield statements
            // and calling that method actually executes it (it does not prepare enumeration)
            Assert.Throws<Exception>(() => scheduler.Execute(coroutine));
        }

        class RunCoroutineExecuteThrowsExceptionInCoroutine : Coroutine
        {
            int numberOfNullReturns = 0;

            public RunCoroutineExecuteThrowsExceptionInCoroutine(int nullReturns)
            {
                numberOfNullReturns = nullReturns;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                for (int i = 0; i < numberOfNullReturns; i++)
                {
                    yield return null;
                }
                throw new Exception();
            }
        }

        [Fact]
        public void RunCoroutine_ThrowsException()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineExecuteThrowsExceptionInCoroutine(1);

            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);
            scheduler.Execute(coroutine);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            scheduler.Update(0);

            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<Exception>(coroutine.Exception);
        }

        class RunCoroutineWithYieldNull : Coroutine
        {
            public int Iteration { get; private set; } = 0;

            protected override IEnumerator<IWaitObject> Execute()
            {
                Iteration = 1;
                yield return null;
                Iteration = 2;
                yield return Coroutines.NextFrame(); // same as null
                Iteration = 3;
                yield break;
            }
        }

        [Fact]
        public void RunCoroutine_WithYieldNull()
        {
            var scheduler = new CoroutineScheduler();

            var coroutine = new RunCoroutineWithYieldNull();
            Assert.Equal(CoroutineStatus.WaitingForStart, coroutine.Status);
            scheduler.Execute(coroutine);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            Assert.Equal(0, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(1, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(2, coroutine.Iteration);
            scheduler.Update(0);
            Assert.Equal(3, coroutine.Iteration);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        class CustomWaitObject : IWaitObject
        {
            public void Complete(Exception ex = null)
            {
                IsComplete = true;
                Exception = ex;
            }

            public bool IsComplete { get; private set; } = false;
            public Exception Exception { get; private set; }
        }

        class CustomWaitObjectCoroutine : Coroutine
        {
            IWaitObject waitObject;

            public CustomWaitObjectCoroutine(IWaitObject waitObject)
            {
                this.waitObject = waitObject;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return waitObject;
            }
        }

        [Fact]
        public void RunCoroutine_YieldCustomWaitObject()
        {
            var scheduler = new CoroutineScheduler();

            var waitObject = new CustomWaitObject();
            var coroutine = new CustomWaitObjectCoroutine(waitObject);

            scheduler.Execute(coroutine);

            for (int i = 0; i < 10; i++)
                scheduler.Update(0);

            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            waitObject.Complete(null);
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        [Fact]
        public void RunCoroutine_YieldCustomWaitObject_WithException()
        {
            var scheduler = new CoroutineScheduler();

            var waitObject = new CustomWaitObject();
            var coroutine = new CustomWaitObjectCoroutine(waitObject);

            scheduler.Execute(coroutine);

            for (int i = 0; i < 10; i++)
                scheduler.Update(0);

            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            waitObject.Complete(new Exception());
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<AggregateException>(coroutine.Exception);
        }

        class CustomWaitObjectWithNotifyCompletion : IWaitObjectWithNotifyCompletion
        {
            object syncRoot = new object();
            Action onCompleted;

            public void Complete(Exception ex = null)
            {
                lock (syncRoot)
                {
                    IsComplete = true;
                    Exception = ex;

                    onCompleted?.Invoke();
                }
            }

            public void RegisterCompleteSignal(Action onCompleted)
            {
                lock (syncRoot)
                {
                    // The custom implementation must trigger event if it is already completed
                    if (IsComplete)
                    {
                        onCompleted();
                    }
                    else
                    {
                        this.onCompleted += onCompleted;
                    }
                }
            }

            public bool IsComplete { get; private set; } = false;
            public Exception Exception { get; private set; }
        }

        [Fact]
        public async Task RunCoroutine_YieldCustomWaitObjectWithNotify()
        {
            var scheduler = new CoroutineScheduler();

            var waitObject = new CustomWaitObjectWithNotifyCompletion();
            var coroutine = new CustomWaitObjectCoroutine(waitObject);

            scheduler.Execute(coroutine);

            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            Task completionTask = Task.Run(async () =>
            {
               await Task.Delay(500);
               waitObject.Complete(null);
            });

            while (coroutine.Status == CoroutineStatus.Running)
            {
                scheduler.Update(0);
                await Task.Delay(10);
            }

            Assert.Equal(TaskStatus.RanToCompletion, completionTask.Status);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }


    }
}
