# Native Command Line

## Context

The `litespec` command line shipped as one framework-dependent .NET tool, so every consumer needed the .NET 10 runtime to run it and the .NET SDK to install it, including a client-only web project with no other .NET code. `research/native-command-line-evidence` shows that the CLI compiles with .NET Native AOT once its JSON output stops relying on reflection, keeps every output byte for byte, and finishes a model check of the Claimwick example in 1.32 s against 2.80 s. It also shows what each platform needs: its own build on its own operating system, a glibc build no newer than the oldest distribution it serves, a musl build for Alpine, and OpenSSL on Linux.

## Decision

The CLI compiles ahead of time into one native executable per platform: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm64`, `osx-x64` and `osx-arm64`. The `LiteSpec.Cli` package names one native package per platform, and a framework-dependent package serves every other platform, so `dotnet tool install` and `dotnet tool restore` work as before. Each GitHub release also carries a standalone archive per platform with `SHA256SUMS`, a build provenance attestation per file, and install scripts for Linux, macOS and Windows. A project runs either install, and `litespec init --install standalone` writes one that runs the binary and pins the release in `reviewedLiteSpecVersion`. The Linux glibc builds link against glibc 2.28 on AlmaLinux 8. CI builds, installs and tests the executable on every platform, and runs it on stock Linux distributions.

## Evidence with Confidence

`research/native-command-line-evidence` holds the trial build, the output comparison, the distribution runs and the timings behind this choice, high confidence for Windows and Linux on x64, where each claim was run. The macOS and arm64 builds rest on the cross-compilation documentation until CI runs them, medium confidence.
