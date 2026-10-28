namespace LiteSpec.Tests.Model.Marks;

/// <summary>
///     Proves the marked-act findings against the act fixture, where two use cases
///     share one marked method.
/// </summary>
public sealed class ActCheckTests
{
    [Fact]
    public void Handler_Calling_No_Marked_Method_Reports_Use_Case_Act_Uncalled()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-uncalled");

        var finding = Assert.Single(uncalled);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("use-case/lending.renew-loan", finding.Message);
    }

    [Fact]
    public void Handler_Calling_The_Shared_Method_Passes()
    {
        using var fixture = MarksFixture.Prepare("act");
        var uncalled = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "use-case-act-uncalled");

        Assert.DoesNotContain(uncalled, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    [Fact]
    public void Shared_Method_Raising_An_Unlisted_Event_Reports_Act_Event_Undeclared()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        var finding = Assert.Single(undeclared);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("Loan.Borrow", finding.Message);
        Assert.Contains("event/loan.loan-borrowed", finding.Message);
    }

    [Fact]
    public void Use_Case_Sharing_The_Method_Reports_No_Undeclared_For_Its_Listed_Event()
    {
        using var fixture = MarksFixture.Prepare("act");
        var undeclared = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-undeclared");

        Assert.DoesNotContain(undeclared, finding => finding.Subject == "use-case/lending.borrow-copy");
    }

    [Fact]
    public void Listed_Event_The_Shared_Method_Never_Raises_Reports_Act_Event_Unraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unraised");

        var finding = Assert.Single(unraised);
        Assert.Equal("use-case/lending.renew-loan", finding.Subject);
        Assert.Contains("event/loan.loan-renewed", finding.Message);
    }

    [Fact]
    public void Use_Case_Whose_Shared_Method_Raises_Its_Event_Reports_No_Unraised()
    {
        using var fixture = MarksFixture.Prepare("act");
        var unraised = MarksFixture.WithCode(MarksFixture.Check(fixture.Path), "act-event-unraised");

        Assert.DoesNotContain(unraised, finding => finding.Subject == "use-case/lending.borrow-copy");
    }
}
