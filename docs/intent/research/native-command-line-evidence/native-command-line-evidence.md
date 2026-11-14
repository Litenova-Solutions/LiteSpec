# Native Command Line Evidence

The `litespec` command-line tool (CLI) checks LiteSpec records and prints the results. This research record reports whether the CLI can ship as a native executable, compiled to machine code with .NET Native AOT, for every platform that a consumer runs. It also reports what each platform needs. It matters because each operating system needs its own executable build, and a release can claim a platform only after that build runs there.

## Findings

The CLI compiles with .NET Native AOT once every JSON document it writes is built as a node tree. The native executable then gives the same output as the build on the .NET runtime. Each operating system needs its own build. A Linux build runs only on a glibc at least as new as the machine that built it.

Measured on 9 November 2026 against release 4.0.0, publishing the CLI project with `PublishAot` succeeded with 155 warnings. The warnings were 77 IL2026, 77 IL3050 and one IL3000. All but the IL3000 sat in the CLI's own JSON code, at 77 call sites. Roslyn, YamlDotNet, JsonSchema.Net and Markdig reported none. The record gives no confidence for this claim.

The one IL3000 came from Roslyn's command-line compiler driver, in `CommonCompiler.GetAssemblyLocation`. The CLI never calls that method. The native executable ran `--help`, `--version`, `rules`, `init` and every text `model check`. Its output matched the build on the .NET runtime byte for byte. The record gives no confidence for this claim.

The JSON call sites were the one defect in the build itself. Native AOT turns reflection-based serialization off. With it off, `model check`, `model query` and `book` stopped with an error. With reflection turned back on, they exited with code 0 but wrote each finding as `{}`. The trimmer had removed the property metadata of the anonymous types. `model check --format json` on the small fixture wrote 239 bytes instead of 9,520. Building every JSON document as a node tree removed all 154 warnings and kept each output byte for byte. The record gives no confidence for these claims.

After that rewrite, the native executable passed all 1,756 CLI tests on win-x64 in 3 minutes 8 seconds. The same suite took 5 minutes 15 seconds on the .NET runtime. A full `model check` of the Claimwick example took 1.32 seconds natively and 2.80 seconds on the runtime. These are means of 8 runs on one Windows machine. The record gives no confidence for these timings.

Native AOT builds for no other operating system. It builds for another architecture only with that architecture's linker, according to the cross-compilation page. A linux-x64 executable built on Ubuntu 24.04, with glibc 2.39, failed to start on Debian 12 and Ubuntu 22.04 with `GLIBC_2.38 not found`. The record gives no confidence for these claims.

The same build on AlmaLinux 8, with glibc 2.28, ran on AlmaLinux 8, Ubuntu 22.04 and 24.04, and Debian 12 and 13. Its `rules` output matched the Windows output byte for byte. The glibc executable did not start on Alpine, which needs a musl build of its own. The record gives no confidence for these claims.

The CLI computed SHA-256 hashes through `System.Security.Cryptography`. On Linux, .NET implements that with OpenSSL. On a bare Debian 12 image, `model check` aborted with `No usable version of libssl was found`. It passed once `libssl3` was installed. Fedora 45 ships OpenSSL 4 alone, and the same abort followed there, because .NET 10 loads OpenSSL 1.1 or 3. The record gives no confidence for these claims.

The record then computed the hash in managed code and used invariant globalization. With that change, the executable links only `libc` and `libm`. `model check` passed on a bare Debian 12 image with no OpenSSL, and on Fedora 45 with OpenSSL 4 alone. The JSON output matched on both. The record gives no confidence for this claim.

The .NET 10 SDK packs one package per runtime identifier, behind a top-level package. The RID-specific tools page states this, along with the rule that a Native AOT package is packed on its own operating system. It also states that the platform packages publish before the top-level package. A local test confirmed that the platform package carries the release beside the executable. It also confirmed that `dotnet litespec`, installed from that package, runs the native executable. The record gives no confidence for these claims.

The runtime identifier graph maps `linux-musl-arm64` onto `linux-arm64`. So without a `linux-musl-arm64` package, an Alpine arm64 machine would install the glibc executable, which cannot start there. The record gives no confidence for this claim.

On 9 November 2026, CI built all 8 executables, each on a runner of its own operating system. It installed each archive with the install script. It then ran the smoke scenario and the CLI suite against the installed executable on 6 platforms: win-x64, win-arm64, osx-x64, osx-arm64, linux-x64 and linux-arm64. The record gives no confidence for this claim.

Those CI runs found 2 defects that the Windows and Linux trials had not shown. `model check --since` and the contract diff joined the resolved repository path that git reports to a root that was reached through the symbolic link of a macOS temporary folder. `install.ps1` read the processor architecture that an emulated shell on an Arm PC passed down. With both fixed, the standalone executable installed and ran on AlmaLinux 8 and 9, Amazon Linux 2023, Debian 12 and 13, openSUSE Leap 15.6, Ubuntu 20.04, 22.04 and 24.04, and Alpine 3.21 and 3.22. The .NET tool installed from the packages on Windows, macOS, Linux and Alpine arm64. The record gives no confidence for these claims.

The ILCompiler advisory lists `Microsoft.DotNet.ILCompiler` below 10.0.14 as vulnerable. `PublishAot` adds that package at the runtime version that the SDK bundles. So the NuGet audit fails every restore on an SDK that bundles an earlier runtime, such as 10.0.113. SDK 10.0.114 is the lowest version that bundles 10.0.14. The record gives no confidence for this claim.

## Source Notes

The record read 4 sources on 9 November 2026. The first is the .NET documentation on RID-specific, self-contained and AOT tools. The record read it for the package layout, the per-platform packing rule and the publish order. The second is the .NET documentation on Native AOT cross-compilation. The record read it for the operating system and architecture limits, and for the linker each platform needs.

The third source is the GitHub advisory for the ILCompiler package (GHSA-p3jp-9v83-76w9). The record read it for the vulnerable range and the first patched version. The fourth source is the CLI project file (`apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj`). The record read it for the trial build.

The warning counts, the distribution runs, the CI runs and the timings are the researcher's own measurements. The record names no disagreement between sources. It names no claim that it could not confirm.
