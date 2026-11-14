# Native Command Line

The `litespec` command line is the program that checks, queries and initializes a LiteSpec project. LiteSpec compiles it ahead of time with Native AOT, which turns the code into machine code before it runs, so each platform gets an executable that needs no .NET runtime. Consumers who install LiteSpec in a project, including client-only web projects, are the people this choice concerns.

## Context

The `litespec` command line ships as one framework-dependent .NET tool. Every consumer therefore needs the .NET 10 runtime to run it and the .NET SDK to install it. A client-only web project with no other .NET code installs the SDK only to run LiteSpec.

The CLI compiles with Native AOT once its JSON output stops relying on reflection, which is the runtime's way of reading type information while the program runs. In a trial, the compiled CLI wrote the same output bytes as the build on the .NET runtime. A full model check of the Claimwick example took 1.32 s, against 2.80 s on the runtime, as the mean of 8 runs on one Windows machine.

Each operating system builds its own executable. A glibc build runs only on a glibc at least as new as the machine that built it, and an Alpine machine needs a musl build of its own. Hashing through `System.Security.Cryptography` tied the Linux executable to an OpenSSL release that .NET loads, and a bare container image or Fedora 45 does not have that release.

## Options

LiteSpec compared 4 options for installing the CLI. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Native AOT packages per platform and standalone archives | One executable serves both installs, needs no .NET runtime and runs without the SDK. | Each operating system builds its own executable, CI gains a job per platform, and a browser download meets a macOS or Windows prompt. |
| Native AOT packages per platform only | Consumers keep one install path, and nothing downloads outside NuGet. | A consumer without the .NET SDK cannot install the CLI. |
| Keep the framework-dependent .NET tool | No build matrix and no packaging change. | Every consumer needs the .NET 10 runtime, and a client-only web project installs the SDK only to run LiteSpec. |
| Self-contained executables on the .NET runtime | Reflection keeps working, so no JSON code changes. | The executable keeps the start-up and compile cost of the runtime, and still needs one build per platform. |

## Decision

The CLI compiles ahead of time into one native executable for each of 8 platforms: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64` and `osx-arm64`. The `LiteSpec.Cli` package names one native package for each platform, and a framework-dependent package serves every other platform. So `dotnet tool install` and `dotnet tool restore` keep working.

Each GitHub release also carries a standalone archive for each platform, a `SHA256SUMS` file, a build provenance attestation for each file, and install scripts for Linux, macOS and Windows. A project uses one of the two installs.

## Consequences

The CLI builds every JSON document as a node tree. `dotnet build` rejects reflection-based serialization, so each output stays byte for byte the same in both builds.

`litespec init --install standalone` writes a project that runs the binary, and it pins the release in `reviewedLiteSpecVersion`. The Linux glibc builds link against glibc 2.28 on AlmaLinux 8. The CLI computes SHA-256 in managed code, so a Linux executable needs only the C library.

CI builds, installs and tests the executable on each platform it ships for, and it runs the executable on stock Linux distributions. The repository's `global.json` sets SDK 10.0.114 as the lowest SDK, because earlier SDKs bundle an ILCompiler package that is vulnerable below 10.0.14.

## Evidence

`research/native-command-line-evidence` holds the trial build, the output comparison, the distribution and CI runs and the timings behind this choice, with high confidence. The research shows that the native executable's `rules`, `init` and text `model check` output matched the runtime build byte for byte.

The research built the Linux executable on glibc 2.28 from AlmaLinux 8. That build ran on Ubuntu 22.04 and 24.04 and on Debian 12 and 13. A build on Ubuntu 24.04 with glibc 2.39 did not start on Debian 12. The distribution runs and the timings are the researcher's own measurements.

The research reports that CI built all 8 executables, and it lists 6 platforms for the CI test run: `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64` and `linux-arm64`. The two musl platforms appear only in the distribution runs, so the statement that CI tests every platform covers 6 of the 8.

## Revisit Conditions

This decision reopens when a library the CLI needs stops compiling ahead of time, or when a platform that consumers run has no Native AOT support.

A library that fails under Native AOT forces a change to the build. A platform without Native AOT support stays on the framework-dependent package, which the decision already serves.
