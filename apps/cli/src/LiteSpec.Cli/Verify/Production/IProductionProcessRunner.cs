namespace LiteSpec.Cli.Verify.Production;

/// <summary>Runs the container commands of the production smoke: the compose up, the optional migrate, and the final down.</summary>
interface IProductionProcessRunner
{
    /// <summary>Runs one program without a shell in the consumer root.</summary>
    /// <param name="file">The program to start, found on the PATH when it is not a full path.</param>
    /// <param name="args">The arguments, passed one by one without a shell.</param>
    /// <param name="workdir">The folder the program runs in.</param>
    /// <returns>The exit status with the error text.</returns>
    ProductionProcessResult Run(string file, IReadOnlyList<string> args, string workdir);
}
