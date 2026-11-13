<#
.SYNOPSIS
Installs the LiteSpec command line on Windows from a GitHub release.

.DESCRIPTION
Downloads the native binary for this machine and the LiteSpec release it carries, checks the
archive against the release's SHA256SUMS, keeps it in <InstallDir>\<version>, and puts that
folder on the user PATH in place of any other LiteSpec version. The binary needs no .NET runtime.
Runs on Windows PowerShell 5.1 and PowerShell 7.

.PARAMETER Version
The release to install. Without one, the script installs the release .litespec\project.yaml
reviews in the working directory, and else the latest release. Also read from LITESPEC_VERSION.

.PARAMETER InstallDir
The folder that keeps each release. Defaults to %LOCALAPPDATA%\Programs\LiteSpec. Also read
from LITESPEC_INSTALL_DIR. LITESPEC_DOWNLOAD_URL names another base URL that holds
v<version>/ with the release assets.

.EXAMPLE
irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex

.EXAMPLE
$env:LITESPEC_VERSION = '5.3.0'; irm https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.ps1 | iex
#>
[CmdletBinding()]
param(
    [string] $Version = $env:LITESPEC_VERSION,
    [string] $InstallDir = $env:LITESPEC_INSTALL_DIR
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$Repository = 'Litenova-Solutions/LiteSpec'

if (-not $InstallDir) {
    $InstallDir = Join-Path $env:LOCALAPPDATA 'Programs\LiteSpec'
}

$DownloadUrl = if ($env:LITESPEC_DOWNLOAD_URL) { $env:LITESPEC_DOWNLOAD_URL } else { "https://github.com/$Repository/releases/download" }

# Windows PowerShell 5.1 offers TLS 1.2 only when asked.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Stop-Install([string] $Message) {
    throw "litespec install: $Message"
}

# The latest release version, from the tag GitHub redirects the latest release page to.
function Get-LatestVersion {
    $page = "https://github.com/$Repository/releases/latest"
    $response = Invoke-WebRequest -Uri $page -UseBasicParsing -Method Head
    $final = if ($response.BaseResponse.PSObject.Properties['ResponseUri']) {
        $response.BaseResponse.ResponseUri.AbsoluteUri
    } else {
        $response.BaseResponse.RequestMessage.RequestUri.AbsoluteUri
    }

    $tag = $final.Substring($final.LastIndexOf('/') + 1)

    if ($tag -notmatch '^v[0-9]') {
        Stop-Install "the latest release could not be read from $page; pass -Version"
    }

    return $tag.Substring(1)
}

if (-not $Version -and (Test-Path '.litespec\project.yaml')) {
    $line = Select-String -Path '.litespec\project.yaml' -Pattern '^reviewedLiteSpecVersion: [''"]?([^''"\s]+)' | Select-Object -First 1

    if ($line) {
        $Version = $line.Matches[0].Groups[1].Value
        Write-Host "Installing the release .litespec\project.yaml reviews: $Version"
    }
}

if (-not $Version) {
    $Version = Get-LatestVersion
}

$Version = $Version.TrimStart('v')

# A process inherits PROCESSOR_ARCHITECTURE from its parent, so a shell that runs emulated on an Arm PC
# passes AMD64 down. The system's own value names the machine's native architecture.
$machine = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -ErrorAction SilentlyContinue).PROCESSOR_ARCHITECTURE

if (-not $machine) {
    $machine = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
}

$arch = switch ($machine) {
    'AMD64' { 'x64' }
    'ARM64' { 'arm64' }
    default { Stop-Install "no native build for $machine; install the .NET tool LiteSpec.Cli" }
}

$rid = "win-$arch"
$name = "litespec-$Version-$rid"
$archive = "$name.zip"
$target = Join-Path $InstallDir $Version
$work = Join-Path ([IO.Path]::GetTempPath()) ("litespec-" + [Guid]::NewGuid().ToString('N'))
# The archive unpacks inside the install folder, so the final move stays on one drive.
$extract = Join-Path $InstallDir ('.extract-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null

try {
    Write-Host "Downloading LiteSpec $Version for $rid"
    $archivePath = Join-Path $work $archive
    $sumsPath = Join-Path $work 'SHA256SUMS'
    Invoke-WebRequest -Uri "$DownloadUrl/v$Version/$archive" -OutFile $archivePath -UseBasicParsing
    Invoke-WebRequest -Uri "$DownloadUrl/v$Version/SHA256SUMS" -OutFile $sumsPath -UseBasicParsing

    $expected = $null

    foreach ($line in Get-Content $sumsPath) {
        $fields = $line -split '\s+', 2

        if ($fields.Count -eq 2 -and $fields[1].TrimStart('*') -eq $archive) {
            $expected = $fields[0].ToLowerInvariant()
        }
    }

    if (-not $expected) {
        Stop-Install "SHA256SUMS lists no $archive"
    }

    $actual = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($actual -ne $expected) {
        Stop-Install "$archive does not match its SHA256SUMS entry"
    }

    New-Item -ItemType Directory -Path $extract -Force | Out-Null
    Expand-Archive -Path $archivePath -DestinationPath $extract

    if (-not (Test-Path (Join-Path $extract "$name\litespec.exe"))) {
        Stop-Install "$archive holds no $name\litespec.exe"
    }

    # Each release keeps its own folder, so replacing one never touches another or anything else in the install folder.
    if (Test-Path $target) {
        Remove-Item -Path $target -Recurse -Force
    }

    Move-Item -Path (Join-Path $extract $name) -Destination $target
}
finally {
    Remove-Item -Path $work -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $extract -Recurse -Force -ErrorAction SilentlyContinue
}

$executable = Join-Path $target 'litespec.exe'
$installed = & $executable --version

if ($LASTEXITCODE -ne 0) {
    Stop-Install 'the installed binary does not run; see the platform requirements in the CLI overview'
}

if ($installed -ne $Version) {
    Stop-Install "the installed binary reports $installed instead of $Version"
}

Write-Host "Installed LiteSpec $Version at $target"

# The user PATH names one LiteSpec version: this one replaces any other under the install folder.
$prefix = $InstallDir.TrimEnd('\') + '\'
$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
$entries = @($userPath -split ';' | Where-Object { $_ -and -not $_.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) })
[Environment]::SetEnvironmentVariable('Path', (@($target) + $entries) -join ';', 'User')
$env:Path = (@($target) + @($env:Path -split ';' | Where-Object { $_ -and -not $_.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) })) -join ';'

# In a GitHub Actions job, later steps find the command on the PATH.
if ($env:GITHUB_PATH) {
    Add-Content -Path $env:GITHUB_PATH -Value $target
}

Write-Host 'Run: litespec --help (open a new terminal first, so it reads the updated PATH)'
