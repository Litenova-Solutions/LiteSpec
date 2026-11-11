using LiteSpec.Cli.Model.Configuration;

namespace LiteSpec.Cli.Model.Readers;

/// <summary>Reads the acceptance scenarios and what they claim.</summary>
interface IScenarioReader
{
    /// <summary>Reads scenarios with their claimed cases, steps and asserted codes, and file comments.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <returns>The scenario read: the scenarios and the files it could not read.</returns>
    ScenarioRead Read(ConsumerConfig config);
}
