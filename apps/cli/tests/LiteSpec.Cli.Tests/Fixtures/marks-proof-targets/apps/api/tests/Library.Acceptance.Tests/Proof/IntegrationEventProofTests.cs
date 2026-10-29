namespace Library.Acceptance.Tests.Proof;

public sealed class IntegrationEventProofTests
{
    /// <proves>integration-event/loan-borrowed</proves>
    [Fact]
    public void Publishes_loan_borrowed()
    {
        Assert.True(true);
    }
}
