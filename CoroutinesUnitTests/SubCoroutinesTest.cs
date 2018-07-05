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

        [Fact]
        public void CoroutineCancelTest_WhenOnWaitObject()
        {

        }

        [Fact]
        public void CoroutineCancelTest_CancelPropagation()
        {

        }

        [Fact]
        public void CoroutineExceptionPropagation()
        {

        }


        [Fact]
        public void CoroutineSpawner()
        {

        }

    }
}
