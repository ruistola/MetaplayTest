// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core.Tasks;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Cloud.Tests
{
    class TaskQueueExecutorTest
    {
        [Test]
        public async Task TestEnqueueActionWithTimers()
        {
            for (int schedulerI = 0; schedulerI < 2; schedulerI++)
            {
                for (int delayPosI = 0; delayPosI < 6; delayPosI++)
                {
                    TaskQueueExecutor<int, int, int, int> executor = new TaskQueueExecutor<int, int, int, int>(schedulerI == 0 ? TaskScheduler.Default : TaskScheduler.Current);

                    int target = 0;
                    int expected = 5;

                    if (delayPosI == 1) await Task.Delay(10);
                    executor.EnqueueAsync(() => { if (target == 0) target = 1; });

                    if (delayPosI == 2) await Task.Delay(10);
                    executor.EnqueueAsync(1, arg1 => { if (target == arg1) target = 2; });

                    if (delayPosI == 3) await Task.Delay(10);
                    executor.EnqueueAsync(2, 3, (arg1,  arg2) => { if (target == arg1) target = arg2; });

                    if (delayPosI == 4) await Task.Delay(10);
                    executor.EnqueueAsync(3, 6, 2, (arg1,  arg2,  arg3) =>
                    {
                        if (target == arg1)
                            target = arg2 - arg3;
                    });
                    if (delayPosI == 5) await Task.Delay(10);
                    executor.EnqueueAsync(4, 10, 2, 3, (arg1,  arg2,  arg3, arg4) =>
                    {
                        if (target == arg1)
                            target = arg2 - arg3 - arg4;
                    });

                    if (delayPosI == 6)
                    {
                        executor.EnqueueAsync(async () =>
                        {
                            await Task.Delay(10);
                            if (target == 5)
                                target = 6;
                        });
                        expected = 6;
                    }

                    TaskCompletionSource tcs = new TaskCompletionSource();
                    executor.EnqueueAsync(() => tcs.SetResult());

                    await tcs.Task;

                    Assert.AreEqual(expected, target, "With params: {0} -- delay {1}", schedulerI, delayPosI);
                }
            }
        }

        [Test]
        public async Task TestEnqueueActionWithCompletions()
        {
            for (int schedulerI = 0; schedulerI < 2; schedulerI++)
            {
                for (int delayPosI = 0; delayPosI < 6; delayPosI++)
                {
                    TaskQueueExecutor<int, int, int, int> executor = new TaskQueueExecutor<int, int, int, int>(schedulerI == 0 ? TaskScheduler.Default : TaskScheduler.Current);
                    TaskCompletionSource                  delayTcs = new TaskCompletionSource();

                    int target = 0;
                    int expected = 5;

                    if (delayPosI == 1)
                        await Task.Delay(10);

                    executor.EnqueueAsync(() =>
                    {
                        if (target == 0)
                            target = 1;
                        if (delayPosI == 2)
                            delayTcs.SetResult();
                    });
                    if (delayPosI == 2)
                        await delayTcs.Task;

                    executor.EnqueueAsync(1, (arg1) =>
                    {
                        if (target == arg1)
                            target = 2;
                        if (delayPosI == 3)
                            delayTcs.SetResult();
                    });
                    if (delayPosI == 3)
                        await delayTcs.Task;

                    executor.EnqueueAsync(2, 3, (arg1, arg2) =>
                    {
                        if (target == arg1)
                            target = arg2;
                        if (delayPosI == 4)
                            delayTcs.SetResult();
                    });
                    if (delayPosI == 4)
                        await delayTcs.Task;

                    executor.EnqueueAsync(3, 6, 2, (arg1, arg2, arg3) =>
                    {
                        if (target == arg1)
                            target = arg2 - arg3;
                        if (delayPosI == 5)
                            delayTcs.SetResult();
                    });
                    if (delayPosI == 5)
                        await delayTcs.Task;

                    executor.EnqueueAsync(4, 10, 2, 3, (arg1, arg2, arg3, arg4) =>
                    {
                        if (target == arg1)
                            target = arg2 - arg3 - arg4;
                    });

                    if (delayPosI == 6)
                    {
                        executor.EnqueueAsync(async () =>
                        {
                            await delayTcs.Task;
                            if (target == 5)
                                target = 6;
                        });
                        expected = 6;
                    }

                    TaskCompletionSource completionTcs = new TaskCompletionSource();
                    executor.EnqueueAsync(() => completionTcs.SetResult());

                    if (delayPosI == 6)
                        delayTcs.SetResult();

                    await completionTcs.Task;

                    Assert.AreEqual(expected, target, "With params: {0} -- delay {1}", schedulerI, delayPosI);
                }
            }
        }
    }
}
