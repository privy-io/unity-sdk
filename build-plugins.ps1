<#
Build script for generating native Unity plugin binaries for Windows and Linux.

Usage:
  .\build-plugins.ps1 [-Platform windows|linux|all] [-Configuration Release|Debug]

Requirements:
- cmake (>= 3.16)
- Visual Studio (for Windows builds)
- WebView2 SDK (for Windows builds)
- libwebkit2gtk-4.0 (for Linux builds)
#>

param(
    [ValidateSet('windows','linux','all')]
    [string]$Platform = 'all',

    [ValidateSet('Release','Debug')]
    [string]$Configuration = 'Release'
)

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$pluginsDir = Join-Path $repoRoot 'SDK\Plugins'
$buildDir = Join-Path $pluginsDir 'build'

function Run-CMake {
    param(
        [string]$Generator,
        [string]$Options
    )

    if (!(Test-Path $buildDir)) {
        New-Item -ItemType Directory -Path $buildDir | Out-Null
    }

    Write-Host "Generating build files (generator: $Generator, options: $Options)"
    & cmake -S $pluginsDir -B $buildDir -G $Generator $Options
    if ($LASTEXITCODE -ne 0) { throw "cmake generation failed" }

    Write-Host "Building native plugins ($Configuration)"
    & cmake --build $buildDir --config $Configuration
    if ($LASTEXITCODE -ne 0) { throw "build failed" }
}

function Get-WebView2SdkRoot {
    if ($env:WEBVIEW2_ROOT) {
        return $env:WEBVIEW2_ROOT
    }

    $candidates = @(
        'C:\Program Files (x86)\Microsoft WebView2 SDK',
        'C:\Program Files\Microsoft WebView2 SDK'
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    return $null
}

if ($Platform -eq 'windows' -or $Platform -eq 'all') {
    Write-Host "=== Building Windows plugin ==="

    $webView2Root = Get-WebView2SdkRoot
    if (-not $webView2Root) {
        Write-Host "ERROR: WebView2 SDK not found. The build requires the WebView2 SDK headers/libs."
        Write-Host "Download it from: https://developer.microsoft.com/microsoft-edge/webview2/"
        Write-Host "Or set WEBVIEW2_ROOT to the SDK folder (contains include/WebView2.h)."
        throw "WebView2 SDK not found"
    }

    Write-Host "Using WebView2 SDK root: $webView2Root"

    # Use Visual Studio generator (adjust if you have a different VS version)
    $vsGen = 'Visual Studio 18 2026'
    $options = "-D BUILD_WINDOWS_PLUGIN=ON -D BUILD_LINUX_PLUGIN=OFF -D WEBVIEW2_ROOT=$webView2Root"
    Run-CMake -Generator $vsGen -Options $options
}

if ($Platform -eq 'linux' -or $Platform -eq 'all') {
    Write-Host "=== Building Linux plugin ==="
    # On Windows, you can still build Linux plugin if you have Linux toolchain (WSL or cross-compile)
    Run-CMake -Generator 'Unix Makefiles' -Options "-D BUILD_WINDOWS_PLUGIN=OFF -D BUILD_LINUX_PLUGIN=ON"
}

Write-Host "Done. Plugin binaries are in: $pluginsDir\x86_64"
