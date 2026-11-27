# Verify Production Smoke

## Name

`litespec verify production-smoke` boots the production containers and probes them.

## Synopsis

```bash
litespec verify production-smoke [--root <consumer>] [--base-url <url>]
```

## Description

The command reads the `production` block of the consumer `verify` block and runs the production smoke against it. The block names the compose file, the env file, the base address, the readiness path, and the smoke paths:

```yaml
verify:
  production:
    compose: deploy/compose.yaml
    envFile: .env.example
    baseUrl: http://localhost:8080
    migrate: docker compose -f deploy/compose.yaml run --rm migrate
    health: /health/ready
    paths: [/api, /auth]
```

The smoke runs `docker compose -f <compose> --env-file <envFile> up -d --build`, then the `migrate` command when the block names one, then polls `baseUrl + health` for up to 120 seconds. A host that never answers 2xx is reported as `production-readiness-failed`. Once the host is ready, the smoke requests each path with `X-Forwarded-Proto: https`; a path that answers 5xx or gives no answer is reported as `path-failed`. A 4xx answer is not a failure: the smoke proves the host answers behind the reverse proxy, not that every path exists.

The smoke reads the readiness body and reports `stub-adapter-shown` for each entry of a top-level `stubAdapters` array, so a stand-in that serves production traffic is named. For a Claimwick expense host whose readiness body lists `"stubAdapters": ["mail"]`, the smoke exits 1 and names the `mail` adapter.

The down command `docker compose -f <compose> --env-file <envFile> down -v` always runs, also after a failure, so a smoke never leaves containers behind.

With `--base-url` the smoke probes the given address instead of the block address. The command writes one JSON report to `.litespec/verify/production-smoke.json` with the probed address and the findings.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <consumer>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--base-url <url>` | the verify block address | The production address to probe. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:---|:---|
| `0` | The smoke raised no finding. |
| `1` | The smoke raised a finding or could not run. |
| `2` | The arguments are wrong, the root holds no project file, or the verify block names no production. |

## Examples

Run the smoke from the consumer root:

```bash
litespec verify production-smoke --root ../consumer
```

Probe a host that already runs on another address:

```bash
litespec verify production-smoke --root ../consumer --base-url http://localhost:8080
```

## Underneath

The command runs the container commands in the consumer root through one process runner, polls the readiness path every two seconds, and prints each finding once in the model check format. The JSON report holds the same findings with their codes, subjects, and messages. Nothing beneath this command is separately runnable.
