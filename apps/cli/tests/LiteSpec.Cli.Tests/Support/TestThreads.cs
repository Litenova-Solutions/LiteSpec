using System.Runtime.CompilerServices;

namespace LiteSpec.Cli.Tests.Support;

/// <summary>
///     Sizes the thread pool for the suite. Commands run in-process, many at once, and each
///     check fans out over the pool and waits on its parts. With the default minimum the pool
///     adds threads about twice a second, so concurrent commands queue behind each other's
///     waits; a higher minimum starts the threads they need at once.
/// </summary>
static class TestThreads
{
    /// <summary>Raises the thread pool minimum before the first test runs.</summary>
    [ModuleInitializer]
    internal static void Size()
    {
        ThreadPool.GetMinThreads(out var workers, out var completion);
        var wanted = Math.Max(workers, Environment.ProcessorCount * 8);
        ThreadPool.SetMinThreads(wanted, completion);
    }
}
