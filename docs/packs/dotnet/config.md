# .NET Configuration

**Stack:** dotnet

**Concern:** workspace

This page states how a .NET backend pins its SDK, build settings, package versions, and local tools, and how a keyed setting binds to one options class. It is for developers who add or change .NET projects, packages, tools, or settings. Fixed paths and single homes keep each machine and CI on the same build, and a spelled-out section name catches a renamed setting before the product starts on a default.

## Concepts

### The Four Pinning Files

Four files at fixed paths decide how the backend builds on each machine. The root `global.json` names the lowest SDK that builds the workspace, with `latestFeature` roll-forward inside the same major release.

`apps/api/Directory.Build.props` sets the shared target framework, nullable types, implicit usings, and warnings as errors. `apps/api/Directory.Packages.props` pins each NuGet version in a single place, while project files name packages without versions.

The root `.config/dotnet-tools.json` pins each local tool that documented commands invoke, including the Stryker.NET runner. One version per tool keeps local runs and CI runs on the same behavior.

### How a Setting Reaches Code

A keyed setting from the records becomes a typed value through a single options class. The class carries an `implements` mark naming the setting, plus a `SectionName` constant and property spelling the setting's `configurationPath`. Claimwick's `SpendingOptions` shows the shape: `SectionName = "Spending"` with an `ApprovalReminderDelay` property. A renamed section or a typo shows up as a mismatch between the class and the record path.

## Standards

### Bind Every Keyed Setting in One Options Class

**ID:** `litespec/packs/dotnet/config#bind-every-keyed-setting-in-one-options-class`

**Strength:** required

**Statement:** An options class that binds a setting with a `configurationPath` MUST carry an `implements` mark naming the setting, with a `SectionName` constant and property that spell the setting's `configurationPath`.

**Rationale:** The options class turns configuration into a typed value the code reads. The mark names the promise the class holds, and the section name plus property spell the record path. Comparing the two finds a renamed section or a typo before the product starts on a default. This provision binds `litespec/core/use-cases#bind-every-keyed-setting-in-one-marked-element` for .NET.

**Example:** Claimwick's `SpendingOptions` class declares `SectionName = "Spending"`, an `ApprovalReminderDelay` property, and `/// <implements>setting/spending.approval-reminder-delay</implements>`.

**Checked by:** `litespec model check` reports `setting-unimplemented` (level S) for a keyed setting no options class marks, and `setting-key-drift` (level S) for a marked options class whose `SectionName` constant and property do not spell the setting's `configurationPath`.

## Conventions

None.
