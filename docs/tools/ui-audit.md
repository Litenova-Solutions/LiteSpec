# UI Audit

## Name

`litespec ui audit` visits each route of a running application in a browser and reports what the rendered pages show wrong.

## Synopsis

```bash
litespec ui audit --base-url <url> [--root <folder>] [--routes <file>] [--widths <list>] [--schemes <list>] [--storage-state <file>] [--out <dir>] [--fail-on blocking|none] [--primary-selector <css>] [--browser <path>]
```

## Description

The model check reads records and code, but some defects only show in the rendered page: a surface drawn inside another, a field on the page background, a button group of mixed sizes, a raw code where a name belongs. The audit visits each route at each width in each color scheme, runs one audit script after the page loads and settles, and writes a screenshot per page with a JSON report of the findings. Run it against a running build of the application before reporting a screen done.

Without `--routes`, the routes come from the screen records: the `route` of every screen, skipping routes with parameters (a path holding `{`, `}`, `[`, `]`, `:`, or `*`, which the audit cannot fill). With `--routes`, the routes come from the named file instead, a YAML or JSON list of entries with a `path` and an optional `name`:

```yaml
- path: /expenses
  name: Expense list
- path: /expenses/new
```

Each route loads at each width with each scheme emulated, so the defaults visit every route six times. Widths default to 390, 1440, and 2560 pixels (phone, laptop, and wide) at a viewport height of 900 pixels; schemes default to light and dark through color-scheme emulation. `--storage-state` takes a Playwright storage state file, so signed-in pages audit in their signed-in state.

Each finding carries a code, the subject (the screen id, or the routes file name), and one present-tense fact. One finding per code, route, scheme, selector, and message prints, with the widths that showed it merged into one entry:

```text
/expenses [light 390,1440] [surface-nesting-invalid] A surface inside another surface draws its own border. (section > div:nth-of-type(2))
```

The audit codes are:

| Code | Meaning | Fix |
|:---|:---|:---|
| `surface-nesting-invalid` | A surface inside another surface draws its own border and fill. | Draw the inner content with no surface of its own; separate regions with spacing or a divider. |
| `surface-contrast-failed` | A table header, a marked surface, or a padded rounded box shares its fill with the surface behind it and draws no border. | Give the surface its layer fill or its accent fill, or draw a border of its own. |
| `field-surface-missing` | A field sits on the page background. | Move the field into a section, a dialog, or a side sheet. |
| `field-width-invalid` | A short field (a code, a date, or a number) spans the form. | Size the field to its value, and place short related fields on one row. |
| `action-group-invalid` | Actions in one group have different sizes, or one action sits alone on the last row of a wrapped group. | Give the group one size on one row, and keep a wrap from stranding one action. |
| `primary-action-duplicate` | The view shows more than one primary action. | Keep one primary action per view. |
| `column-alignment-invalid` | A column whose body cells mostly parse as numbers or money aligns to the start. | Align number and amount columns to the end with tabular figures. |
| `text-code-invalid` | The page shows a raw identifier, a message key, `null`, `undefined`, or `(null)` where a name belongs. | Show the name the reader knows; never show the code. |
| `tab-label-invalid` | A tab label wraps, or the tab list wraps to two rows. | Shorten the labels and keep the tab list to one row. |
| `table-clip-invalid` | Below 600 pixels, a table is wider than its container and no ancestor up to `main` scrolls horizontally. | Let the table scroll inside its own region. |
| `page-exception-unhandled` | The page throws an exception it never catches while it loads. | Fix the code that throws, or catch the failure and show its problem state. |
| `page-request-failed` | A fetch or XHR request the page makes to its own origin answers 5xx or fails at the network level while the page loads and settles. | Fix the endpoint the finding names, or catch the failure and show its problem state. |
| `page-error-shown` | A visible `role="alert"` element holds text once the page settles. | Fix the failure the alert reports. |

The primary action is read with `--primary-selector`, which defaults to `[data-variant=primary]`. The command prints one line per finding, then the finding count and the report path.

`audit.json` in the output folder holds the base URL, the routes, the widths, the schemes, every finding, and the screenshot file names. Screenshots follow the capture pattern `{route}--{scheme}--{width}.png`, with the route slugged and the root route named `home`, so `expenses-new--light--390.png` shows `/expenses/new` at phone width in the light scheme.

The audit also writes the route-suite check result beside the report: `route-suite.json` under the project's `paths.checkResults` folder (`.litespec/proof/checks/` by default), holding `check`, `passed`, and `value` as `schemas/check-result.schema.yaml` states.

`passed` is true when the audit reports no findings across every route it visited; `--fail-on none` keeps the exit code zero but still records `passed` false when findings exist. The model check reads the file like any other check result, so a passing audit run measures every requirement naming `route-suite`.

## Arguments

None.

## Options

| Option | Default | Effect |
|:---|:---|:---|
| `--root <folder>` | the working directory | The folder to start from. The command uses the first folder at or above it that holds `.litespec/project.yaml`. |
| `--base-url <url>` | none, required | The running application to visit, such as `http://localhost:4173`. |
| `--routes <file>` | the screen records | A YAML or JSON file listing the routes: entries with a `path` and an optional `name`. |
| `--widths <list>` | `390,1440,2560` | Comma list of viewport widths in pixels. |
| `--schemes <list>` | `light,dark` | Comma list of `light` and `dark`, run through color-scheme emulation. |
| `--storage-state <file>` | none | A Playwright storage state file for signed-in pages. |
| `--out <dir>` | `ui-audit` in the working directory | The folder receiving the screenshots and `audit.json`. |
| `--fail-on <mode>` | `blocking` | One of `blocking` or `none`: exit 1 on any finding, or always pass. |
| `--primary-selector <css>` | `[data-variant=primary]` | The CSS selector marking the primary action. |
| `--browser <path>` | the installed browser | The browser executable to audit with. Without it, the audit uses the installed Chrome, then Edge, then Chromium. |
| `--help` | off | Prints the usage text and exits 0. |

## Exit Codes

| Code | Meaning |
|:--|:--|
| `0` | The audit ran with no finding, `--fail-on none` was passed, or no routes were found. |
| `1` | A finding was reported with `--fail-on blocking`, no browser was available, or a page could not be audited. |
| `2` | The arguments are wrong, a named file is missing or invalid, or the root or the project file is missing or invalid. |

## Examples

Audit every screen route of the running application:

```bash
litespec ui audit --root ../consumer --base-url http://localhost:4173
```

Audit one route file at laptop width only, keeping the report beside the build:

```bash
litespec ui audit --root ../consumer --base-url http://localhost:4173 --routes routes.yaml --widths 1440 --out ../consumer/ui-audit
```

Audit signed-in pages with a stored session, without failing the run:

```bash
litespec ui audit --root ../consumer --base-url http://localhost:4173 --storage-state storage.json --fail-on none
```

## Underneath

The command finds the browser as Chrome, then Edge, then Chromium, or takes the `--browser` executable, reporting when none is available. Install Google Chrome, Microsoft Edge, or Chromium, or pass `--browser <path>`. On Windows the search covers Program Files and LocalAppData, on macOS /Applications, and on Linux the PATH.

Each page loads in a fresh target with its viewport and emulated scheme until the load event plus a quiet network, the embedded audit script reads the computed styles and text back as findings, and a full-page screenshot lands in the output folder. The report merges identical findings across widths and writes `audit.json` beside the screenshots. The command then writes `route-suite.json` into `paths.checkResults` with the run outcome. Nothing beneath this command is separately runnable.
