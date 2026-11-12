# Record Folders

## Intent

Use this generated page to find the folder and file each record type lives in, relative to `.litespec/`. A record in any other folder is a `record-placement-invalid` finding. The Flat module column marks a home that applies only to a module that keeps its records flat. Folders carry ownership and layer only, meaning product or module. Categories live in fields and generated indexes, because a record often fits several categories and a folder per category forces one and churns history on every recategorization. Decision numbers stay identity and proposal order only.

## Reference

| Type | Folder | File | Flat module |
|:---|:---|:---|:---|
| `product` | `intent` | `product.yaml` | - |
| `goal` | `intent/goals` | `{name}.yaml` | - |
| `goal` | `intent/modules/{module}/goals` | `{name}.yaml` | - |
| `problem` | `intent/problems` | `{name}.yaml` | - |
| `problem` | `intent/modules/{module}/problems` | `{name}.yaml` | - |
| `feature` | `intent/modules/{module}/features` | `{name}.yaml` | - |
| `requirement` | `intent/modules/{module}/features` | `*` | - |
| `constraint` | `intent/constraints` | `{name}.yaml` | - |
| `constraint` | `intent/modules/{module}/constraints` | `{name}.yaml` | - |
| `decision` | `intent/decisions` | `{name}.yaml` | - |
| `decision` | `intent/modules/{module}/decisions` | `{name}.yaml` | - |
| `decision` | `implementation/decisions` | `{name}.yaml` | - |
| `decision` | `implementation/modules/{module}/decisions` | `{name}.yaml` | - |
| `research` | `intent/research` | `{name}.yaml` | - |
| `research` | `intent/modules/{module}/research` | `{name}.yaml` | - |
| `journey` | `intent/journeys` | `{name}.yaml` | - |
| `actor` | `intent/actors` | `{name}.yaml` | - |
| `role` | `intent/roles` | `{name}.yaml` | - |
| `module` | `intent/modules/{name}` | `module.yaml` | - |
| `term` | `` | `glossary.yaml` | - |
| `aggregate` | `behavior/modules/{module}/{aggregate}` | `aggregate.yaml` | - |
| `aggregate` | `behavior/modules/{module}` | `aggregate.yaml` | yes |
| `use-case` | `behavior/modules/{module}/{aggregate}` | `*` | - |
| `use-case` | `behavior/modules/{module}/use-cases` | `{name}.yaml` | - |
| `use-case` | `behavior/modules/{module}` | `*` | yes |
| `result` | `behavior/modules/{module}/{aggregate}` | `*` | - |
| `result` | `behavior/modules/{module}/use-cases` | `*` | - |
| `result` | `behavior/modules/{module}` | `*` | yes |
| `scenario` | `behavior/modules/{module}/{aggregate}` | `*` | - |
| `scenario` | `behavior/modules/{module}/use-cases` | `*` | - |
| `scenario` | `behavior/modules/{module}` | `*` | yes |
| `event` | `behavior/modules/{module}/{aggregate}` | `aggregate.yaml` | - |
| `event` | `behavior/modules/{module}/{aggregate}/events` | `*` | - |
| `event` | `behavior/modules/{module}` | `aggregate.yaml` | yes |
| `event` | `behavior/modules/{module}/events` | `*` | yes |
| `rule` | `behavior/rules` | `*` | - |
| `rule` | `behavior/modules/{module}/rules` | `*` | - |
| `rule` | `behavior/modules/{module}/{aggregate}/rules` | `*` | - |
| `reaction` | `behavior/modules/{module}/reactions` | `{name}.yaml` | - |
| `job` | `behavior/modules/{module}/jobs` | `{name}.yaml` | - |
| `notification` | `behavior/modules/{module}/notifications` | `{name}.yaml` | - |
| `setting` | `behavior/modules/{module}/settings` | `*` | - |
| `integration-event` | `behavior/modules/{module}/integration-events` | `{name}.yaml` | - |
| `permission` | `behavior/modules/{module}` | `permissions.yaml` | - |
| `entry-point` | `behavior/entry-points/{entry-point}` | `entry-point.yaml` | - |
| `screen` | `behavior/entry-points/{entry-point}/screens` | `{home}.{name}.yaml` | - |
| `external-system` | `behavior/external-systems` | `{name}.yaml` | - |
| `external-system` | `behavior/modules/{module}/external-systems` | `{name}.yaml` | - |
| `runbook` | `implementation/runbooks` | `{name}.yaml` | - |
| `runbook` | `implementation/modules/{module}/runbooks` | `{name}.yaml` | - |
| `departure` | `governance/departures` | `{name}.yaml` | - |
| `departure` | `governance/modules/{module}/departures` | `{name}.yaml` | - |

## Notes

The `litespec-maintain provisions` command writes this page from the record folder table in the model, and `litespec-maintain provisions --check` fails when it differs. Edit the table, not the page.
