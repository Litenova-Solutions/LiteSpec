# Overview

## Intent

This index lists the 46 analyzer rules, one reference page per rule, grouped by the projects each rule runs in. A developer who meets an error opens the page with the same ID to see what the rule reports, where the error sits, and how to fix the code. Each entry carries a 1-line summary of what the rule reports.

## Domain Rules

Rules `LS1001` to `LS1013` run in Domain only.

- [LS1001: Domain References an Outer-Layer Concern](ls1001.md): reports a Domain use of an outer-layer namespace.
- [LS1002: Domain Declares an Enum](ls1002.md): reports an enum declared in Domain.
- [LS1003: Aggregate Has No State Record](ls1003.md): reports an aggregate with no `State` record.
- [LS1004: State Case Is Not Sealed](ls1004.md): reports a state case that is not sealed.
- [LS1005: Domain Throws a Non-Domain Exception](ls1005.md): reports a throw of a non-`DomainException` type.
- [LS1006: Domain Exception Is Misnamed or Open](ls1006.md): reports a `DomainException` subclass that is misnamed or open.
- [LS1007: Domain Event Is Not an Immutable Record](ls1007.md): reports a domain event that is not an immutable record.
- [LS1008: Domain Uses Binary Floating Point](ls1008.md): reports a `double` or `float` use.
- [LS1009: Domain Reads a Nondeterministic Value](ls1009.md): reports a read of the clock, randomness, or a new identifier.
- [LS1010: Repository Exposes a Query Surface](ls1010.md): reports a repository that exposes a query surface.
- [LS1011: Public Domain Contract Is Undocumented](ls1011.md): reports a public contract without XML documentation.
- [LS1012: Identifier Is Not a Strongly Typed Value](ls1012.md): reports an identifier that is not a strongly typed value.
- [LS1013: Domain Event Sits Outside the Events Folder](ls1013.md): reports a domain event outside the `Events` folder.

## Outer-Layer Rules

Rules `LS2001` and `LS2003` to `LS2017` run in Application, Infrastructure, WebApi, or Worker, as each page states.

- [LS2001: Implementation Type Is Not Internal Sealed](ls2001.md): reports an implementation type that is not `internal sealed`.
- [LS2003: WebApi Uses an MVC Controller](ls2003.md): reports an MVC controller.
- [LS2004: Command Handler Writes Through a Document Session](ls2004.md): reports a command handler that writes through a session.
- [LS2005: Handler Name Lacks Its Command or Query Suffix](ls2005.md): reports a handler whose name lacks its command or query suffix.
- [LS2006: Endpoint Name Lacks Its Suffix](ls2006.md): reports an endpoint whose name does not end in `Endpoint`.
- [LS2007: Event Handler or Job Name Hides Its Kind](ls2007.md): reports an event handler or job whose name hides its kind.
- [LS2008: Event Handler Sits Outside Its Fact Folder](ls2008.md): reports an event handler outside its `On{Fact}` folder.
- [LS2009: Result Part Not Named After Its Result](ls2009.md): reports a result part not named after its result.
- [LS2010: Repository Variable Not Named After Its Interface](ls2010.md): reports a repository variable not named after its interface.
- [LS2011: Query Handler Takes a Repository](ls2011.md): reports a query handler that takes a repository.
- [LS2012: Handler Authorizes Itself](ls2012.md): reports a handler that throws a forbidden refusal.
- [LS2013: Audit Value Read from the Message in the Handler](ls2013.md): reports an audit value read from the message in the handler.
- [LS2014: Command Handler Finds or Creates](ls2014.md): reports a command handler that finds or creates.
- [LS2015: Command Handler Changes Records in a Loop](ls2015.md): reports a command handler that changes records in a loop.
- [LS2016: Handler Takes a Pipeline-Owned Collaborator](ls2016.md): reports a handler that takes a pipeline-owned collaborator.
- [LS2017: LiteSpecLayer Names No Known Layer](ls2017.md): reports a declared layer value that names no known layer.

## Naming and Style Rules

Rules `LS3001` to `LS3004` run in every production project.

- [LS3001: Asynchronous Method Name Lacks Async](ls3001.md): reports an async method name without `Async`.
- [LS3002: CancellationToken Is Not the Last Parameter](ls3002.md): reports a `CancellationToken` that is misplaced or misnamed.
- [LS3003: File Does Not Match Its Primary Type](ls3003.md): reports a file that does not match its primary type.
- [LS3004: Concrete Class Is Not Sealed](ls3004.md): reports a concrete leaf class that is not sealed.

## Test Rules

Rule `LS4001` runs in test projects only.

- [LS4001: Test Contains No Assertion](ls4001.md): reports a test method with no assertion.

## Traceability Rules

Rules `LS5001` to `LS5012` run across layers, as each page states.

- [LS5001: Code Element Carries No Mark](ls5001.md): reports a code element of a recognized kind with no mark.
- [LS5002: Mark Names No Usable Record](ls5002.md): reports a mark naming no record, or a record of a type its element does not take.
- [LS5003: Code Element Carries the Wrong Number of Marks](ls5003.md): reports a code element with more or fewer marks than its kind takes.
- [LS5004: Mark Uses the Undeclared Style](ls5004.md): reports a mark written in the style the project did not declare.
- [LS5005: Runtime Value Differs from Its Record](ls5005.md): reports a hand-written event `Classification` or exception `Code` differing from its record.
- [LS5006: Marked Event or Exception Is Not Partial](ls5006.md): reports a marked domain event or rule exception that is not `partial`.
- [LS5007: Marked Act Raises an Undeclared Event](ls5007.md): reports a marked method raising an event its use case does not list.
- [LS5008: Declared Event Is Raised by No Marked Act](ls5008.md): reports a listed event no method marked with its use case raises.
- [LS5009: Two Permissions Generate One Constant Name](ls5009.md): reports two permission records whose names generate the same constant.
- [LS5010: Permission Opens with an Undeclared Verb](ls5010.md): reports a permission record whose name opens with an undeclared verb.
- [LS5011: One Message Maps to Two Use Cases](ls5011.md): reports a message two marked handlers serve with different use cases.
- [LS5012: Role Allows an Undeclared Permission](ls5012.md): reports a role whose record allows a permission no record declares.
