using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for the stage selection and its deferral exceptions.</summary>
public sealed partial class ConsumerValidatorTests
{
    static JsonObject StageSelection() =>
        new()
        {
            ["id"] = "stage",
            ["criterion"] = "No deployment holds customer data.",
            ["releaseStage"] = "pre-production",
        };

    /// <summary>
    ///     Writes a departure exception deferring the provision until the date: the
    ///     record shape a pre-production consumer defers with. Every sentence stays
    ///     inside the prose measures, so the cases prove the checks.
    /// </summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="provision">The provision id the deferral covers.</param>
    /// <param name="expiresOn">The date the deferral expires, in ISO form.</param>
    static void WriteDeferral(string root, string provision, string expiresOn = "2999-01-01") =>
        TestRecords.Write(root, ".litespec/governance/exceptions/stage-deferral.yaml",
            "{\"type\":\"exception\",\"id\":\"exception/stage-deferral\",\"title\":\"Stage Deferral\","
            + "\"kind\":\"departure\","
            + $"\"covers\":\"{provision}\",\"subject\":[\"use-case/orders.cancel-order\"],"
            + "\"consequences\":\"The shop defers the provision until promotion.\","
            + $"\"reason\":\"No deployment holds customer data yet.\",\"expiresOn\":\"{expiresOn}\"}}");

    /// <summary>Stage selection with a live deferral.</summary>
    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "stage selection with a live deferral")]
    public void StageSelectionWithALiveDeferral()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection());
        });
        WriteDeferral(folder.Path, "litespec/core/scope#select-a-size-a-shape-and-stacks");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Deferral of a provision the standards do not hold.</summary>
    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "deferral of a provision the standards do not hold")]
    public void DeferralOfAProvisionTheStandardsDoNotHold()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection());
        });
        WriteDeferral(folder.Path, "litespec/core/scope#no-such-provision");
        AssertProblem(RunConsumer(folder.Path), "is not a provision in the pinned LiteSpec release");
    }

    /// <summary>Deferral past its end date stays silent here.</summary>
    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "deferral past its end date stays silent here")]
    public void DeferralPastItsEndDateStaysSilentHere()
    {
        // Expiry belongs to the model check, which reports exception-expired:
        // the consumer holds the deferral's shape and provision only.
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection());
        });
        WriteDeferral(folder.Path, "litespec/core/scope#select-a-size-a-shape-and-stacks", "2020-01-01");
        AssertClean(RunConsumer(folder.Path));
    }
}
