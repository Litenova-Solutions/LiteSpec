using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Tests;

namespace LiteSpec.Cli.Model.Readers;

/// <summary>Reads test outcomes by test name and by fully qualified test name.</summary>
interface ITestRunReader
{
    /// <summary>Reads the run, or null when no result file exists.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The test run, or null when no result file exists.</returns>
    TestRun? Read(ConsumerConfig config);
}
