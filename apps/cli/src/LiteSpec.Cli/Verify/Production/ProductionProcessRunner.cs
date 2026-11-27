using LiteSpec.Cli.Model;

namespace LiteSpec.Cli.Verify.Production;

/// <summary>The production process runner that starts real external programs through the shared process runner.</summary>
/// <follows>litespec/core/release#boot-the-production-configuration-before-release</follows>
sealed class ProductionProcessRunner : IProductionProcessRunner
{
    /// <summary>Runs one program without a shell, allowing ten minutes for a compose build.</summary>
    /// <param name="file">The program to start, found on the PATH when it is not a full path.</param>
    /// <param name="args">The arguments, passed one by one without a shell.</param>
    /// <param name="workdir">The folder the program runs in.</param>
    /// <returns>The exit status with the error text.</returns>
    public ProductionProcessResult Run(string file, IReadOnlyList<string> args, string workdir)
    {
        var captured = ProcessRunner.Capture(file, args, workdir, TimeSpan.FromMinutes(10));
        return new ProductionProcessResult(captured.Status, captured.Error);
    }
}
