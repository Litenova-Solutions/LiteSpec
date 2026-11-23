# API Explore

## Name

`litespec api explore` walks a running API from its OpenAPI document and reports server faults.

## Synopsis

```bash
litespec api explore --base-url <url> [--openapi <path-or-url>] [--steps <n>] [--seed <n>] [--header "Name: value"]... [--cookie-file <file>] [--deny <method> <path-pattern>]... [--out <dir>] [--allow-delete] [--allow-remote]
```

## Description

The model check reads records and code, but some defects only show when the API runs: an unhandled exception on an unusual id, a refusal that is not problem details, a 500 on a boundary body. The walk makes valid-looking calls from the operations the OpenAPI document lists, reuses ids from earlier responses so later calls address real records, and reports every fault with the request that reproduces it. Run it against a local build with seeded data before reporting the API done.

Without `--openapi`, the document comes from `/openapi/v1.json` under the base URL. With `--openapi`, it comes from the named source instead: a file when a file exists at that path, an absolute URL when the value is one, or a path under the base URL otherwise.

The walk lists every operation the document holds except the OpenAPI document and health routes, which it never calls. It skips the deny list: by default nothing that deletes, signs out, or erases. That is every DELETE operation, every path holding `delete`, and every path holding `sign-out`, `signout`, `logout`, `log-out`, `erase`, or `erasure`. Each `--deny` adds one entry: a method, or `*` for every method, and a path substring, or `*` for every path. `--allow-delete` lifts the delete entries and walks DELETE operations and paths holding `delete`; the sign-out and erasure entries stay.

The walk calls a base URL whose host is loopback only: `localhost`, `127.x.x.x`, or `::1`. Any other host needs `--allow-remote`, because the walk sends state-changing calls and belongs against a local build.

Each call sends the `--header` headers and the `--cookie-file` cookies. A header is one `Name: value` pair; repeat `--header` for each header. The cookie file holds one `name=value` cookie per line, with blank lines and `#` comments ignored; the walk sends them as one `Cookie` header. The `Cookie` header itself comes from `--cookie-file`, never from `--header`.

Each fault carries its step number, which replays the call from the seed:

```text
Step 12: GET /expenses/e-42 answered 500. Detail: The list failed.
```

The fault kinds are:

| Kind | Meaning | Fix |
|:---|:---|:---|
| `server-error` | The call answered 5xx. | Fix the code that throws, or catch the failure and answer problem details. |
| `client-format` | The call answered 4xx with a body that is not problem details. | Answer the refusal as problem details. |
| `timeout` | The call timed out after 30 seconds. | Fix the slow path, or split the call. |

The command prints one line per fault, then the fault count and the report paths. `explore.json` in the output folder holds the base URL, the OpenAPI source, the seed, the calls, the operations listed, the skipped routes, the calls per status, and every fault with its request body, response excerpt, and problem detail. `explore.md` beside it reports the same run for readers, grouped by operation.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--base-url <url>` | none, required | The running API to walk, such as `http://localhost:5000`. |
| `--openapi <path-or-url>` | `/openapi/v1.json` | The OpenAPI document: a file, an absolute URL, or a path under the base URL. |
| `--steps <n>` | `200` | How many calls the walk makes. |
| `--seed <n>` | `1` | The seed that replays the same calls. |
| `--header "Name: value"` | none | A request header sent on every call. Repeat for each header. |
| `--cookie-file <file>` | none | A file with one `name=value` cookie per line, sent as the `Cookie` header. |
| `--deny <method> <path-pattern>` | the safe list | A method and a path pattern the walk never calls. Repeat for each entry. |
| `--out <dir>` | `.litespec/explore` in the working directory | The folder receiving `explore.md` and `explore.json`. |
| `--allow-delete` | off | Walk DELETE operations and paths holding `delete`. |
| `--allow-remote` | off | Walk a base URL that is not loopback. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The walk ran with no 5xx, or no callable operations were listed. |
| `1` | A 5xx was found, or the walk could not run. |
| `2` | The arguments are wrong, a named file is missing or invalid, or the base URL is not loopback without `--allow-remote`. |

## Examples

Walk the local API with the defaults:

```bash
litespec api explore --base-url http://localhost:5000
```

Walk a signed-in API for longer, skipping bulk reads beside the safe list:

```bash
litespec api explore --base-url http://localhost:5000 --header "Authorization: Bearer abc" --cookie-file cookies.txt --deny POST export --steps 1000 --out .litespec/explore-auth
```

Replay a reported fault from its seed:

```bash
litespec api explore --base-url http://localhost:5000 --seed 7 --steps 50
```

## Underneath

The command reads the OpenAPI document, resolves its schema, parameter, and request-body references, and lists the callable operations in document order. It first calls each parameterless GET once, collecting ids from the answers, then picks random operations for the remaining steps from `new Random(seed)`, so the same seed against the same data replays the same calls. Each call fills path parameters from the collected ids, builds its query string and JSON body from the resolved schemas with mostly valid-looking values and boundary values with a small probability, and sends the walk headers and cookies. One call may take 30 seconds; the client follows no redirects and keeps no cookie jar. The OpenAPI document and health routes are never walked. The reports land as `explore.md` and `explore.json` in the output folder. Nothing beneath this command is separately runnable.
