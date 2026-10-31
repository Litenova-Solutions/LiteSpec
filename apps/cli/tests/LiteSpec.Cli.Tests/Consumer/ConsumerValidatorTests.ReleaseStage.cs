using System.Text.Json.Nodes;

namespace LiteSpec.Tests.Consumer;

public sealed partial class ConsumerValidatorTests
{
    static JsonObject StageSelection(Action<JsonObject>? extend = null)
    {
        var selection = new JsonObject
        {
            ["id"] = "stage",
            ["criterion"] = "No deployment holds customer data.",
            ["reviewBy"] = "2999-01-01",
        };
        extend?.Invoke(selection);
        return selection;
    }

    /// <summary>
    ///     Writes a departure exception deferring the provision until the date: the
    ///     record shape a pre-production consumer defers with. Every sentence stays
    ///     inside the prose measures, so the cases prove the checks.
    /// </summary>
    static void WriteDeferral(string root, string provision, string expiresOn = "2999-01-01") =>
        ConsumerFixture.WriteFile(root, "docs/governance/exceptions/stage-deferral.md", ConsumerFixture.Page(
            ConsumerFixture.Meta(
                "{\"type\":\"exception\",\"id\":\"exception/stage-deferral\",\"title\":\"Stage Deferral\",\"owner\":\"fixture\",\"status\":\"approved\","
                + "\"approvedBy\":\"fixture\",\"approvedOn\":\"2026-10-01\",\"approvedHash\":\"00000000\",\"kind\":\"departure\","
                + $"\"covers\":\"{provision}\",\"subject\":[\"use-case/orders.cancel-order\"],"
                + $"\"reason\":\"No deployment holds customer data yet.\",\"expiresOn\":\"{expiresOn}\"}}"),
            """
            # Stage Deferral

            ## Consequences

            The shop defers the provision until promotion.

            """));

    /// <proves>litespec/ext/stage#declare-the-release-stage</proves>
    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "stage selection with its release stage and a live deferral")]
    public void StageSelectionWithItsReleaseStageAndALiveDeferral()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection(selection =>
            {
                selection["releaseStage"] = "pre-production";
            }));
        });
        WriteDeferral(folder.Path, "litespec/core/scope#select-a-size-a-shape-and-stacks");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <proves>litespec/ext/stage#declare-the-release-stage</proves>
    [Fact(DisplayName = "stage selection without a release stage")]
    public void StageSelectionWithoutAReleaseStage()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection());
        });
        AssertProblem(RunConsumer(folder.Path), "releaseStage");
    }

    /// <proves>litespec/ext/stage#declare-the-release-stage</proves>
    [Fact(DisplayName = "stage selection with an unknown release stage")]
    public void StageSelectionWithAnUnknownReleaseStage()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection(selection =>
            {
                selection["releaseStage"] = "beta";
            }));
        });
        AssertProblem(RunConsumer(folder.Path), "/releaseStage");
    }

    /// <proves>litespec/ext/stage#declare-the-release-stage</proves>
    [Fact(DisplayName = "release stage on another extension")]
    public void ReleaseStageOnAnotherExtension()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(
                new JsonObject
                {
                    ["id"] = ConsumerFixture.KnownExtension,
                    ["criterion"] = "The suite needs one.",
                    ["reviewBy"] = "2999-01-01",
                    ["releaseStage"] = "production",
                });
        });
        AssertProblem(RunConsumer(folder.Path), "/id");
    }

    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "production stage that still defers")]
    public void ProductionStageThatStillDefers()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection(selection =>
            {
                selection["releaseStage"] = "production";
            }));
        });
        WriteDeferral(folder.Path, "litespec/core/scope#select-a-size-a-shape-and-stacks");
        AssertProblem(RunConsumer(folder.Path), "declares production and still defers");
    }

    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "deferral of a provision the standards do not hold")]
    public void DeferralOfAProvisionTheStandardsDoNotHold()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection(selection =>
            {
                selection["releaseStage"] = "pre-production";
            }));
        });
        WriteDeferral(folder.Path, "litespec/core/scope#no-such-provision");
        AssertProblem(RunConsumer(folder.Path), "is not a provision in the pinned LiteSpec release");
    }

    /// <proves>litespec/ext/stage#defer-named-provisions-until-promotion</proves>
    [Fact(DisplayName = "deferral past its end date stays silent here")]
    public void DeferralPastItsEndDateStaysSilentHere()
    {
        // Expiry belongs to the model check, which reports exception-expired:
        // the consumer holds the deferral's shape and provision only.
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(StageSelection(selection =>
            {
                selection["releaseStage"] = "pre-production";
            }));
        });
        WriteDeferral(folder.Path, "litespec/core/scope#select-a-size-a-shape-and-stacks", "2020-01-01");
        AssertClean(RunConsumer(folder.Path));
    }
}
