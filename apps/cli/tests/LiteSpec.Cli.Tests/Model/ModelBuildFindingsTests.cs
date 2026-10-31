using System.Text.Json;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model;

/// <summary>Proves <c>litespec model build</c> writes its check result into the model it writes.</summary>
public sealed class ModelBuildFindingsTests
{
    /// <summary>The built model carries findings and known at its top level, in the shape the check writes.</summary>
    /// <proves>litespec/core/model#mark-every-check-with-its-level</proves>
    [Fact]
    public void Build_Writes_Findings_And_Known_Into_The_Model()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        var output = Path.Combine(fixture.Path, "built-model.json");
        var result = Cli.Run("model", "build", "--root", fixture.Path, "--out", output);
        Assert.Equal(0, result.Status);

        using var document = JsonDocument.Parse(File.ReadAllText(output));
        var findings = document.RootElement.GetProperty("findings");
        Assert.Equal(JsonValueKind.Array, findings.ValueKind);
        Assert.Contains(
            findings.EnumerateArray(),
            finding => finding.GetProperty("code").GetString() == "use-case-unimplemented");
        Assert.Equal(JsonValueKind.Array, document.RootElement.GetProperty("known").ValueKind);
    }
}
