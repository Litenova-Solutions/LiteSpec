# Record Folders

## Intent

Use this generated page to find the folder and file each record type lives in, relative to `.litespec/`. A named record sits in a folder that carries its name, beside its explanation, so `<name>/<name>.yaml` and `<name>/<name>.md` are the pair. A record in any other folder is a `record-placement-invalid` finding, a folder whose name differs from its file stem is a `record-folder-drift` finding. The Flat module column marks a home that applies only to a module that keeps its records flat. Folders carry ownership and layer only, meaning product or module. Categories live in fields and generated indexes, because a record often fits several categories and a folder per category forces one and churns history on every recategorization. Decision numbers stay identity and proposal order only.

## Reference

| Type | Folder | File | Flat module |
|:---|:---|:---|:---|
| `product` | `intent` | `product.yaml` | - |
| `goal` | `intent/goals/{name}` | `{name}.yaml` | - |
| `vision` | `intent/visions/{name}` | `{name}.yaml` | - |
| `problem` | `intent/problems/{name}` | `{name}.yaml` | - |
| `feature` | `intent/problems/{problem}/features/{name}` | `{name}.yaml` | - |
| `requirement` | `intent/problems/{problem}/features/{feature}/{name}` | `{name}.yaml` | - |
| `constraint` | `intent/constraints/{name}` | `{name}.yaml` | - |
| `decision` | `intent/decisions/{name}` | `{name}.yaml` | - |
| `decision` | `implementation/decisions/{name}` | `{name}.yaml` | - |
| `decision` | `implementation/modules/{module}/decisions/{name}` | `{name}.yaml` | - |
| `research` | `intent/research/{name}` | `{name}.yaml` | - |
| `journey` | `intent/journeys/{name}` | `{name}.yaml` | - |
| `actor` | `intent/actors/{name}` | `{name}.yaml` | - |
| `role` | `intent/roles/{name}` | `{name}.yaml` | - |
| `module` | `behavior/modules/{name}` | `module.yaml` | - |
| `term` | `` | `glossary.yaml` | - |
| `aggregate` | `behavior/modules/{module}/{aggregate}` | `aggregate.yaml` | - |
| `aggregate` | `behavior/modules/{module}` | `aggregate.yaml` | yes |
| `use-case` | `behavior/modules/{module}/{aggregate}/{name}` | `{name}.yaml` | - |
| `use-case` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` | - |
| `use-case` | `behavior/modules/{module}` | `*` | yes |
| `result` | `behavior/modules/{module}/{aggregate}/{name}` | `{name}.yaml` | - |
| `result` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` | - |
| `result` | `behavior/modules/{module}` | `*` | yes |
| `scenario` | `behavior/modules/{module}/{aggregate}/{name}` | `{name}.yaml` | - |
| `scenario` | `behavior/modules/{module}/use-cases/{name}` | `{name}.yaml` | - |
| `scenario` | `behavior/modules/{module}` | `*` | yes |
| `event` | `behavior/modules/{module}/{aggregate}` | `aggregate.yaml` | - |
| `event` | `behavior/modules/{module}/{aggregate}/events` | `*` | - |
| `event` | `behavior/modules/{module}` | `aggregate.yaml` | yes |
| `event` | `behavior/modules/{module}/events` | `*` | yes |
| `rule` | `behavior/rules` | `*` | - |
| `rule` | `behavior/modules/{module}/rules` | `*` | - |
| `rule` | `behavior/modules/{module}/{aggregate}/rules` | `*` | - |
| `reaction` | `behavior/modules/{module}/reactions/{name}` | `{name}.yaml` | - |
| `job` | `behavior/modules/{module}/jobs/{name}` | `{name}.yaml` | - |
| `notification` | `behavior/modules/{module}/notifications/{name}` | `{name}.yaml` | - |
| `setting` | `behavior/modules/{module}/settings` | `*` | - |
| `integration-event` | `behavior/modules/{module}/integration-events/{name}` | `{name}.yaml` | - |
| `permission` | `behavior/modules/{module}` | `permissions.yaml` | - |
| `entry-point` | `behavior/entry-points/{entry-point}` | `entry-point.yaml` | - |
| `screen` | `behavior/entry-points/{entry-point}/screens/{home}.{name}` | `{home}.{name}.yaml` | - |
| `external-system` | `behavior/external-systems/{name}` | `{name}.yaml` | - |
| `external-system` | `behavior/modules/{module}/external-systems/{name}` | `{name}.yaml` | - |
| `runbook` | `implementation/runbooks/{name}` | `{name}.yaml` | - |
| `runbook` | `implementation/modules/{module}/runbooks/{name}` | `{name}.yaml` | - |
| `departure` | `governance/departures/{name}` | `{name}.yaml` | - |
| `departure` | `governance/modules/{module}/departures/{name}` | `{name}.yaml` | - |

## Notes

The `litespec-maintain provisions` command writes this page from the record folder table in the model, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.
