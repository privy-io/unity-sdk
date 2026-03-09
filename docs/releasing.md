# Releasing the SDK

## Packaging

The SDK is distributed primarily as a UPM package (`com.privy.unity-sdk`).
Consumers can obtain it via several mechanisms:

- Clone/download `SDK/` into their project's `Assets` folder
- Add as a git URL package in manifest.json (`"com.privy.unity-sdk": "https://github.com/<org>/unity-sdk.git?path=SDK"`)
- Install via OpenUPM (`openupm add com.privy.unity-sdk`)

### What the package includes

- All SDK scripts
- External dependencies (WebView, Newtonsoft Json, jsoncanonicalizer) located in `SDK/ExternalDependencies`

Note: the package is structured as a standard UPM package. WebGL custom
templates are copied via the installer script or manually placed in the
consumer project; the package itself does not automatically install them.

### Versioning

Versions are managed automatically by
[release-please](https://github.com/googleapis/release-please). The version
string lives in three places and is kept in sync by release-please:

| File                              | Updater                                |
| --------------------------------- | -------------------------------------- |
| `version.txt`                     | `simple` (primary version file)        |
| `SDK/package.json`                | `json` (`$.version`)                   |
| `SDK/Runtime/Utils/SdkVersion.cs` | `generic` (`x-release-please` markers) |

### How to release

1. Merge conventional-commit-formatted PRs into `main` (e.g. `feat:`, `fix:`).
2. Release-please automatically opens (or updates) a release PR that bumps
   versions and updates `SDK/CHANGELOG.md`.
3. Review the release PR and merge it when ready.
4. On merge, release-please creates a GitHub Release and git tag.

For UPM releases, the tag is sufficient; OpenUPM and Package Manager clients
will pick up the new version automatically.
