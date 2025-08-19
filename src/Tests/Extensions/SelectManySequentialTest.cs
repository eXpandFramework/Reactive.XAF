



















//         var queues = new QueueDict<string>();
//         var key = "A";
//         int concurrentExecutions = 0;
//         int maxConcurrentExecutions = 0;
//         var lockObject = new object(); // Used only to synchronize access to maxConcurrentExecutions
//
//         // Act
//         // Launch 10 operations concurrently for the same key
//         var tasks = Enumerable.Range(0, 10).Select(i =>
//             i.SelectManySequential(
//                 action: () => Observable.FromAsync(async () =>
//                 {
//                     // 1. Track entry (Thread-safe)
//                     Interlocked.Increment(ref concurrentExecutions);
//
//                     lock (lockObject)
//                     {
//                         // 2. Record the maximum concurrency observed so far
//                         maxConcurrentExecutions = Math.Max(maxConcurrentExecutions, concurrentExecutions);
//                     }
//
//                     await Task.Delay(10); // Simulate work
//
//                     // 3. Track exit (Thread-safe)
//                     Interlocked.Decrement(ref concurrentExecutions);
//                     return i;
//                 }),
//                 keySelector: _ => key,
//                 queues: queues
//             ).ToTask()
//         ).ToList();
//
//         await Task.WhenAll(tasks);
//
//         // Assert
//         maxConcurrentExecutions.Should().Be(1, because: "operations with the same key must execute sequentially.");
//     }
//
//     // Test 2: Parallel Execution (Different Keys, Same Dictionary)
//     // Verifies that operations with different keys can run simultaneously.
//     [Fact]
//     public async Task DifferentKeys_ShouldExecuteInParallel()
//     {
//         // Arrange
//         var queues = new QueueDict<string>();
//
//         // Use a Barrier to prove parallelism. Both tasks must reach the barrier before either can proceed.
//         // If they ran sequentially, this would deadlock.
//         var barrier = new Barrier(2);
//
//         // Act
//         // Task 1 (Key1)
//         var t1 = 1.SelectManySequential(
//             action: () => Observable.FromAsync(() =>
//             {
//                 barrier.SignalAndWait(); // Signal arrival and wait for T2
//                 return Task.FromResult(1);
//             }),
//             keySelector: _ => "Key1",
//             queues: queues
//         ).ToTask();
//
//         // Task 2 (Key2)
//         var t2 = 2.SelectManySequential(
//              action: () => Observable.FromAsync(() =>
//             {
//                 barrier.SignalAndWait(); // Signal arrival and wait for T1
//                 return Task.FromResult(2);
//             }),
//             keySelector: _ => "Key2",
//             queues: queues
//         ).ToTask();
//
//         // Assert
//         await AssertParallelExecution(t1, t2, "tasks with different keys should run in parallel.");
//     }
//
//     // Test 3: Error Resilience (Queue does not die)
//     // Verifies that if an operation fails, the lock is released and subsequent operations proceed.
//     // This tests the critical fix implemented in the new version.
//     [Fact]
//     public async Task Failure_ShouldNotBlockSubsequentTasks()
//     {
//         // Arrange
//         var queues = new QueueDict<string>();
//         var key = "A";
//         bool task2Executed = false;
//
//         // Act
//         // 1. Enqueue a failing task (don't await yet)
//         var failingTask = 1.SelectManySequential(
//             action: () => Observable.Throw<int>(new InvalidOperationException("Boom!")),
//             keySelector: _ => key,
//             queues: queues
//         ).ToTask();
//
//         // 2. Enqueue a succeeding task immediately after
//         var succeedingTask = 2.SelectManySequential(
//             action: () =>
//             {
//                 task2Executed = true;
//                 return Observable.Return(42);
//             },
//             keySelector: _ => key,
//             queues: queues
//         ).ToTask();
//
//         // Assert
//         // Verify the first task failed as expected
//         await FluentActions.Awaiting(() => failingTask).Should().ThrowAsync<InvalidOperationException>();
//
//         // Verify the second task succeeded, proving the queue survived the error
//         var result = await succeedingTask;
//         result.Should().Be(42);
//         task2Executed.Should().BeTrue();
//     }
//
//     // Test 4: Context Isolation (Different Dictionaries)
//     // Verifies that synchronization contexts are isolated based on the dictionary instance
//     // provided (testing the ConditionalWeakTable behavior).
//     [Fact]
//     public async Task DifferentDictionaries_ShouldExecuteInParallel_EvenWithSameKey()
//     {
//         // Arrange
//         // Two separate dictionaries should mean two separate synchronization contexts.
//         var queues1 = new QueueDict<string>();
//         var queues2 = new QueueDict<string>();
//         var key = "A"; // Same key used in both contexts
//         var barrier = new Barrier(2);
//
//         // Act
//         // Task 1 (using queues1)
//         var t1 = 1.SelectManySequential(
//             action: () => Observable.FromAsync(() =>
//             {
//                 barrier.SignalAndWait();
//                 return Task.FromResult(1);
//             }),
//             keySelector: _ => key,
//             queues: queues1
//         ).ToTask();
//
//         // Task 2 (using queues2)
//         var t2 = 2.SelectManySequential(
//              action: () => Observable.FromAsync(() =>
//             {
//                 barrier.SignalAndWait();
//                 return Task.FromResult(2);
//             }),
//             keySelector: _ => key,
//             queues: queues2 // Different dictionary instance
//         ).ToTask();
//
//         // Assert
//         await AssertParallelExecution(t1, t2, "tasks using different dictionary instances should run in parallel.");
//     }
//
//     // Test 5: Execution Order
//     // Verifies that tasks execute strictly in the order they were enqueued, regardless of duration.
//     [Fact]
//     public async Task ShouldExecuteInEnqueueOrder()
//     {
//         // Arrange
//         var queues = new QueueDict<int>();
//         var key = 1;
//         // Use a thread-safe collection if adding from potentially parallel contexts, 
//         // though here they should be sequential.
//         var executionOrder = new ConcurrentBag<int>();
//
//         // Act
//         // Task 1 (Slow)
//         var task1 = 100.SelectManySequential(
//             action: () => Observable.FromAsync(async () => {
//                 await Task.Delay(100);
//                 executionOrder.Add(100);
//                 return 100;
//             }),
//             keySelector: _ => key,
//             queues: queues
//         ).ToTask();
//
//         // Task 2 (Medium)
//         var task2 = 200.SelectManySequential(
//             action: () => Observable.FromAsync(async () => {
//                 await Task.Delay(50);
//                 executionOrder.Add(200);
//                 return 200;
//             }),
//             keySelector: _ => key,
//             queues: queues
//         ).ToTask();
//
//          // Task 3 (Fast)
//         var task3 = 300.SelectManySequential(
//             action: () => Observable.FromAsync(async () => {
//                 await Task.Delay(10);
//                 executionOrder.Add(300);
//                 return 300;
//             }),
//             keySelector: _ => key,
//             queues: queues
//         ).ToTask();
//
//         await Task.WhenAll(task1, task2, task3);
//
//         // Assert
//         // Regardless of their execution time, the order must match the enqueue order.
//         // Convert ConcurrentBag to List to check order.
//         executionOrder.ToList().Should().ContainInOrder(100, 200, 300);
//     }
//
//     // Helper to detect deadlocks when testing parallelism
//     private async Task AssertParallelExecution(Task t1, Task t2, string because)
//     {
//         var timeout = Task.Delay(5000); // 5 second timeout
//         var completedTask = await Task.WhenAny(Task.WhenAll(t1, t2), timeout);
//
//         completedTask.Should().NotBe(timeout, because: because + " (Deadlock detected).");
//     }
// }