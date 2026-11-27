# .NET Domain Layer

**Stack:** dotnet
**Concern:** domain

This page states how the Domain layer of a .NET backend looks in C#. It is for developers who place aggregates, value objects, and repositories in `Domain/` folders. A business rule in the wrong layer pulls framework concerns into behavior, so each type stays with the aggregate that owns it.

## Concepts

### What Lives in Domain

Domain folders hold the business model: aggregates with their state records, child entities, value objects, closed sets, money, identifiers, repository interfaces, domain events, and refusal exceptions. Handlers, queries, ports, adapters, and endpoints live in the layers that serve them, and Domain points at the base class library plus product-owned Domain types.

```csharp
// Modules/Claimwick.Spending/Domain/Expenses/
Expense.cs  IExpenseRepository.cs  Submit/  Parts/  Facts/  Events/  Exceptions/
```

The Expense aggregate shows the shape: the root file beside its repository interface, one folder per root action, and groupings for parts, facts, events, and exceptions. Application reaches the aggregate through that root and its repository, and through nothing else.

### Aggregates and State Records

An aggregate is one unit of business state that stays consistent and changes in one step. Its root is the single type through which outside code changes it, and every aggregate exposes its lifecycle through one `State` property.

```csharp
public abstract record PostState;

public sealed record PostDraftState : PostState;

public sealed record PostPublishedState(DateTimeOffset PublishedAt) : PostState;
```

The `State` property rests on an abstract state record with one sealed record per state. Each state record carries that state's own data, such as the publication time above, and the aggregate methods own the transitions between states. A profile with one current state still defines its hierarchy, so later states have a home.

The root is a sealed class on the project-owned `AggregateRoot<TId>` base. The base supplies typed identity with a pending event collection plus protected recording and clearing, and nothing else: no timestamps, audit, tenancy, lifecycle, or persistence. The aggregate version exists as persistence metadata, outside what Domain reads or sets, and the root carries a mark naming its aggregate record for the builder link.

The C# compiler does not check that a switch over the hierarchy covers every sealed case. A switch expression carries a discard arm to satisfy warning CS8509, and a sealed case left to that discard arm reaches it at run time. Tests cover the discard path for a case added later, and the sealed set matches the aggregate's declared `states`.

In Claimwick, an expense moves from submitted to approved when `Approve` runs. The method accepts a submitted expense, stores an approved state carrying the approver and the decided time, and raises the approval event.

Child entities live inside their aggregate boundary. Each child is a sealed class on the project-owned `Entity<TId>`, which gives it typed identity and identity equality, so the child declares its own data and behavior. The root creates, finds, and changes its children through business methods, with construction and mutation internal or private. A child is a design choice inside its aggregate: it appears in code and the glossary, not as a record.

### Closed Sets Without Enums

A closed set is a fixed group of values, such as refund outcomes or merchant roles. Domain models each one as an abstract record with one sealed case record per value, and each case exposes a stable string `Code`.

```csharp
public abstract record MerchantRole
{
    public abstract string Code { get; }

    public static MerchantRole FromCode(string code) => code switch
    {
        "owner" => new MerchantOwnerRole(),
        "warehouse-staff" => new MerchantWarehouseStaffRole(),
        _ => throw new MerchantUnsupportedRoleException(code),
    };
}
```

A boundary turns a label into a case through `FromCode`, and an unrecognized label becomes a validation failure. An enum carries no case data and admits undefined values through a cast, so a case that later gains data lands in its own sealed record instead of growing parallel fields beside the enum. A static class of string constants counts as the same shape without its cases, and a closed set that one scalar represents uses a typed value object instead of an enum backing field.

The same set appears at three other layers, and each layer has exactly one form. Domain holds the abstract record base with sealed cases. Application mirrors that union in its results. Transport uses a polymorphic `oneOf` model, or a string `enum` for a label-only set. Persistence keeps one stable string discriminator per case. A transport `enum` fits one situation: every case carries no data, and the labels form the complete contract. A set where any case carries data crosses the boundary as a polymorphic model instead, and a transport label turns back into its record case in Application before reaching a Domain method.

Callers branch on the case type with a `switch` expression. A case that carries data exposes it directly instead of through a separate nullable field. The discard arm sits after every sealed case has its own arm, handling a value outside the sealed set. Round-trip tests cover every case without data and catch a renamed code or a missing label.

### Identities, Money, and Repositories

Each aggregate identity is a small struct over one version 7 `Guid`. The caller creates the value and passes it in, and the struct turns away empty values in every factory. It implements `IStronglyTypedId` and `IParsable<TId>`, so Minimal API route and query binding parse it from text. `Parse` follows the .NET parsing contract for malformed text, while `From` applies the empty-identity rule.

One UUID text form travels every boundary: the typed identifier in Domain and Application, UUID text through `IParsable<TId>` on routes and queries, a UUID string in JSON through a WebApi converter, `type: string` with `format: uuid` in OpenAPI through a schema transformer, native UUID identity plus explicit typed-identifier mapping in Marten, and `string` from OpenAPI in generated TypeScript clients. One converter factory restricted to `IStronglyTypedId` implementations serves WebApi, and the OpenAPI transformer applies the same restriction.

An aggregate points at another aggregate through a property typed as that aggregate's typed identifier. A property typed as the other root or entity would load one aggregate's data through another, joining two consistency boundaries and their storage. Each aggregate stays loadable on its own.

Clocks, randomness, and fresh identifiers arrive as arguments. `DateTime`, `DateTimeOffset`, `TimeProvider`, `Random`, `RandomNumberGenerator`, `Guid.NewGuid`, and `Guid.CreateVersion7` return a fresh value on every call, so a Domain method that reads one runs differently on each run and resists fixed-value tests. Receiving each value as an argument keeps every Domain call a pure function of its inputs, and the Application layer obtains each value through the port it owns.

Money is one shared value object with a decimal amount and an ISO 4217 currency code. It serializes with a string amount, as `{ "amount": "12.50", "currency": "EUR" }`, so readers parse it as decimal text. The owning module record defines the supported currencies, scale, rounding, sign rules, and cross-currency arithmetic, and Infrastructure maps that precision explicitly. A use case fixed to one currency accepts an amount alone while stating that currency in its record. Rates applied to money and quantities compared for exact equality stay decimal as well, since binary floating point renders most decimal fractions approximately.

Each aggregate root gets one repository interface that loads and stages that aggregate. The interface exposes aggregate and Domain types for its root, with a throwing `GetByIdAsync` for a load that has to succeed and a nullable `FindBy...Async` for lookups where absence is normal. A flow that finds a record or creates it is two use cases, and the boundary owning the flow chooses between them. The repository is the write side: queries read through Application read models, and the command pipeline owns the commit.

### Value Objects and Domain Services

A value object is an immutable C# record with no base type. Comparison by complete value lets two instances with the same data stand in for each other. Creation runs through a named static method, so every construction path passes the validation the type declares, and the type exposes no setter and no implicit conversion.

```csharp
public sealed record PostTitle
{
    private PostTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PostTitle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new PostTitleRequiredException();
        }

        if (value.Length > 200)
        {
            throw new PostTitleExceedsMaximumLengthException(value.Length);
        }

        return new PostTitle(value);
    }

    public override string ToString() => Value;
}
```

A post title validates at creation: empty text and text past 200 characters each throw their own exception. A value object holding a collection compares by content with a matching hash for its declared ordering rule, copies incoming collections, and returns read-only views. When order is not part of the value, creation normalizes the values to the documented comparison order before storing them.

Some calculations span concepts with no natural aggregate owner, such as pricing an order from its lines. A stateless domain service holds that calculation with no persistence, messaging, logging, clock, authorization, or provider call. The service accepts and returns Domain values and carries a mark naming the rule it computes. The root calls the service inside its own method, or Application passes the service's plain result into a root method as an input.

```csharp
public sealed class OrderPricingDomainService
{
    public Money CalculateTotal(
        IReadOnlyList<OrderLine> lines,
        Currency currency)
    {
        return lines.Aggregate(
            Money.Zero(currency),
            (total, line) => total.Add(line.Subtotal));
    }
}
```

### Shared Contracts and File Shape

Every module raises events, throws domain exceptions, and identifies aggregates, so the shared kernel Domain folder declares those contracts a single time: `IDomainEvent`, `DomainException`, `IStronglyTypedId`, `Entity<TId>`, `IAggregateRoot`, and `AggregateRoot<TId>`. `DomainException` takes the message in its constructor and declares `Code` abstract, so each refusal code lands as an override in the partial exception. Generic handling constrains its parameters with `IAggregateRoot`. `ClearDomainEvents` is event-delivery mechanics, and Infrastructure calls it after the delivery path accepts the pending events.

A root grows with every business action it owns. A root past 500 lines becomes a partial class with one folder per business action, holding the partial file with the action's inputs and its private rules. In Claimwick, the expense root keeps its identity and state in `Expense.cs`, its submission factory and allocation rules in `Submit/Expense.Submit.cs`, and its approval in `Approve/Expense.Approve.cs`. The folder name is the use case act, so `Domain/Entries/PostInvoice/` mirrors `Application/Entries/PostInvoiceEntry/`, and the action rules stay private to the root.

## Standards

### Keep Domain Free of Outer-Layer Concerns

**ID:** `litespec/packs/dotnet/domain#keep-domain-free-of-outer-layer-concerns`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Domain code MUST reference only the .NET base class library and product-owned Domain types.

**Rationale:** Business behavior stays testable while it points at the base library and product-owned types. A persistence or web reference inside Domain ties each rule to a framework release, so a provider change reaches behavior it has nothing to do with. Infrastructure registers persisted discriminators and mappings, which leaves Domain free of serialization attributes. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:** In Claimwick, the expense aggregate compiles against money, typed identifiers, and its own parts, with the Marten serializer and its converters registered in Infrastructure.

**Checked by:** `dotnet build` reports `LS1001` for Domain code naming an outer-layer type.

### Construct Aggregate Parts Only Inside the Root

**ID:** `litespec/packs/dotnet/domain#construct-aggregate-parts-only-inside-the-root`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A file outside an aggregate's `Domain/` folder MUST NOT construct a type from that folder other than the root's input records, typed identifiers, and the types the interfaces its class implements name in their member signatures (a repository building the query result its Domain interface returns, an adapter building the result its port returns).

**Rationale:** The root factories and methods form the single entry point to the aggregate. A caller that constructs an entity or value object of the aggregate builds a part the root did not validate, and a caller that builds a part to pass it in takes over behavior the root owns. Callers pass plain inputs instead, and the root builds its own parts behind that boundary. This provision binds `litespec/core/backend-domain#reach-an-aggregate-only-through-its-root` for .NET.

**Example:** In the Claimwick product, the submit-expense handler constructs no expense line and no allocation. It passes the identifiers, the line inputs, and the submitted time to `Expense.Submit`, and the root builds its lines and its allocation inside the factory.

**Checked by:** `dotnet build` reports `LS1014` for a file outside an aggregate's `Domain/` folder that constructs a type from that folder other than the root's input records and typed identifiers.

### Mark Each State-Changing Root Method with Its Use Case

**ID:** `litespec/packs/dotnet/domain#mark-each-state-changing-root-method-with-its-use-case`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A root method that changes state MUST carry exactly one `<implements>` mark naming its use case.

**Rationale:** The `<implements>` mark is the XML documentation tag tying a method to the use case act it performs. The builder and the analyzer read the same link from it. A reading or private method performs no act and carries no use-case mark. This provision binds `litespec/core/backend-domain#give-each-use-case-its-own-act` for .NET.

**Example:**

```csharp
/// <implements>use-case/spending.approve-expense</implements>
public void Approve(ApproverId approverId, DateTimeOffset decidedAt)
{
    if (State is not ExpenseSubmittedState)
    {
        throw new ExpenseNotSubmittedException(Id);
    }

    State = new ExpenseApprovedState(approverId, decidedAt);
    RaiseDomainEvent(new ExpenseApprovedDomainEvent(Id, approverId, decidedAt));
}
```

**Checked by:** `dotnet build` reports `LS5003` for a root method carrying other than one use-case mark, and `LS5002` for a mark on a reading or private method.

### Model Child Entities as Sealed Classes

**ID:** `litespec/packs/dotnet/domain#model-child-entities-as-sealed-classes`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** A child entity MUST be a sealed class deriving from the project-owned `Entity<TId>`, with no public setter, mutating method, or constructor.

**Rationale:** The project-owned `Entity<TId>` base gives the child typed identity and identity equality, so the child declares its own data and behavior. The root creates, finds, and changes its children through business methods, and a child exposes no mutable state outside the root. Construction and mutation stay internal or private, so the root alone reaches them. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public sealed class OrderLine : Entity<OrderLineId>
{
    internal OrderLine(OrderLineId id, ProductId product, int quantity, Money price)
        : base(id)
    {
        Product = product;
        Quantity = quantity;
        Price = price;
    }

    public ProductId Product { get; }

    public int Quantity { get; private set; }

    public Money Price { get; }

    internal void ChangeQuantity(int quantity)
    {
        Quantity = quantity;
    }
}
```

`Order.AddLine` creates the line, and `Order.ChangeLineQuantity` checks its invariant before calling `ChangeQuantity`.

**Checked by:** `dotnet build` reports `LS1015` for a public setter, constructor, or mutable public collection on a non-root type of an aggregate.

### Implement Domain Services as Stateless Classes

**ID:** `litespec/packs/dotnet/domain#implement-domain-services-as-stateless-classes`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A domain service MUST be a stateless sealed class holding only a calculation that spans aggregates with no owning aggregate, accepting and returning Domain values.

**Rationale:** A calculation with no natural aggregate owner still belongs somewhere explicit. The stateless service holds that calculation apart from aggregates, with no persistence, messaging, logging, clock, authorization, or provider call. A calculation one aggregate owns stays in that aggregate root instead. The service carries a mark naming its rule, and the builder links it to the rule it computes. This provision binds `litespec/core/backend-domain#use-stateless-domain-services-for-ownerless-rules` for .NET.

**Example:** An order pricing service takes the lines and returns the total, and the order root calls it inside `Order.ConfirmPrice`. Application passes the lines and hands the returned `Money` to `Order.ConfirmPrice` as an input. The service itself holds no repository, clock, logger, or provider client, and returns no order line.

**Reviewed for:** Does every domain service stay stateless, take only Domain values, and avoid provider calls? Check each domain service.

### Model Every Aggregate Lifecycle with State Records

**ID:** `litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records`

**Strength:** required

**Applies to:** artifact: state-record

**Statement:** An aggregate MUST expose its lifecycle only through one `State` property backed by an abstract `{Aggregate}State` record with sealed per-state records.

**Rationale:** An enum, a status string, a boolean flag, a parallel nullable field, or a computed state carries no state-specific data such as a publication time. State records hold each state's data where it belongs, and aggregate methods own the transitions between them. A one-state aggregate still defines its hierarchy, so later states have a home. The sealed set matches the aggregate's declared `states`, which is the link the builder follows. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public abstract record PostState;

public sealed record PostDraftState : PostState;

public sealed record PostPublishedState(
    DateTimeOffset PublishedAt) : PostState;
```

A profile with one current state still defines its hierarchy:

```csharp
public abstract record ProfileState;

public sealed record ProfileActiveState : ProfileState;
```

**Checked by:** `dotnet build` reports `LS1003` for an aggregate with no `State` property, `LS1004` for a state case left unsealed, and `LS1013` for a switch that leaves a sealed state case to its discard arm.

### Model Every Closed Set of Domain Values Without Enums

**ID:** `litespec/packs/dotnet/domain#model-every-closed-set-of-domain-values-without-enums`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** Domain code MUST model every closed set as an abstract record base with sealed cases exposing one stable `Code`, converting labels through `FromCode`.

**Rationale:** An enum is a named integer, so it carries no case data and admits undefined values through a cast. When a case later gains data, the enum grows parallel fields. A record hierarchy carries each case's data in its own sealed record instead. A boundary turns a label into a case through `FromCode` instead of `ToString`, which turns an unrecognized label into a validation failure rather than a silent default. Tests cover the discard path for a case added later. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public abstract record MerchantRole
{
    public abstract string Code { get; }

    public static MerchantRole FromCode(string code) => code switch
    {
        "owner" => new MerchantOwnerRole(),
        "warehouse-staff" => new MerchantWarehouseStaffRole(),
        _ => throw new MerchantUnsupportedRoleException(code),
    };
}

public sealed record MerchantOwnerRole : MerchantRole
{
    public override string Code => "owner";
}

public sealed record MerchantWarehouseStaffRole : MerchantRole
{
    public override string Code => "warehouse-staff";
}
```

Callers branch on the case type with a `switch` expression. A case that carries data exposes it directly instead of through a separate nullable field. The discard arm sits after every sealed case has its own arm, handling a value outside the sealed set. Round-trip tests cover every case without data and catch a renamed code or a missing label:

```csharp
[Theory]
[MemberData(nameof(AllRoles))]
public void FromCode_round_trips_every_case(MerchantRole role)
{
    Assert.Equal(role, MerchantRole.FromCode(role.Code));
}
```

**Checked by:** `dotnet build` reports `LS1002` for an enum declared in Domain, `LS1013` for a switch that leaves a sealed case to its discard arm, and `LS2041` for a closed-set `Code` compared to a string literal or a refusal exception returning a raw string literal.

### Use Strongly Typed Version 7 Identifiers

**ID:** `litespec/packs/dotnet/domain#use-strongly-typed-version-7-identifiers`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** An aggregate identity MUST be a `readonly record struct` wrapping one version 7 `Guid`, declaring no implicit conversion, primitive alias, or public parameterless creation path.

**Rationale:** A bare `Guid` parameter accepts any identifier, including an empty one and one meant for another aggregate. The typed struct turns away `Guid.Empty` and the struct default in every factory, and a misplaced identifier fails at the boundary. New values arrive from the caller: a factory takes its identifier as an argument, and a method that needs several takes a generator. Domain reads no clock and calls no static source of time or randomness, which keeps business behavior deterministic and testable. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public readonly record struct PostId : IStronglyTypedId, IParsable<PostId>
{
    private PostId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get; }

    public static PostId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new PostIdentityRequiredException();
        }

        return new PostId(value);
    }

    public static PostId Parse(string value, IFormatProvider? provider) =>
        From(Guid.Parse(value));

    public static bool TryParse(
        string? value,
        IFormatProvider? provider,
        out PostId result)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            result = new PostId(parsed);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}
```

The `IParsable<TId>` implementation supports Minimal API route and query binding: `Parse` follows the .NET parsing contract for malformed text, while `From` applies the domain empty-identity rule. When two callers generate the same value, persistence turns away the duplicate key on the same path as any other write conflict.

**Checked by:** `dotnet build` reports `LS1012` for an `Id` struct outside the strongly typed form, or a raw `Guid` as an aggregate identity.

### Reference Other Aggregates by Typed Identifier

**ID:** `litespec/packs/dotnet/domain#reference-other-aggregates-by-typed-identifier`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate MUST reference another aggregate only through a property typed as its typed identifier, never through a property typed as its root or entity.

**Rationale:** An object reference between aggregates loads one aggregate's data through another, which joins their consistency boundaries and their storage. A property typed as the other aggregate's typed identifier keeps each aggregate loadable on its own. This provision binds `litespec/core/backend-domain#reference-other-aggregates-by-id` for .NET.

**Example:** In the Claimwick product, an expense holds the submitting employee as `EmployeeId`, not as an `Employee` object. Application loads the employee through its own repository when a command needs both sides.

**Checked by:** `dotnet build` reports `LS1016` for a property of one aggregate whose type is another aggregate's root or entity.

### Offer a Non-Throwing Parse on Validated Value Types

**ID:** `litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** A typed identifier or value type whose constructor throws on invalid input MUST offer a static `TryParse` that returns false instead of throwing.

**Rationale:** A serializer and an API boundary meet invalid input as a normal case: a malformed identifier in a route, an empty value in a stored payload. When the single way in is a throwing constructor, the boundary answers a server fault for a caller mistake, and a serializer that calls no constructor rebuilds the value empty without saying so. A `TryParse` lets the boundary turn the input away with a 400 and lets a converter fail loudly on a value it reads.

**Example:** In the Claimwick product, `ExpenseId` throws on an empty `Guid` and offers `ExpenseId.TryParse(string? text, out ExpenseId id)`. The route binding calls `TryParse` and answers 400 for `/expenses/not-an-id`; the JSON converter calls it and fails the read for a stored empty value.

**Checked by:** `dotnet build` reports `LS1017` for a Domain struct whose constructor throws and which declares no non-throwing parse.

### Keep Clock Reads and Random Values Out of Domain

**ID:** `litespec/packs/dotnet/domain#keep-clock-reads-and-random-values-out-of-domain`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Domain code MUST NOT read a clock, a random source, or a new `Guid`, so every such value arrives as an argument.

**Rationale:** Each of `DateTime`, `DateTimeOffset`, `TimeProvider`, `Random`, `RandomNumberGenerator`, `Guid.NewGuid`, and `Guid.CreateVersion7` returns a different value on every call. A Domain method that reads one runs differently on each run and resists fixed-value tests. Receiving each value as an argument keeps every Domain call a pure function of its inputs, and the Application layer obtains each value through the port it owns. This provision binds `litespec/core/backend-domain#pass-nondeterministic-values-into-domain` for .NET.

**Example:**

```csharp
// The rule reads the clock itself.
public void Publish() => PublishedAt = DateTimeOffset.UtcNow;

// The caller passes the time in.
public void Publish(DateTimeOffset publishedAt) => PublishedAt = publishedAt;
```

**Checked by:** `dotnet build` reports `LS1009` for a clock, randomness, or identifier read in Domain.

### Keep Double and Float Out of Exact Quantities

**ID:** `litespec/packs/dotnet/domain#keep-double-and-float-out-of-exact-quantities`

**Strength:** required

**Applies to:** artifact: value-object

**Statement:** Domain code MUST NOT declare a `double` or `float` for a monetary amount, a rate applied to one, or a quantity compared for exact equality.

**Rationale:** `double` and `float` render most decimal fractions approximately, so two amounts that read as equal compare as different. The shared `Money` object holds a `decimal`, and a rate applied to a `Money` amount is a `decimal` too. The analyzer reports the Domain declaration that breaks this form. This provision binds `litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities` for .NET.

**Example:**

```csharp
// A rate applied to a Money amount is a decimal.
public Money ApplyRate(decimal rate) => Create(Amount * rate, Currency);
```

**Checked by:** `dotnet build` reports `LS1008` for a `double` or `float` type in Domain.

### Keep Repository Interfaces in Domain

**ID:** `litespec/packs/dotnet/domain#keep-repository-interfaces-in-domain`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A repository interface MUST expose only aggregate and Domain types for exactly one aggregate root, without `IQueryable`, a session, or a generic CRUD surface.

**Rationale:** One repository per aggregate keeps the write boundary aligned with the consistency boundary: the interface loads and stages complete aggregates of its own type, and nothing else. `GetByIdAsync` throws `{Aggregate}NotFoundException` for a load that has to succeed, which frees handlers from repeating null checks, while a nullable `FindBy...Async` stays reserved for lookups where absence is normal. `IQueryable`, sessions, and generic CRUD surfaces would leak query shaping and provider behavior into Domain, so the interface exposes none of them. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public interface IPostRepository
{
    Task<Post> GetByIdAsync(
        PostId id,
        CancellationToken cancellationToken);

    void Store(Post post);
}
```

In the example, `GetByIdAsync` returns the loaded `Post` and throws `PostNotFoundException` when the identity has no aggregate. The Infrastructure implementation throws that exception, and the command pipeline owns the commit. `Store` stages the aggregate through the selected provider implementation.

```csharp
// Infrastructure throws the aggregate's own not-found exception.
public async Task<Post> GetByIdAsync(PostId id, CancellationToken cancellationToken)
{
    var post = await session.LoadAsync<Post>(id, cancellationToken);
    return post ?? throw new PostNotFoundException();
}
```

**Checked by:** `dotnet build` reports `LS1010` for a repository interface exposing a query surface.

### Raise Immutable Domain Facts

**ID:** `litespec/packs/dotnet/domain#raise-immutable-domain-facts`

**Strength:** required

**Applies to:** artifact: domain-event

**Statement:** A domain event MUST be a public immutable record named `{Aggregate}{PastFact}DomainEvent` implementing `IDomainEvent`, carrying business data only and one mark naming its domain-event record.

**Rationale:** The `{Aggregate}{PastFact}DomainEvent` name tells a reader which aggregate raised the fact without opening the file. The event carries business data: the typed identifiers and the values its reactions need. It carries no aggregate, session, service, mutable collection, or exception, because a mutable payload lets a reaction observe state that changed after the fact. Envelope identity, such as message and causation identity, lives on the published integration event that carries the fact outside the process. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:** In Claimwick, `ExpenseApprovedDomainEvent` carries the expense identifier, the approver, the amount, and the decided time. Only the expense root raises it, through `RaiseDomainEvent` on the shared base.

**Checked by:** `dotnet build` reports `LS1007` for a domain event that is not an immutable record.

### Reject Business Violations with Domain Exceptions

**ID:** `litespec/packs/dotnet/domain#reject-business-violations-with-domain-exceptions`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** A rejected business rule MUST throw a `DomainException` subclass named `{DomainType}{Reason}Exception`, carrying the refusal `code` and one mark naming its rule.

**Rationale:** One exception type per rule puts the stable code and the message in the type. The call site passes domain values, and two rules share a type by accident no more. A shared rule exception with caller-supplied codes lets any caller invent a code and lets unrelated rules merge silently. Each exception's code equals the refusal `code` on the result record it answers. The root factory and methods check an invariant rule, and the use-case validator or the root method checks a precondition rule. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
// One type per rule; the type owns the code and message.
if (allocation.Count == 0)
{
    throw new RefundAllocationRequiredException();
}

if (allocation.Any(line => line.Quantity <= 0))
{
    throw new RefundAllocationQuantityInvalidException();
}
```

```csharp
/// <implements>rule/refunds.allocation-required</implements>
public sealed partial class RefundAllocationRequiredException : DomainException
{
    public RefundAllocationRequiredException()
        : base("A refund requires at least one allocation line.")
    {
    }
}
```

From the refusal result the rule names, the generator writes `public override string Code => "refunds.allocation-required";`. Malformed caller input stays with Application validators, and command handlers catch no expected Domain exceptions: the host maps them through the documented error boundary.

**Checked by:** `dotnet build` reports `LS1005` for a throw outside a `DomainException` subclass and `LS1006` for a domain exception that is misnamed or open.

### Throw Only Refusal Codes the Module Owns

**ID:** `litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns`

**Strength:** required

**Applies to:** artifact: aggregate, value-object

**Statement:** Each refusal code a module declares MUST be carried by at most one exception type, in the module that declares it, under the aggregate whose set declares the refusal.

**Rationale:** A refusal code tells the caller which module refused and why. An exception in one module that carries another module's code blames the wrong owner, and two exception types carrying one code leave the caller unable to tell which rule failed. One carrying type per code in the declaring module keeps the blame and the rule behind each code fixed. Each aggregate declares the refusals its rules raise once in its own set, with the module set keeping the refusals no aggregate raises. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:** The spending module declares `spending.receipt-missing`, and only its `ExpenseReceiptMissingException` carries that code:

```csharp
/// <implements>rule/expense.receipt-required-above-threshold</implements>
public sealed partial class ExpenseReceiptMissingException : DomainException
{
    public ExpenseReceiptMissingException()
        : base("An expense above the receipt threshold carries no receipt.")
    {
    }
}
```

From the refusal result the rule names, the generator writes `public override string Code => "spending.receipt-missing";`. A receipt exception in the reporting module carrying `spending.receipt-missing`, or a second spending exception carrying the same code, breaks this rule.

**Checked by:** `litespec model check` reports `refusal-module-foreign` (level S) for a refusal code carried from another module's code, `refusal-code-duplicate` (level S) for a refusal code two exception types carry, and `code-placement-drift` or `record-placement-invalid` for a refusal and its exception declared under different homes.

## Conventions

### Define the Shared Domain Contracts Once

**ID:** `litespec/packs/dotnet/domain#define-the-shared-domain-contracts-once`

**Strength:** default

**Applies to:** artifact: any

**Default:** Declare the shared Domain contracts once in the shared kernel Domain folder and reuse them across every module.

**Replacement:** A project names its shared contract home in its Domain documentation.

**Rationale:** Every module raises events, throws domain exceptions, and identifies aggregates. A shared contract declared twice becomes two contracts that drift. Declaring `IDomainEvent`, `DomainException`, `IStronglyTypedId`, `Entity<TId>`, `IAggregateRoot`, and `AggregateRoot<TId>` once in the shared kernel Domain folder gives every module the same mechanics. `DomainException` takes the message in its constructor and declares `Code` abstract, so each refusal code lands as an override in the partial exception. Generic handling constrains its parameters with `IAggregateRoot`. This provision binds [Backend Domain](../../core/backend-domain.md) for .NET.

**Example:**

```csharp
public interface IDomainEvent;

public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    public abstract string Code { get; }
}

public interface IStronglyTypedId
{
    Guid Value { get; }
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IStronglyTypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

In the example, `ClearDomainEvents` is event-delivery mechanics, and Infrastructure calls it after the selected delivery path accepts the pending events. Concrete aggregates validate that `id.Value` is not `Guid.Empty` before or while calling the base constructor.

**Reviewed for:** Is every shared Domain contract declared once, with `DomainException` and `AggregateRoot<TId>` in their declared shapes? Check the shared contracts; `dotnet build` reports `LS5013` for a base that declares a non-virtual `Code`.

### Split a Large Root into One File per Action

**ID:** `litespec/packs/dotnet/domain#split-a-large-root-into-one-file-per-action`

**Strength:** default

**Applies to:** artifact: aggregate

**Default:** Declare a root whose file passes 500 lines as a partial class with one folder per business action named `{Act}/`, holding the partial file `{Root}.{Act}.cs` with the action's inputs and its private rules.

**Replacement:** A project names its own large-root layout in its Domain documentation.

**Rationale:** A root grows with every business action it owns, and one file holding every action reads as one long listing. Splitting the root into one folder per action keeps each action readable on its own while the root stays the single entry point to its aggregate. The name is the use case act, so `Domain/Entries/PostInvoice/` mirrors `Application/Entries/PostInvoiceEntry/`, and the action rules stay private to the root.

**Example:** In the Claimwick product, the expense root keeps its identity and state in `Expense.cs`, its submission factory and allocation rules in `Submit/Expense.Submit.cs`, and its approval in `Approve/Expense.Approve.cs`:

```csharp
// Submit/Expense.Submit.cs
public sealed partial class Expense
{
    public static Expense Submit(
        ExpenseId id,
        EmployeeId employeeId,
        IReadOnlyList<ExpenseLineInput> lines,
        DateTimeOffset submittedAt)
    {
        // Builds the lines and the cost-center allocation, selects the submitted state,
        // and records the submitted event.
        ...
    }
}
```

**Reviewed for:** Does any root file hold more than one business action or run past 500 lines without following this layout? Look at each root file beside its actions.
