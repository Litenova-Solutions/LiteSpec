# Deck Sources

The sources of the three decks in `D:/docs/` (built 2026-10-30 and 2026-10-31) and of the Lantern example in `D:/docs/lantern-example/`. Wave 6 (spec Part F) rebuilds them with the new names.

| File | Role |
|:---|:---|
| `head.html`, `foot.html` | Shared styles, navigation, slide index, notes, theme |
| `intent-teach.html`, `behavior-teach.html`, `full-teach.html` | The teaching slides of each deck |
| `findings-*.js` | The review findings, one array per deck: severity, category, title, evidence, fix |
| `review.js` | Turns a findings file into the review appendix slides |
| `close-*.html` | The proposal and recommendation slides that end each deck |
| `provgrid.js` | Generates the Behavior provision grids from the five Behavior pages under `docs/core/` (domain model, use cases, automation, edges, privacy) into `provgrid.html`, split over three slides |
| `build.js` | Joins the parts into one self-contained HTML file |
| `lantern-gen.js` | Writes the Lantern example project |
| `serve.js`, `checkall.js` | A local server and a Playwright overflow check |
| `fields.js` | Lists every field in `schemas/record.schema.json` with its shape and owners |

Build, from this folder:

```bash
node provgrid.js <litespec>/docs/core > provgrid.html
node review.js findings-intent.js intent "Intent Layer" 6 > review-intent.html
node review.js findings-behavior.js behavior "Behavior Layer" 6 > review-behavior.html
node review.js findings-full.js full "Layer Connections" 6 > review-full.html
node build.js "LiteSpec Intent Layer" 1-intent-layer.html intent-teach.html review-intent.html close-intent.html
node build.js "LiteSpec Behavior Layer" 2-behavior-layer.html behavior-teach.html review-behavior.html close-behavior.html
node build.js "LiteSpec From Promise to Proof" 3-promise-to-proof.html full-teach.html review-full.html close-full.html
```

Check: `node serve.js .` (port 8791), then `playwright-cli open about:blank` and `playwright-cli --raw run-code --filename=checkall.js` (edit its file list). `lantern-gen.js` writes beside itself and reads the consumer project template from `D:/Personal-Projects/LiteSpec/templates/consumer/`; validate with `LITESPEC_HOME=<litespec> litespec consumer` and `litespec model check` in the generated folder.
