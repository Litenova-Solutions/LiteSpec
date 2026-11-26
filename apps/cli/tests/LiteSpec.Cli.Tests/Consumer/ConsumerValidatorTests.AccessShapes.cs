using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>Consumer validator cases for the allowed shapes of a use case access block.</summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>The fixture use case carrying each access shape under test.</summary>
    const string AccessUseCasePath = ".litespec/behavior/modules/orders/aggregates/order-claims/use-cases/cancel-order/cancel-order.yaml";

    /// <summary>The access block of the fixture use case as written.</summary>
    const string BaseAccess = """"access":{"grant":"permission/cancel-order","tenant":"own"}"""";

    /// <summary>The input array of the fixture use case as written.</summary>
    const string BaseInput = """"input":[{"name":"orderId","meaning":"The order the customer cancels.","type":"id","identifies":"aggregate/orders.order-claim","required":true,"origin":"caller","classification":"internal"}]"""";

    /// <summary>The fixture use case as written, with its access block and input array replaced per case.</summary>
    const string BaseUseCase = """{"type":"use-case","id":"use-case/orders.cancel-order","title":"Cancel order","kind":"command","summary":"A customer cancels an order claim before shipment.","access":{"grant":"permission/cancel-order","tenant":"own"},"reachedThrough":["entry-point/web"],"input":[{"name":"orderId","meaning":"The order the customer cancels.","type":"id","identifies":"aggregate/orders.order-claim","required":true,"origin":"caller","classification":"internal"}],"writes":["aggregate/orders.order-claim"],"idempotency":{"mode":"key","key":"{orderId}","keyScope":"tenant","replay":"returns-first-result"},"audit":{"level":"required","category":"monetary-action","targetKind":"order claim"},"raises":["domain-event/order-claim.order-canceled"],"fulfills":["requirement/pricing.flat-fee"],"steps":[{"statement":"The customer sends the order identifier.","cites":["aggregate/orders.order-claim"]},{"statement":"Orders checks the cancel rule against the shipment.","cites":["rule/order-claim.cancel-before-shipment-only"]},{"statement":"Orders refuses the cancel with `orders.cancel-refused` when the shipment already left.","cites":["result/cancel-order.cancel-refused","rule/order-claim.cancel-before-shipment-only"]},{"statement":"Orders cancels the claim and raises the canceled event.","cites":["result/cancel-order.order-canceled","domain-event/order-claim.order-canceled","aggregate/orders.order-claim"]}],"records":[{"type":"result","id":"result/cancel-order.order-canceled","title":"Order canceled","outcome":"success","meaning":"The order claim was canceled."},{"type":"result","id":"result/cancel-order.cancel-refused","title":"Cancel refused","outcome":"refusal","refusal":"refusal/orders.cancel-refused","sentence":"The shipment already left, so the cancel is refused."}]}""";

    /// <summary>Use case access carrying a duration window, a self scope with its identity, and a cited precondition.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "access carrying a window, a self scope, and a precondition")]
    public void AccessCarryingWindowScopeAndPrecondition()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(
                """"access":{"grant":"permission/cancel-order","tenant":"own","recentSignIn":"PT15M","scope":"self","identityInput":"customer","preconditions":["rule/order-claim.cancel-before-shipment-only"]}"""",
                """"input":[{"name":"orderId","meaning":"The order the customer cancels.","type":"id","identifies":"aggregate/orders.order-claim","required":true,"origin":"caller","classification":"internal"},{"name":"customer","meaning":"The customer canceling the order.","type":"id","identifies":"actor/customer","required":false,"origin":"verified-claim","verifiedBy":"entry-point/web","classification":"personal"}]""""));
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Access carrying a self scope without its identity input.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "access carrying a self scope without its identity")]
    public void AccessCarryingScopeWithoutIdentity()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(""""access":{"grant":"permission/cancel-order","tenant":"own","scope":"self"}""""));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'identityInput'");
    }

    /// <summary>Access carrying an identity input without its self scope.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "access carrying an identity without its self scope")]
    public void AccessCarryingIdentityWithoutScope()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(""""access":{"grant":"permission/cancel-order","tenant":"own","identityInput":"customer"}""""));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'scope'");
    }

    /// <summary>Access carrying a scope outside self.</summary>
    /// <proves>litespec/core/access#name-the-caller-identity-of-a-self-scope</proves>
    [Fact(DisplayName = "access carrying a scope outside self")]
    public void AccessCarryingScopeOutsideSelf()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(""""access":{"grant":"permission/cancel-order","tenant":"own","scope":"tenant","identityInput":"customer"}""""));
        AssertProblem(RunConsumer(folder.Path), "access/scope");
    }

    /// <summary>Access carrying a window that is neither a duration nor a setting.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "access carrying a window outside durations and settings")]
    public void AccessCarryingWindowOutsideDurationsAndSettings()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(""""access":{"grant":"permission/cancel-order","tenant":"own","recentSignIn":"fifteen minutes"}""""));
        AssertProblem(RunConsumer(folder.Path), "access/recentSignIn");
    }

    /// <summary>Access carrying the retired window field.</summary>
    /// <proves>litespec/core/access#limit-the-age-of-a-sign-in</proves>
    [Fact(DisplayName = "access carrying the retired window field")]
    public void AccessCarryingTheRetiredWindowField()
    {
        using var folder = ConsumerFixture.Create();
        TestRecords.Write(
            folder.Path,
            AccessUseCasePath,
            UseCaseWith(""""access":{"grant":"permission/cancel-order","tenant":"own","recentSignInLimit":"setting/orders.sign-in-window"}""""));
        AssertProblem(RunConsumer(folder.Path), "unknown property 'recentSignInLimit'");
    }

    /// <summary>The fixture use case with its access block replaced, and its input array replaced when one is given.</summary>
    /// <param name="access">The access block JSON with its property name.</param>
    /// <param name="input">The input array JSON with its property name, or null to keep the fixture input.</param>
    /// <returns>The use case fields JSON carrying the access block.</returns>
    static string UseCaseWith(string access, string? input = null)
    {
        var fields = BaseUseCase.Replace(BaseAccess, access, StringComparison.Ordinal);

        return input is null ? fields : fields.Replace(BaseInput, input, StringComparison.Ordinal);
    }
}
