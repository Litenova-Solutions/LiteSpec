# UI Review

## Name

`litespec ui review` lists the screen routes whose files changed since a revision, pairs each with its golden baseline PNGs when present, and writes a review packet an independent reviewer answers.

## Synopsis

```bash
litespec ui review --since <ref> [--root <folder>]
```

## Description

The audit visits rendered pages in a browser, but a reviewer first needs to know which routes changed and what good looks like for each. `ui review` answers both without calling the model or the network: it lists the screen routes whose files changed since the git revision and pairs each with its golden baseline PNGs under `.agents/ui-baseline/`, so the reviewer compares the changed route against the locked archetype.

The command reads the files changed since the revision: tracked files differing between the revision and the working tree, plus untracked files, minus the review outputs themselves. Every changed file maps to one route entry. A screen record that declares a `route:` line names its route; any other file guesses its route from its file name, lowercased with separators as dashes under the root slash. Each route pairs with the baseline PNGs whose file names start with the route slug (slashes as dashes, `home` for the root route), in file order. A route with no baseline still lists, marked as having no baseline PNG.

The command always writes a review packet to `.litespec/review/ui-<timestamp>.md`, even when no files changed. The packet holds the changed routes with their source files, the baselines per route, the geometry checklist the independent reviewer answers (edge alignment, control heights, symmetric grids, form width, nested surfaces, overflow), and each changed file's diff. Each route also prints to standard output with its baselines or its missing-baseline mark. The packet names the base revision and the generation time, so a later reader sees what the reviewer saw. The command takes its scope from options.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <folder>` | the working directory | The repository to read and to write the packet under. |
| `--since <ref>` | none, required | The git revision to diff the working tree against, such as `HEAD~1`. |
| `--help` | off | Print the usage text and exit 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | Every changed route has a baseline PNG. |
| `1` | A changed route has no baseline PNG. |
| `2` | The arguments are wrong, the root is missing, or the changes cannot be read. |

## Examples

List the changed routes with their baselines:

```bash
litespec ui review --since main
```

Check the change set from another root:

```bash
litespec ui review --since HEAD~1 --root ../consumer
```

## Underneath

The command reads the change set the same way `rules --since` does: the tracked files differing between the revision and the working tree with the untracked files, minus the review outputs themselves. It maps each changed file to one route entry and pairs each route with its baseline PNGs in file order. Nothing beneath this command is separately runnable.
