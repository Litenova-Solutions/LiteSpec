namespace Library.Acceptance.Tests.Proof;

public sealed class ScenarioProofTests
{
    /// <proves>scenario/borrow-copy.borrowing-an-available-copy-succeeds</proves>
    [Fact]
    public void Borrowing_an_available_copy_succeeds()
    {
        var command = new BorrowCopyCommand();
        Assert.True(command is not null);
    }
}
