using LiteSpec.Cli.Verify.Production;

namespace LiteSpec.Cli.Tests.Verify.Production;

/// <summary>A test double for the container commands: records every command line and answers success unless a handler says otherwise.</summary>
sealed class FakeProductionProcessRunner : IProductionProcessRunner
{
    /// <summary>Gets the recorded command lines in run order.</summary>
    public List<string> Commands { get; } = [];

    /// <summary>Gets or sets the handler that answers one command line, or null to answer success.</summary>
    public Func<string, ProductionProcessResult>? Handler { get; set; }

    /// <summary>Records the command line and answers it.</summary>
    /// <param name="file">The program to start.</param>
    /// <param name="args">The arguments.</param>
    /// <param name="workdir">The folder the program runs in.</param>
    /// <returns>The handler answer, or success.</returns>
    public ProductionProcessResult Run(string file, IReadOnlyList<string> args, string workdir)
    {
        var line = file + " " + string.Join(" ", args);
        Commands.Add(line);
        return Handler is not null ? Handler(line) : new ProductionProcessResult(0, string.Empty);
    }
}
