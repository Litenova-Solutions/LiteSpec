using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the store and aggregate shape checks: a store names its owning module or external system, an aggregate declares its lifecycle states, and a command changing only stores calls no marked aggregate act.</summary>
public sealed class StoreCheckTests
{
    /// <summary>Store Without Owner Reports Record Invalid.</summary>
    /// <proves>litespec/core/domain-model#name-the-owner-of-every-store</proves>
    [Fact]
    public void StoreWithoutOwnerReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds."}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Store Naming Its Owning Module Passes.</summary>
    /// <proves>litespec/core/domain-model#name-the-owner-of-every-store</proves>
    [Fact]
    public void StoreNamingItsOwningModulePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds.", "owner": "module/lending"}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Store Owner Naming An Aggregate Reports Field Invalid.</summary>
    /// <proves>litespec/core/domain-model#name-the-owner-of-every-store</proves>
    [Fact]
    public void StoreOwnerNamingAnAggregateReportsFieldInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds.", "owner": "aggregate/lending.loan"}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is open."}]}"""));

        Assert.Contains("field-invalid", TestModel.Codes(model));
    }

    /// <summary>Aggregate Without States Reports Record Invalid.</summary>
    /// <proves>litespec/core/domain-model#model-each-aggregate-lifecycle-with-states</proves>
    [Fact]
    public void AggregateWithoutStatesReportsRecordInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"summary": "A loan lends one copy to one reader."}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Aggregate With Named States Passes.</summary>
    /// <proves>litespec/core/domain-model#model-each-aggregate-lifecycle-with-states</proves>
    [Fact]
    public void AggregateWithNamedStatesPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is open."}]}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>Aggregate With Empty States Reports Field Invalid.</summary>
    /// <proves>litespec/core/domain-model#model-each-aggregate-lifecycle-with-states</proves>
    [Fact]
    public void AggregateWithEmptyStatesReportsFieldInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": []}"""));

        Assert.Contains("field-invalid", TestModel.Codes(model));
    }

    /// <summary>Store Only Command Needs No Marked Act.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void StoreOnlyCommandNeedsNoMarkedAct()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.log-delivery", Path.Combine(temp.Path, "log-delivery.yaml"), """{"kind": "command", "writes": ["store/lending.delivery-log"]}"""),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds.", "owner": "module/lending"}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.log-delivery", "lending", "log-delivery", "LogDelivery", "command",
            Path.Combine(temp.Path, "LogDeliveryHandler.cs")));

        Assert.DoesNotContain("use-case-act-missing", TestModel.Codes(model));
    }

    /// <summary>Command Writing An Aggregate And A Store Still Needs Its Act.</summary>
    /// <proves>litespec/backend/domain#change-each-aggregate-through-its-marked-act</proves>
    [Fact]
    public void CommandWritingAnAggregateAndAStoreStillNeedsItsAct()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow-copy.yaml"), """{"kind": "command", "writes": ["aggregate/lending.loan", "store/lending.delivery-log"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(temp.Path, "loan.yaml"), """{"states": [{"name": "Active", "meaning": "The loan is open."}]}"""),
            TestModel.Record("store", "store/lending.delivery-log", Path.Combine(temp.Path, "delivery-log.yaml"), """{"summary": "The log of delivered holds.", "owner": "module/lending"}"""));
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(temp.Path, "BorrowCopyHandler.cs")));

        Assert.Contains("use-case-act-missing", TestModel.Codes(model));
    }
}
