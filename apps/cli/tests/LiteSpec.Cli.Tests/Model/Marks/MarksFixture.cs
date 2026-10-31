using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Copies a marks fixture consumer and checks its model in process.</summary>
static class MarksFixture
{
    /// <summary>Copies <c>Fixtures/marks-&lt;name&gt;</c> to a fresh temp folder; the caller deletes it.</summary>
    /// <param name="name">The fixture after <c>marks-</c>: <c>clean</c>, <c>missing</c>, and the rest.</param>
    /// <returns>The temp folder holding the prepared fixture.</returns>
    public static TempFolder Prepare(string name)
    {
        var temp = Cli.Temp("marks");

        try
        {
            CopyContents(Cli.Fixture($"marks-{name}"), temp.Path);
            return temp;
        }
        catch
        {
            temp.Dispose();
            throw;
        }
    }

    /// <summary>Builds the model for the prepared fixture and runs every check.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <returns>The reported findings.</returns>
    public static IReadOnlyList<Finding> Check(string root)
    {
        var config = ConsumerConfig.Load(root);
        var model = ModelBuilder.Build(config);

        return Checker.Check(model).Findings;
    }

    /// <summary>The findings carrying the code.</summary>
    /// <param name="findings">The findings to filter.</param>
    /// <param name="code">The finding code to keep.</param>
    /// <returns>The mark findings whose code matches, in the order reported.</returns>
    public static List<Finding> WithCode(IReadOnlyList<Finding> findings, string code) =>
        findings.Where(finding => finding.Code == code).ToList();

    static void CopyContents(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }
}
