using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace LiteSpec.Cli.Model;

/// <summary>
///     Runs model work on threads with an explicit stack size. The readers and checks walk syntax trees, and the
///     compiler's walkers stop with an insufficient-stack failure when a thread's stack runs low. Thread-pool threads
///     take the platform's default size, which is small on musl and in native builds, so the CLI never relies on it.
/// </summary>
/// <follows>litespec/core/model#mark-every-check-with-its-level</follows>
static class WideStack
{
    /// <summary>The stack size of every model thread: 16 MiB, far above what the deepest walk needs.</summary>
    const int StackSize = 16 * 1024 * 1024;

    /// <summary>Starts work on its own thread and returns a handle that yields its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="work">The work to run.</param>
    /// <returns>A function that waits for the work and returns its result, rethrowing its failure.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The failure is captured on the worker thread and rethrown unchanged on the thread that waits for it.")]
    public static Func<T> Start<T>(Func<T> work)
    {
        T result = default!;
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    result = work();
                }
                catch (Exception exception)
                {
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
            },
            StackSize) { IsBackground = true };
        thread.Start();

        return () =>
        {
            thread.Join();
            failure?.Throw();
            return result;
        };
    }

    /// <summary>Runs one action per index on a fixed set of threads, each taking the next index as it frees.</summary>
    /// <param name="count">The number of indexes, from 0.</param>
    /// <param name="action">The action to run for each index.</param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The failure is captured on the worker thread and rethrown unchanged on the thread that waits for it.")]
    public static void ForEach(int count, Action<int> action)
    {
        var next = -1;
        ExceptionDispatchInfo? failure = null;
        var threads = Enumerable.Range(0, Math.Max(1, Math.Min(Environment.ProcessorCount, count)))
            .Select(_ => new Thread(
                () =>
                {
                    int index;
                    while (failure is null && (index = Interlocked.Increment(ref next)) < count)
                    {
                        try
                        {
                            action(index);
                        }
                        catch (Exception exception)
                        {
                            Interlocked.CompareExchange(ref failure, ExceptionDispatchInfo.Capture(exception), null);
                        }
                    }
                },
                StackSize) { IsBackground = true })
            .ToList();

        threads.ForEach(thread => thread.Start());
        threads.ForEach(thread => thread.Join());
        failure?.Throw();
    }
}
