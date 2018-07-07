using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Coroutines.Tests
{
    public class SubCoroutinesTest
    {
        public class SubCoroutine : Coroutine
        {
            public int SumWork { get; private set; } = 0;
            SubCoroutine[] children = new SubCoroutine[0];
            bool spawnParallel;
            bool executeImmediatelly = false;

            public SubCoroutine()
            {
            }

            public SubCoroutine(bool spawnParallel, bool executeImmediatelly, params SubCoroutine[] executeChildren)
            {
                children = executeChildren;
                this.executeImmediatelly = executeImmediatelly;
                this.spawnParallel = spawnParallel;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                if (children.Length == 0)
                {
                    yield return null;
                }
                else
                {
                    if (spawnParallel)
                    {
                        foreach (var child in children)
                        {
                            if (executeImmediatelly)
                            {
                                // This will be processed now (first frame)
                                Scheduler.ExecuteImmediately(child);
                            }
                            else
                            {
                                // This will be processed in the NEXT frame
                                Scheduler.Execute(child);
                            }
                        }

                        // Wait for the last child
                        yield return children[children.Length - 1];
                    }
                    else
                    {
                        foreach (var child in children)
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        [Fact]
        public void SubCoroutine_SpawnOneChild()
        {
            var subCoroutine = new SubCoroutine();
            var coroutine = new SubCoroutine(
                false, false, subCoroutine);

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);

            Assert.Equal(CoroutineStatus.WaitingForStart, subCoroutine.Status);

            // Yield of child executes immediatelly. As child
            // has one yield null, we expect that after one update,
            // it is still running, second update it completes
            // and also completes parent
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);
            Assert.Equal(CoroutineStatus.Running, subCoroutine.Status);

            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, subCoroutine.Status);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        [Fact]
        public void SubCoroutine_SpawnSequentialChildren()
        {
            var coroutine = new SubCoroutine(
                spawnParallel: false, executeImmediatelly: false, 
                executeChildren: new SubCoroutine[] { new SubCoroutine(), new SubCoroutine(), new SubCoroutine() });

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);

            for (int i = 0; i < 3; i++)
                scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        [Fact]
        public void SubCoroutine_SpawnParallelChildren()
        {
            var subCoroutines = new List<SubCoroutine>()
            {
                new SubCoroutine(),
                new SubCoroutine(),
                new SubCoroutine()
            };

            var coroutine = new SubCoroutine(
                spawnParallel: true, executeImmediatelly: false, executeChildren: subCoroutines.ToArray());

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);

            scheduler.Update(0); //< Executes ALL children (not processed in this frame)
            scheduler.Update(0); //< All children execute in one frame
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            scheduler.Update(0); //< All children complete
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }

        [Fact]
        public void SubCoroutine_ExecuteChildrenImmediatelly()
        {
            var subCoroutines = new List<SubCoroutine>()
            {
                new SubCoroutine(),
                new SubCoroutine(),
                new SubCoroutine()
            };

            var coroutine = new SubCoroutine(
                spawnParallel: true, executeImmediatelly: true, executeChildren: subCoroutines.ToArray());

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);

            scheduler.Update(0); //< Executes ALL children with one frame
            Assert.Equal(CoroutineStatus.Running, coroutine.Status);

            scheduler.Update(0); //< All children complete
            Assert.Equal(CoroutineStatus.CompletedNormal, coroutine.Status);
        }


        public class BaseCancelCoroutine : Coroutine
        {
            public Coroutine Internal { get; private set; }

            IEnumerator<IWaitObject> ToCancelCoroutine()
            {
                yield return null;
            }

            protected override IEnumerator<IWaitObject> Execute()
            {
                Internal = Coroutines.FromEnumerator(ToCancelCoroutine());
                yield return Internal;
            }
        }

        [Fact]
        public void CoroutineExceptionPropagation()
        {
            var coroutine = new BaseCancelCoroutine();

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);
            scheduler.Update(0);
            coroutine.Internal.Cancel();
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.Cancelled, coroutine.Internal.Status);
            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<AggregateException>(coroutine.Exception);
        }

        class CustomWaitObject : IWaitObject
        {
            public bool IsComplete { get; set; }
            public Exception Exception { get; set; }
        }

        IEnumerator<IWaitObject> WaitForWaitObject(CustomWaitObject which)
        {
            yield return which;
        }

        [Fact]
        public void Coroutine_WhenOnWaitObject()
        {
            var waitObject = new CustomWaitObject();
            var coroutine = Coroutines.FromEnumerator(WaitForWaitObject(waitObject));

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);
            scheduler.Update(0);
            waitObject.Exception = new Exception();
            waitObject.IsComplete = true;
            scheduler.Update(0);
            Assert.Equal(CoroutineStatus.CompletedWithException, coroutine.Status);
            Assert.IsType<AggregateException>(coroutine.Exception);
        }

        class SpawnedCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                yield return null;
            }
        }

        class SpawnerCoroutine : Coroutine
        {
            protected override IEnumerator<IWaitObject> Execute()
            {
                Spawned = new SpawnedCoroutine();
                yield return Spawned;
            }

            public SpawnedCoroutine Spawned{ get; set; }
        }

        [Fact]
        public void CoroutineSpawner()
        {
            var waitObject = new CustomWaitObject();
            var coroutine = new SpawnerCoroutine();

            var scheduler = new InterleavedCoroutineScheduler();
            scheduler.Execute(coroutine);
            scheduler.Update(0);
            Assert.Equal(coroutine, coroutine.Spawned.Spawner);
        }

    }
}
