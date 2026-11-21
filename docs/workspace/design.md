# Class Design

**Stack:** any
**Concern:** workspace

Every codebase answers the same design questions: what one class owns, when it takes a second interface, and how shared behavior reaches it. This page states the design rules behind the naming and layering pages: one responsibility per class, one capability per set of interfaces, one class per pipeline concern, and composition over helper base classes. The rules apply to every stack; each stack page binds them where a check needs the stack form.

## Concepts

### Responsibility and Capability

A responsibility is one reason for a class to change. The test is plain: the class's job reads in one sentence without "and". A class that joins two jobs forces every reader of one job to review the other.

A capability is one thing a class offers its callers. Interfaces that describe one capability, such as read-only list access with enumeration, belong on one class. Interfaces for unrelated concerns, such as authorization and audit, describe two capabilities and belong on two classes.

### Pipeline Concerns

Each position of the command pipeline, such as authorization, audit, and idempotency, declares its behavior beside the message it guards. Each declaration changes for its own reason and is read by its own review: an access review reads the authorization declaration, and an audit review reads the audit declaration.

## Standards

### Give Each Class One Responsibility

**ID:** `litespec/workspace/design#give-each-class-one-responsibility`

**Strength:** required

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A class MUST have one responsibility.

**Rationale:** A class with one responsibility has one reason to change, so a reader reviews one job and a change risks one job. Without this rule, an author optimizes for fewer files and merges unrelated concerns wherever an interface allows it. The test stays plain: the class's job reads in one sentence without "and". Settled by [decision/0106-state-class-design-rules](decision/0106-state-class-design-rules).

**Example:** In the Claimwick product, an expense repository that also renders the spending report splits into the repository and a report renderer. Each is read by different people and changes for different reasons.

**Reviewed for:** Does every class have one responsibility, readable in one sentence without "and"? Read each class beside its callers.

### Implement One Capability per Class

**ID:** `litespec/workspace/design#implement-one-capability-per-class`

**Strength:** required

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A class MUST implement more than one interface only when the interfaces describe one capability.

**Rationale:** Interfaces that describe one capability offer callers one thing, so they belong together. Interfaces for unrelated concerns, such as authorization, audit, idempotency, persistence, and rendering, change for different reasons and are read by different people. One class implementing two of them merges those concerns into one file, and a later reader cannot review one without the other. Settled by [decision/0106-state-class-design-rules](decision/0106-state-class-design-rules).

**Example:** A class implementing `IReadOnlyList<T>` with `IEnumerable<T>` offers one capability. A class implementing an authorization contract with an audit contract merges two concerns, and splits into two classes.

**Reviewed for:** Does any class implement interfaces for more than one capability? Read the interface list of each class.

### Declare Each Pipeline Concern in Its Own Class

**ID:** `litespec/workspace/design#declare-each-pipeline-concern-in-its-own-class`

**Strength:** required

**Applies to:** artifact: use-case

**Statement:** Each pipeline declaration MUST sit in its own class, implementing exactly one pipeline contract.

**Rationale:** Who may send a message and how the audit trail records it change for different reasons, and are read by different people: an access review reads the authorization declaration, and an audit review reads the audit declaration. One class per concern keeps each review to its own file. Settled by [decision/0106-state-class-design-rules](decision/0106-state-class-design-rules).

**Example:** In the Claimwick product, the submit-expense authorization and audit declarations sit in two classes: `SubmitExpenseCommandAuthorizationDefinition` and `SubmitExpenseCommandAuditDefinition`. Each implements its own pipeline contract, and neither names the other concern.

**Reviewed for:** Does any class declare two pipeline concerns? Read each pipeline declaration beside its message.

### Take Shared Behavior Through Composition

**ID:** `litespec/workspace/design#take-shared-behavior-through-composition`

**Strength:** required

**Applies to:** artifact: aggregate, state-record, value-object, domain-event, use-case, command-handler, query-handler, validator, reaction, port, repository, endpoint, migration, job

**Statement:** A class MUST take shared behavior through composition rather than through a base class that collects unrelated helpers.

**Rationale:** A base class that collects unrelated helpers gives every derived class behavior most of them never use, and each addition to the base reaches every derived class. Taking the behavior through a constructor parameter keeps each class beside only the behavior it uses. A base that defines one shared contract, such as the aggregate root base, is no helper collection. Settled by [decision/0106-state-class-design-rules](decision/0106-state-class-design-rules).

**Example:** In the Claimwick product, an expense validator takes its receipt checker as a constructor parameter instead of deriving from a validation base that also formats currencies.

**Reviewed for:** Does any class derive from a base that collects unrelated helpers? Read each base class beside its derived classes.

## Conventions

### Name Each Definition After Its Message and Concern

**ID:** `litespec/workspace/design#name-each-definition-after-its-message-and-concern`

**Strength:** default

**Applies to:** artifact: use-case

**Default:** Name each pipeline declaration class `{Message}{Concern}Definition`, such as `SubmitExpenseCommandAuthorizationDefinition`.

**Replacement:** A project names its own pipeline declaration naming in its project documentation.

**Rationale:** The message leads, so every declaration for one message lists beside it, and the concern trails, so each review finds its own file. One naming shape lets a reader move from the message to each of its declarations without searching. Settled by [decision/0106-state-class-design-rules](decision/0106-state-class-design-rules).

**Reviewed for:** Does every pipeline declaration class carry its message and concern in `{Message}{Concern}Definition` form? Read each declaration beside its message.
