namespace LiteSpec.Tests.Consumer;

/// <summary>Consumer validator cases for review report pages and their verdicts.</summary>
public sealed partial class ConsumerValidatorTests
{
    const string ReviewReportPage = "docs/proof/reviews/2026-10-30-intent.md";

    const string ReviewReportMeta = """{"type":"review-report","title":"Intent Review","owner":"fixture","status":"approved","reviewedOn":"2026-10-30","reviewer":"agent","batches":["intent/pricing"]}""";

    /// <summary>Review report that carries its verdicts.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "review report that carries its verdicts")]
    public void ReviewReportThatCarriesItsVerdicts()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            ReviewReportPage,
            ConsumerFixture.Page(
                ConsumerFixture.Meta(ReviewReportMeta),
                """
                # Intent Review

                The second review covered the intent batch and approved every record in it.

                ## Verdicts

                | Batch | Records | Verdict |
                |:---|:---|:---|
                | `intent/pricing` | 1 | Approved: every record reads clean. |

                ## How the Review Ran

                The reviewer read the pricing page and its requirement, then checked each verdict.

                """));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Review report that omits its verdicts.</summary>
    /// <proves>litespec/core/authoring#use-the-declared-page-contract</proves>
    [Fact(DisplayName = "review report that omits its verdicts")]
    public void ReviewReportThatOmitsItsVerdicts()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(
            folder.Path,
            ReviewReportPage,
            ConsumerFixture.Page(
                ConsumerFixture.Meta(ReviewReportMeta),
                """
                # Intent Review

                The second review covered the intent batch and approved every record in it.

                ## How the Review Ran

                The reviewer read the pricing page and its requirement, then checked each verdict.

                """));
        AssertProblem(RunConsumer(folder.Path), "requires an H2 'Verdicts'");
    }
}
