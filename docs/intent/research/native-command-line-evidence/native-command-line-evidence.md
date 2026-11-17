# Native Command Line Evidence

The `litespec` command-line tool (CLI) checks LiteSpec records and prints the results. This research record reports whether the CLI can ship as a native executable, compiled to machine code with .NET Native AOT, for every platform that a consumer runs. It also reports what each platform needs. It matters because each operating system needs its own executable build, and a release can claim a platform only after that build runs there.

## Findings

The [CLI project][cli-project] compiles with .NET Native AOT once every JSON document it writes is built as a node tree. The native executable gives the same output as the build on the .NET runtime. Each operating system needs its own build [cross-compilation page][native-aot-cross-compile]. A Linux build runs only on a glibc at least as new as the build machine.

Publishing the CLI project release 4.0.0 with `PublishAot` on 9 November 2026 succeeded with 155 warnings [CLI project][cli-project]. The warnings were 77 `IL2026`, 77 `IL3050`, and one `IL3000`. All but the `IL3000` sat in the CLI JSON code, at 77 call sites. Roslyn, YamlDotNet, JsonSchema.Net, and Markdig reported no warnings.

The one `IL3000` came from the Roslyn compiler driver method `CommonCompiler.GetAssemblyLocation` [CLI project][cli-project]. The CLI never calls that method. The native executable ran `--help`, `--version`, `rules`, `init`, and every text `model check`. Its output matched the build on the .NET runtime byte for byte.

The JSON call sites were the one defect in the build itself [CLI project][cli-project]. Native AOT turns reflection-based serialization off. With reflection off, `model check`, `model query`, and `book` stopped with an error. With reflection on, they exited with code 0 but wrote each finding as an empty object. The trimmer had removed the property metadata of the anonymous types. `model check --format json` on the small fixture wrote 239 bytes instead of 9,520. Building every JSON document as a node tree removed all 154 warnings and kept each output byte for byte.

After that rewrite, the native executable passed all 1,756 CLI tests on `win-x64` in 3 minutes 8 seconds [CLI project][cli-project]. The same suite took 5 minutes 15 seconds on the .NET runtime. A full `model check` of the Claimwick example took 1.32 seconds natively and 2.80 seconds on the runtime. These are means of 8 runs on one Windows machine.

Native AOT builds for no other operating system [cross-compilation page][native-aot-cross-compile]. It builds for another architecture only with the linker of that architecture. A `linux-x64` executable built on Ubuntu 24.04 with glibc 2.39 failed to start on Debian 12 and Ubuntu 22.04 [CLI project][cli-project].

The same build on AlmaLinux 8 with glibc 2.28 ran on AlmaLinux 8, Ubuntu 22.04 and 24.04, and Debian 12 and 13 [CLI project][cli-project]. Its `rules` output matched the Windows output byte for byte. The glibc executable did not start on Alpine, which needs a musl build of its own.

The CLI computed SHA-256 hashes through `System.Security.Cryptography` [CLI project][cli-project]. On Linux, .NET implements that with OpenSSL. On a bare Debian 12 image, `model check` aborted with `No usable version of libssl was found`. It passed once `libssl3` was installed. Fedora 45 ships OpenSSL 4 alone, and the same abort followed there, because .NET 10 loads OpenSSL 1.1 or 3.

The researcher then computed the hash in managed code and used invariant globalization [CLI project][cli-project]. With that change, the executable links only `libc` and `libm`. `model check` passed on a bare Debian 12 image with no OpenSSL, and on Fedora 45 with OpenSSL 4 alone. The JSON output matched on both.

The .NET 10 SDK packs one package per runtime identifier, behind a top-level package [RID-specific tools page][rid-specific-tools]. The RID-specific tools page states this, with the rule that each operating system packs its own Native AOT package. It also states that the platform packages publish before the top-level package. A local test confirmed that the platform package carries the release beside the executable [CLI project][cli-project]. It also confirmed that `dotnet litespec`, installed from that package, runs the native executable.

The runtime identifier graph maps `linux-musl-arm64` onto `linux-arm64` [RID-specific tools page][rid-specific-tools]. The record infers that without a `linux-musl-arm64` package, an Alpine arm64 machine installs the glibc executable, which cannot start there.

On 9 November 2026, CI built all 8 executables, each on a runner of its own operating system [CLI project][cli-project]. It installed each archive with the install script. It then ran the smoke scenario and the CLI suite against the installed executable on 6 platforms. Those platforms are `win-x64`, `win-arm64`, `osx-x64`, `osx-arm64`, `linux-x64`, and `linux-arm64`.

Those CI runs found 2 defects that the Windows and Linux trials had not shown [CLI project][cli-project]. `model check --since` and the contract diff joined the resolved repository path that git reports to a root. That root passed through the symbolic link of a macOS temporary folder. `install.ps1` read the processor architecture that an emulated shell on an Arm PC passed down. With both fixed, the standalone executable installed and ran on AlmaLinux 8 and 9, Amazon Linux 2023, Debian 12 and 13, and openSUSE Leap 15.6. It also installed and ran on Ubuntu 20.04, 22.04, and 24.04, and Alpine 3.21 and 3.22. The .NET tool installed from the packages on Windows, macOS, Linux, and Alpine arm64.

The ILCompiler advisory lists `Microsoft.DotNet.ILCompiler` below 10.0.14 as vulnerable [ILCompiler advisory][ilcompiler-advisory]. `PublishAot` adds that package at the runtime version that the SDK bundles [CLI project][cli-project]. So the NuGet audit fails every restore on an SDK that bundles an earlier runtime, such as 10.0.113. SDK 10.0.114 is the lowest version that bundles 10.0.14.

## Source Notes

The record read the .NET documentation on RID-specific, self-contained, and AOT tools for the package layout, the per-platform packing rule, and the publish order [RID-specific tools page][rid-specific-tools].

The record read the .NET documentation on Native AOT cross-compilation for the operating system and architecture limits, and for the linker each platform needs [cross-compilation page][native-aot-cross-compile].

The record read the GitHub advisory for the ILCompiler package (`GHSA-p3jp-9v83-76w9`) for the vulnerable range and the first patched version [ILCompiler advisory][ilcompiler-advisory].

The record read the CLI project file for the trial build [CLI project][cli-project]. The warning counts, distribution runs, CI runs, and timings are the researcher's own measurements.

[rid-specific-tools]: https://learn.microsoft.com/en-us/dotnet/core/tools/rid-specific-tools "Read on 2026-11-09"
[native-aot-cross-compile]: https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/cross-compile "Read on 2026-11-09"
[ilcompiler-advisory]: https://github.com/advisories/GHSA-p3jp-9v83-76w9 "Read on 2026-11-09"
[cli-project]: ../../../../apps/cli/src/LiteSpec.Cli/LiteSpec.Cli.csproj "Read on 2026-11-09"
