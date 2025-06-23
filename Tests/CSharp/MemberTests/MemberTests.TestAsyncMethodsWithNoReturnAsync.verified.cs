using System;
using System.Threading.Tasks;

internal static partial class TaskExtensions
{
    public async static Task<T> Then<T>(this Task task, Func<Task<T>> f)
    {
        await task;
        return await f();
    }

    public async static Task Then(this Task task, Func<Task> f)
    {
        await task;
        await f();
    }

    public async static Task<U> Then<T, U>(this Task<T> task, Func<T, Task<U>> f)
    {
        return await f(await task);
    }

    public async static Task Then<T>(this Task<T> task, Func<T, Task> f)
    {
        await f(await task);
    }

    public async static Task ThenExit<T>(this Task<T> task, Func<T, Task> f)
    {
        await f(await task);
        return;
    }
}