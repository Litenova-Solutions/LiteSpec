# Serve the Book Live and Export It

The book is the model made readable, and every reader should open it current. This decision serves the book live from memory by default and writes a static book only on request, safe to host anywhere.

## Context

`litespec book` built the model in memory and wrote a static site to a git-ignored folder. Every reader held a copy from whenever they last ran the command, and an author editing records reran it after every change. One consumer's book took 16 seconds to build for 1193 records.

The written site was also unsafe to host. Its manifest carried absolute local paths in `roots`, including the install path with the author's user name, and every source link worked only on the author's machine.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Live by default; --out writes the static book | Always current; no generated folder; safe export. | Scripts add --out; readers learn the server. |
| Keep writing the static book | Every script keeps working. | Stale copies; rebuilds; leaked paths. |
| Serve live only, no export | One mode to run. | No published book for tool-less readers. |

## Decision

`litespec book` with no `--out` serves the book from a local server and writes nothing; `litespec book --out <folder>` writes the static book. The server uses the HTTP listener built into .NET, binds to localhost only, and serves only the app and the named data shards. Live and export use one data format with the same file names; the server holds the shards in memory and adds one change notification.

Export drops `roots`, takes its source links from a repository template, and records the release and build time. `--editor` becomes `--links`, with a `{commit}` placeholder filled from the checked-out commit; live defaults to the VS Code template and export defaults to `none`. The command page warns what a hosted book reveals, and no filter switch is added.

Rebuild watches the records, plus the code roots and test results when the project claims Implementation or Proof. It waits for a pause in edits, rebuilds in the background, keeps serving the last good model, and sends the change notification when the new one is ready. A build that fails keeps the last good model and shows the error in the book.

## Consequences

A script that reads the written folder adds `--out <folder>`, and `--editor` becomes `--links`. The consumer template drops the book line from its gitignore, since nothing writes there by default. Over HTTP the app subscribes to the change notification, reloads the manifest and the open record, and shows the build time; over file URLs it behaves as before.

## Evidence

[research/book-outline-evidence](research/book-outline-evidence) found the book teaching a second hierarchy beside the model's own, which one shared data format answers. [research/native-command-line-evidence](research/native-command-line-evidence) proved the native tool runs the model read the live server rebuilds. The 16 seconds and the leaked paths were measured from a live book with high confidence.

## Revisit Conditions

This decision reopens when the live server cannot stay solid. It also reopens when readers need a book the server cannot serve.
