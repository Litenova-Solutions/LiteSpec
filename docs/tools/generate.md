# Generate

## Name

`litespec generate` writes the code the records fully determine, from the `generate` targets of the consumer declaration.

## Synopsis

```bash
litespec generate [--root <consumer>] [--check]
```

## Description

The command reads the built project model and writes one file set per `generate` target in `.litespec/project.yaml`. Each target names a `kind`, a `stack`, and the `path` folder the files go in:

- `refusal-codes` writes the refusal codes the records declare. For a .NET stack each module with refusals gets one C# file with a public static class holding one public const string per refusal, named from the refusal key in PascalCase with the code as its value and the refusal's meaning as its documentation. For a TypeScript stack the target writes one file with a const object of codes, their union type, and a map from each code to the English sentence its refusal results state.
- `permissions` writes the permission ids the same way: one C# constant class per module holding permissions, or one TypeScript file with a const object of ids and their union type.
- `event-decisions` writes, per module with domain events, one C# file with a static table holding each domain event the records declare with the outcome the outbox decision check derives from the records: `ignored` when the event record states `ignored: true`, `consumed` when a reaction or a translating event consumes it, and `missing` when the records state neither. A consumer's startup check compares its own decisions against this table instead of typed lists.
- `closed-sets` writes, per module or knowledge area holding closed value sets, one C# file with one partial abstract record per set and one sealed partial case per allowed value. A set is a reference-data table or a setting stating `choices`. The base name is the set name in PascalCase, and each case name is the base name with the value in PascalCase. The base exposes the stable `Code`, `FromCode`, `TryFromCode`, and `All` in code order. `FromCode` throws `ArgumentException` for an unknown code: an unknown label is malformed input, not a broken business rule, so a caller converts an untrusted label with `TryFromCode` and refuses through its own rule. Each case carries the meaning the record states for its value in `valueMeanings`. A hand-written sibling file may add members to any generated type.
- `knowledge-sets` writes, per area and variant, one C# file per reference-data table holding `row` and `columns`: a static partial class with a `Rows` list building each row of the variant through the model's factory, each row a call with named arguments from the column mapping. A column renders as the mapping states: a literal, a date or money construction, a typed key through the model's factory, a member of the model's closed set, or a mapped literal. A column the row does not state renders as null.
- `exceptions` writes, per aggregate-declared refusal, one C# file with an internal sealed partial exception class named for the aggregate and the refusal, deriving from the target's `baseType`, carrying the code, the meaning as its message, and the rule mark the marks table requires. A refusal with `parameters` takes one string constructor parameter per value and interpolates the message. A refusal no rule names carries no mark, and the mark check then asks for the rule. A module-level refusal renders no file: the placement checks admit no per-module folder for its carrier, so it keeps a hand-written carrier.
- `domain-events` writes, per domain event, one C# file with an internal sealed partial record named for the aggregate and the fact, with one positional property per carried field in written order, implementing the target's `markerType`, documented from the meaning and carrying the event mark the marks table requires. An `id` becomes the typed identifier of what it identifies, or a `Guid` for any other target; a `choice` becomes the closed set it names; a list becomes a read-only list of its row, with a nested row record when the row holds more than one field. A hand-written sibling file may add members to any generated type. An event with a field of no known shape renders no file, and the record check reports the field.
- `module-events` writes, per module event, one C# file in an `On{Fact}` folder with a public sealed partial record named for the fact, with one positional property per published field in written order and types from the translated domain event, documented from the summary and carrying the event mark the marks table requires. The target's `markerType` is optional: a module event implements none when the target names none. A hand-written sibling file keeps the factory building the event from its domain event. An event whose translation or field resolves to nothing renders no file, and the record check reports it.
- `declarations` writes, per module with use cases, one C# file with an internal sealed partial class implementing the LiteBus `IMultiMessageDefinition`, declaring for each command and query what its record states: authorization from `access`, audit from `audit`, and idempotency from `idempotency`. The authorization names one policy: the grant as a permission, signed-in, anonymous, system, or self form, wrapped for a self scope, a recent sign-in, and system runs. The audit writes `Audited` with the use case id, category, and target kind, reading the target off the message when the record names its input, or `NotAudited` with the exemption reason. A keyed command keys through the message's properties from its key template with its scope, replaying the first result exactly when the record states `returns-first-result`; a command of mode `state` or `none` exempts with its rationale. A hand-written sibling file may add members to the generated type. A use case the policy vocabulary cannot express renders nothing, and the command lists it as skipped with the reason.
- `validators` writes, per command and query carrying constrained inputs, one C# file in the use case `Application/` folder with an internal sealed partial validator class implementing the LiteBus validator for its message. Each constrained field renders one rule reporting a validity finding with the member, a stable reason code, and a message naming the field: presence for a required field the type admits missing, length and pattern for text, and span for numbers. The pipeline refuses every finding through the derived `invalid-input` result. A constraint citing a rule that states a refusal renders no rule: Domain refuses it through its written result. A partial method hook runs the hand-written checks after the generated rules, and a hand-written sibling file may add members to any generated type. The validator carries no mark; the handler beside it carries the use case mark. A use case with no constrained input renders no file.
- `endpoints` writes, per module with HTTP-reached use cases, one C# file with an internal static partial class holding one nested endpoint class per use case, each implementing the platform endpoint contract. Each endpoint maps its route with its method, binds each input from its binding, builds the message, sends it through the LiteBus mediator, and answers the success status the outcome table assigns: a query, or a command stating a created location, projects its result with 200 or with 201 and the location, while any other command answers 204 with no body. A choice input converts through its closed set first, and an unlisted label answers 400 before the message runs. A `claim` input reads through the target's `claimReader`, and a derived input the clock does not cover reads through a partial derivation method. A partial shaping method per projected result and a hand-written sibling file keep the response mapping and the derivations; the endpoint carries the use case mark. The claim reader's namespace is imported in every endpoints file, so it sits beside the endpoint contract. A use case stating `generated: false` keeps its hand-written endpoint: uploads, downloads, and sign-in redirects. A use case the endpoint vocabulary cannot express, such as a file or list input, renders nothing, and the command lists it as skipped with the reason.
- `messages` writes, per use case, one C# file in the use case `Application/` folder with an internal sealed partial record named for the use case and its kind: `<UseCase>Command` or `<UseCase>Query`, with one positional property per input in written order mapped through the record type table, the members the record implies (the idempotency key a keyed command computes from its key template, the cursor or offset with the limit a paged query carries, and the tenant key when the tenancy selection names one), the LiteBus message interface the target names over its result, and a row record beside it for each multi-field list input. Required inputs ride non-nullable and optional caller inputs nullable. A hand-written sibling file may add members to any generated type. A use case with an input of no known shape, or a key template naming an input the message does not carry, renders no file, and the record check reports the input.
- `results` writes, per use case declaring an output, one C# file in the use case `Application/` folder with an internal sealed partial `<UseCase>CommandResult` or `<UseCase>QueryResult` record from its output: one positional property per field, a nested record per group when the output holds several groups, a row record beside it for each multi-field list, and the next cursor when the query pages. A hand-written sibling file may add members to any generated type. A use case with an output of no known shape renders no file, and the record check reports the field.

Every file opens with a header line saying `litespec generate` wrote it from the records, the LiteSpec version that wrote it, and that it must not be edited. Rendering is deterministic: modules, files, and entries order by identifier, so two runs write byte-identical files. A target with no entries writes no files.

For a .NET stack the `path` may hold `{module}` for the record module name or `{Module}` for the same name in PascalCase, and each module writes its file into its own resolved folder. Without a placeholder each module writes into `<path>/<Module>/`. A `knowledge-sets` target holds `{area}` and `{variant}` (or `{Area}` and `{Variant}`) instead, and each area and variant writes its files into its own resolved folder; without a placeholder each area and variant writes into `<path>/<Area>.<Variant>/`. An `exceptions`, `domain-events` or `module-events` target holds `{aggregate}` or `{Aggregate}` for the aggregate folder in both its path and its namespace, and each aggregate writes its files into its own resolved folder; a module event writes into an `On{Fact}` folder under it. A `validators` target writes each validator into the `Application/` folder of its use case under the resolved path. A `messages` or `results` target holds `{aggregate}` or `{Aggregate}` for the aggregate folder and `{useCase}` or `{UseCase}` for the use-case folder in both its path and its namespace, and each use case writes its files into its own resolved folder; a module-level use case drops the aggregate segment. An event, `messages`, or `results` target lists in `usings` the C# namespaces its files import for referenced identifier and value types; a `validators` target lists the namespaces of messages declared outside their use case folder. The `namespace` names the C# namespace with the same placeholders. For a TypeScript stack the target writes its one file directly into `path`, which holds no placeholder.

A `declarations` target names the policy vocabulary its project's guard evaluates: `permissionPolicy` turns a permission grant into a policy name with `{permission}` for the permission id or `{name}` for the name after the prefix, `signedInPolicy`, `anonymousPolicy`, and `systemPolicy` name the policy for each grant, and `selfPolicy`, `recentSignInPolicy`, and `runsAsPolicy` wrap a policy with `{policy}` for the wrapped policy and `{window}` for the sign-in window. Each form is optional; an unset form reads the default the .NET application page documents. A use case whose tenant is not `own`, whose grant names no known form, whose access states an authorization condition, or whose audit level `none` carries no exemption rationale renders nothing: the command lists it as `skipped <use-case id>: <reason>`, and a module whose every use case skips writes no file.

An `endpoints` target names in `claimReader` the qualified static method reading an actor identity, as `Namespace.Type.Method`, taking the caller and the actor name the record states. The method answers the identity the message carries for a `claim` input, so every `claim` input is an `id`. The target lists in `usings` the C# namespaces its files import for referenced identifier, closed-set, and response types. A use case carrying a file or list input, a multi-valued input, a choice naming no closed set, a non-identity claim input, or a money input taking its currency from another aggregate renders nothing: the command lists it as `skipped <use-case id>: <reason>`, and a module whose every use case skips writes no file. A use case stating `generated: false` is left out silently.

A `messages` target names in `commandInterface` and `queryInterface` the LiteBus message interfaces its commands and queries implement over their results; a message implements none when the target names none. A `messages` or `results` target renders the use cases of the module, or with `visibility: public` the use cases another module reaches as public records, with the public result named without the kind (`<UseCase>Result`), so a project pairs an internal target writing into `Application/` with a public target writing into the Contracts project.

With `--check` the command writes nothing, lists each file it would write or rewrite, and exits 1 when any would change. Skipped use cases list in either mode.

When a record file does not parse, the command writes nothing, names each invalid file, and exits 2, so a generated file is never quietly short. When a target names an unsupported kind or stack combination, the command writes nothing and names each invalid target. When a target writes inside another tool's output folder or two targets write the same file, the command writes nothing, names each offending target, and exits 2.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--check` | off | Writes nothing, lists each file that would change, and exits 1 when any would. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The files are written, or `--check` found every file current. |
| `1` | A file would change with `--check`, a target is invalid, the records cannot be read, or a file cannot be written. |
| `2` | The arguments are wrong, the root holds no project file, a record file does not parse, or a generate target writes inside another tool's output folder or the same file as another target. |

## Examples

Write the code the records determine:

```bash
litespec generate --root ../consumer
```

List the files that would change, without writing:

```bash
litespec generate --root ../consumer --check
```

## Underneath

The command opens the consumer through the same root walk `litespec model check` uses, builds the project model, and renders every configured target in memory. `litespec model check` renders the same plan and reports each missing or differing file as a `generated-drift` finding, so the check fails exactly when `generate` would write. Nothing beneath this command is separately runnable.
