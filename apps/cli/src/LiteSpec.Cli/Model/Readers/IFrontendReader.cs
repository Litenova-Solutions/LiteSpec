using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Screens;

namespace LiteSpec.Cli.Model.Readers;

/// <summary>Reads the routes of one frontend.</summary>
interface IFrontendReader
{
    /// <summary>Reads the frontend's route screens and file comments.</summary>
    /// <param name="config">The consumer configuration that locates the roots and files to read.</param>
    /// <param name="code">The code model the screens are checked against.</param>
    /// <returns>The screen read of the frontend.</returns>
    ScreenRead Read(ConsumerConfig config, CodeFacts code);
}
