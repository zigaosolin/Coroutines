# Coroutines
A C# implementation of Coroutines similar to Unity (using IEnumerator). Coroutines are "sliced" on each yield statement, very similarly to how "await" slices tasks in C# async.

```
IEnumerator<IWaitObject> Execute()
{
   float time = 0.0f;
   while(time < 1.0f)
   {
      // Waits for "next frame"
      yield return null;

      // Execution state holds globals
      time += ExecutionState.DeltaTime;
   }

   // We can efficiently wait (without polling)
   yield return WaitForSeconds(1.0f);

   // We can wait for other coroutines. It is autostarted
   // and the first frame is processed immediatelly
   yield return new OtherCoroutine(...);
   
   // We can wait for async
   var wait = WaitForAsync<string>(file.ReadLineAsync());
   yield return wait;
   string line = wait.Result;

   // Generally, any wait object can return result
   var wait = new OtherCoroutineWithResult(...);
   yield return wait;
   var result = wait.Result;
}
```

Coroutines can be put on schedulers. When you call update on the interleaved scheduler, it executes one frame for all running coroutines. All coroutines waiting for trigger stay idle until trigerred.

Compared to async/await, you have much greater control over coroutines. You can cancel them at any time, they are thread safe if used correctly as they are only updated in scheduler's update thread and you can synchronize schedulers

# Reactor

On top of coroutines, actor based system (event based system) is also being built. While Coroutines are probably close to final design, Reactor system is there only for experimentation in the moment and probably won't be finished.
