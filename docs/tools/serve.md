# Serve

## Name

`litespec serve` runs a stdio MCP server over the warm project model: it loads the model once, keeps it warm, and answers the four MCP tools agents use instead of shelling out to `model check` and `rules` on every question.

## Synopsis

```bash
litespec serve [--root <consumer>]
```

## Description

The server speaks JSON-RPC 2.0 on standard input and output, one message per line. It answers `initialize`, `notifications/initialized`, `tools/list`, `tools/call`, and `ping`. A request carries an `id` and gets a response with the same `id`; a notification carries no `id` and gets no answer. An unknown method or tool returns a JSON-RPC error: `-32601` for an unknown name, `-32602` for a missing name, `-32600` for a message that is not a request, and `-32700` for a line that is not JSON.

The server builds the project model once at startup, then watches `.litespec/` and the code roots with a `FileSystemWatcher`. After a change settles for 300 ms, the next tool call rebuilds the model and recomputes the open findings; until then the tools answer from cache. A failed reload keeps the last good model, so the session keeps answering.

`litespec serve` owns the YAML graph and the spec-to-code parity checks. It never reports C# compiler diagnostics: Fuse owns those, and the server duplicates none of them.

The four tools are:

- `litespec_task_packet(target)`: the record or records the target implements, their fields, the judgment provisions `rules` returns for the path, and the neighbouring files. The target is a file path under the consumer root or a record id. Checked provisions are left to the checks: the packet names how many apply and says `model check` enforces them. The packet is capped at 10,000 characters, about 2,500 tokens; a longer packet is cut with the cut stated.
- `litespec_check(paths?)`: the open findings for the given file or folder paths, or for every path when `paths` is omitted, from the warm model.
- `litespec_impact(record_id)`: the records, code and tests linked to the record in both directions: outgoing and incoming record links, implementing and followed code paths, proving tests, and calling screens.
- `litespec_verify_slice(use_case_id)`: the use case's open findings, its scenarios with proof state, and its code and test files.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The session ended at end of input. |
| `2` | The arguments are wrong, the root or the project file is missing or invalid, the model cannot be built, or the carried release cannot be read. |

## Examples

Start the server for the consumer and send it one tools/list call:

```bash
litespec serve --root ../consumer
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{}}}
{"jsonrpc":"2.0","method":"notifications/initialized"}
{"jsonrpc":"2.0","id":2,"method":"tools/list"}
```

Read the packet for a handler before editing it:

```json
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"litespec_task_packet","arguments":{"target":"apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy/BorrowCopyCommandHandler.cs"}}}
```

Read the open findings for the touched paths:

```json
{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"litespec_check","arguments":{"paths":["apps/api/src/Modules/Library.Lending/Application/Loans/BorrowCopy"]}}}
```

Read what a record reaches before renaming it:

```json
{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"litespec_impact","arguments":{"record_id":"use-case/lending.borrow-copy"}}}
```

Verify one use case after the change:

```json
{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"litespec_verify_slice","arguments":{"use_case_id":"use-case/lending.borrow-copy"}}}
```

Add the server to `.mcp.json` in the consumer root so Claude Code starts it beside the project:

```json
{
  "mcpServers": {
    "litespec": {
      "command": "litespec",
      "args": ["serve", "--root", "${workspaceFolder}"]
    }
  }
}
```

Claude Code runs the server with the consumer root as its working directory, so `"args": ["serve"]` reads the same. With the local .NET tool, the command is `dotnet` and the first two arguments are `litespec` and `serve`.

## Underneath

The warm model is the built project model from the model builder with the open findings `model check` collects, minus the JSON envelope. The task packet resolves its target the way `rules` resolves its own: a record id reads the record, a file path reads the records its implements marks name. The judgment provisions come from the same retrieval `rules` prints, filtered to the provisions that need judgment. The neighbours are the files beside the packet records, at most 20 in path order.

Nothing beneath this command is separately runnable.
