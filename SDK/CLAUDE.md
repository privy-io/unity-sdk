# Privy Unity SDK — SDK/

Unity Package Manager package (`com.privy.unity-sdk`) providing authentication and embedded wallet functionality. Requires Unity 2022.3+.

## Directory Structure

```
SDK/
├── package.json            # UPM package manifest (version, dependencies)
├── Runtime/                # All runtime C# source (included in player builds)
├── Editor/                 # Editor-only scripts (excluded from player builds)
├── ExternalDependencies/   # Vendored third-party libraries (UnityWebView, jsoncanonicalizer)
└── Plugins/                # Native platform code (iOS Objective-C, WebGL .jslib)
```

## Architecture

Every public service has a `public` interface (e.g. `ILoginWithEmail`) and a separate `internal` implementation (e.g. `LoginWithEmail`). SDK consumers only ever see interfaces and public models — never implementation classes.

All dependencies are constructor-injected. See `docs/dependency-injection.md`.

## Code Formatting

```bash
dotnet format Format.csproj
```

Run before committing. `Format.csproj` covers `Runtime/` and `Editor/` but excludes `ExternalDependencies/`.

## XML Documentation

All `public` interfaces, methods, properties, and classes require `/// <summary>` docs with `/// <param>` and `/// <exception cref="">` where applicable.

## Versioning and Release

See `docs/releasing.md`. Do not manually edit `version.txt`, `SDK/package.json`, or `SDK/Runtime/Utils/SdkVersion.cs`, and do not remove the `// x-release-please-start-version` / `// x-release-please-end` markers in `SdkVersion.cs`.

## Native Plugins

See `docs/native-code.md` for the ARC bridging guide, `MonoPInvokeCallback` pattern, and `DllImport`/`extern` usage.
