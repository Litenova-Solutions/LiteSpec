# LiteSpec.Cli.Tests

The test suite for the `litespec` command line. Each test proves the exit code and the printed text together, in this process through the CLI's own dispatch by default.

## Golden Files for Generators

Each generator renderer ships with golden tests: expected output files under `Generate/Goldens/`, one per renderer and stack, compared byte for byte against what the renderer writes for the small fixture's records.

A golden holds `__LITESPEC_VERSION__` where the header stamps the LiteSpec version, because the version varies by build. The `GenerateGoldens` helper replaces it with the carried release before comparing.

To add a renderer:

1. Add its golden files under `Generate/Goldens/`, named `<kind>-<stack>.txt`, with the version placeholder in the header.
2. Add a test to `GenerateGoldenTests` that builds the small fixture, renders through `GeneratePlan`, and asserts with `GenerateGoldens.AssertEqual`.
3. Run the new test class and fix what it reports.

## Layout

| Folder | What it holds |
|:---|:---|
| `Generate/` | Generator renderer, command, and golden tests with their expected files. |
| `Model/` | Model check, reader, and fixture tests. |
| `Fixtures/` | Consumer repositories the CLI reads, not test code. |
| `Support/` | The CLI runner, temp folders, and record builders the tests share. |
