# Overview

When a build error names a rule ID, open the page with the same ID below. Each rule page names the provision the rule enforces, shows where the diagnostic sits, lists what the rule ignores, and explains the fix, with a failing example beside a passing one. The 55 rules are grouped by the layers they run in, with a one-line summary of what each rule reports.

## Domain Rules

Rules `LS1001` to `LS1016` check Domain modeling. `LS1014` runs in every production layer; the rest run in Domain only.

- [LS1001: Domain References an Outer-Layer Concern](ls1001.md): reports a Domain use of an outer-layer namespace.
- [LS1002: Domain Declares an Enum](ls1002.md): reports any enum declared in Domain.
- [LS1003: Aggregate Has No State Record](ls1003.md): reports an aggregate carrying no `State` record.
- [LS1004: State Case Is Not Sealed](ls1004.md): reports a state case left open for extension.
- [LS1005: Domain Throws a Non-Domain Exception](ls1005.md): reports a throw of any other exception type.
- [LS1006: Domain Exception Is Misnamed or Open](ls1006.md): reports a `DomainException` subclass that is misnamed or open.
- [LS1007: Domain Event Is Not an Immutable Record](ls1007.md): reports a domain event that is not an immutable record.
- [LS1008: Domain Uses Binary Floating Point](ls1008.md): reports a `double` or `float` type in Domain.
- [LS1009: Domain Reads a Nondeterministic Value](ls1009.md): reports Domain reading the clock, randomness, or a new identifier.
- [LS1010: Repository Exposes a Query Surface](ls1010.md): reports a repository interface exposing a query surface.
- [LS1011: Public Domain Contract Is Undocumented](ls1011.md): reports a public Domain type or member with no XML documentation.
- [LS1012: Identifier Is Not a Strongly Typed Value](ls1012.md): reports a raw or malformed identifier in Domain.
- [LS1013: Switch Leaves a Sealed Case to Its Discard Arm](ls1013.md): reports a switch leaving a sealed case to its discard arm.
- [LS1014: Aggregate Part Constructed Outside Its Domain Folder](ls1014.md): reports construction of an aggregate part outside its `Domain/` folder.
- [LS1015: Non-Root Type Exposes Public Mutation](ls1015.md): reports a public setter, constructor, or mutable collection on a non-root aggregate type.
- [LS1016: Aggregate References Another Aggregate by Object](ls1016.md): reports an aggregate property typed as another aggregate's root or entity.

## Outer-Layer Rules

Rules `LS2001`, `LS2003` to `LS2007`, and `LS2009` to `LS2028` run across layers, as each page states.

- [LS2001: Implementation Type Is Not Internal Sealed](ls2001.md): reports an implementation type visible outside its assembly or open to extension.
- [LS2003: WebApi Uses an MVC Controller](ls2003.md): reports a class derived from `ControllerBase` or carrying `ApiController`.
- [LS2004: Command Handler Writes Through a Document Session](ls2004.md): reports a command handler holding a document session or database context.
- [LS2005: Handler Name Lacks Its Command or Query Suffix](ls2005.md): reports a handler name missing its `CommandHandler` or `QueryHandler` suffix.
- [LS2006: Endpoint Name Lacks Its Suffix](ls2006.md): reports an endpoint name missing its `Endpoint` suffix.
- [LS2007: Reaction or Job Name Hides Its Kind](ls2007.md): reports a reaction or job name that hides its kind.
- [LS2009: Result Part Not Named After Its Result](ls2009.md): reports a result part whose name does not start with its result.
- [LS2010: Repository Variable Not Named After Its Interface](ls2010.md): reports a repository variable whose name does not follow its interface.
- [LS2011: Query Handler Takes a Repository](ls2011.md): reports a query handler taking a repository in its constructor.
- [LS2012: Handler Authorizes Itself](ls2012.md): reports a handler that authorizes itself or takes the authorization guard.
- [LS2013: Audit Value Read from the Message in the Handler](ls2013.md): reports a handler audit value the message already carries.
- [LS2014: Command Handler Finds or Creates](ls2014.md): reports a handler that creates only where its find came back empty.
- [LS2015: Command Handler Changes Records in a Loop](ls2015.md): reports a handler writing through a repository inside a loop.
- [LS2016: Handler Takes a Pipeline-Owned Collaborator](ls2016.md): reports a handler taking the unit of work, the outbox, or another listed collaborator.
- [LS2018: File Names a Type Outside Its Layer](ls2018.md): reports an Application, Ports, or WebApi file naming a type its layer may not use.
- [LS2019: Use Case Names a Type of Another Use Case](ls2019.md): reports a use case folder naming a type from another use case folder.
- [LS2020: Module Names a Type Outside Another Module's Contracts](ls2020.md): reports a module naming another module past its `Contracts/` folder.
- [LS2021: Message Sent Outside an Entry Point](ls2021.md): reports a mediator held outside an endpoint, reaction, or job.
- [LS2022: Event Published by User Code](ls2022.md): reports user code naming the event mediator or an event publisher.
- [LS2023: Class Implements Two Message Definitions](ls2023.md): reports a class implementing two message definition contracts.
- [LS2024: Module Holds Platform-Owned Capability Code](ls2024.md): reports serializer, store, guard, or converter code in a module.
- [LS2025: Adapter Names a Type Outside Its Layer](ls2025.md): reports an `Infrastructure/{Port}/` adapter naming a Domain or WebApi type.
- [LS2026: Model Duplicated in Two Use Case Folders](ls2026.md): reports a record declared with the same members in two use case folders.
- [LS2027: Application Does Infrastructure Work](ls2027.md): reports archive, serialization, or document work in Application.
- [LS2028: Module Exposes a Public Type Outside Contracts](ls2028.md): reports a public type in a module project outside its entry type.

## Naming and Style Rules

Rules `LS3001` to `LS3005` run in every production layer.

- [LS3001: Asynchronous Method Name Lacks Async](ls3001.md): reports an async method whose name does not end in `Async`.
- [LS3002: CancellationToken Is Not the Last Parameter](ls3002.md): reports a cancellation token that is misplaced or misnamed.
- [LS3003: File Does Not Match Its Primary Type](ls3003.md): reports a C# file whose name differs from its primary type.
- [LS3004: Concrete Class Is Not Sealed](ls3004.md): reports an unsealed class nothing derives from.
- [LS3005: Class or Struct Declares a Primary Constructor](ls3005.md): reports a class or struct primary constructor.

## Test Rules

Rule `LS4001` runs in test projects only.

- [LS4001: Test Contains No Assertion](ls4001.md): reports a test that asserts nothing.

## Traceability Rules

Rules `LS5003` to `LS5006`, `LS5009`, and `LS5011` to `LS5013` run across layers, as each page states.

- [LS5003: Code Element Carries the Wrong Number of Marks](ls5003.md): reports an element whose mark count differs from what its kind takes.
- [LS5004: Mark Uses the Undeclared Style](ls5004.md): reports a mark in the style the project did not declare.
- [LS5005: Runtime Value Differs from Its Record](ls5005.md): reports a hand-written event `Classification` or exception `Code` differing from its record.
- [LS5006: Marked Event or Exception Is Not Partial](ls5006.md): reports a marked event or exception not declared `partial`.
- [LS5009: Two Permissions Generate One Constant Name](ls5009.md): reports two permissions generating one constant name.
- [LS5011: One Message Maps to Two Use Cases](ls5011.md): reports one message mapped to two use cases.
- [LS5012: Role Allows an Undeclared Permission](ls5012.md): reports a role allowing a permission no record declares.
- [LS5013: Base Declares a Non-Virtual Code](ls5013.md): reports a marked exception inheriting a non-virtual `Code`.
