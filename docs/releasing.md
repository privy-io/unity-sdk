# Releasing the SDK

## Packaging

The SDK ships as a `.unitypackage` file and as a UPM package (`com.privy.unity-sdk`).
Consumers can use any of the supported distribution mechanisms:

* Import the `.unitypackage` from GitHub Releases or Asset Store
* Clone/download `SDK/` into their project's `Assets` folder
* Add as a git URL package in manifest.json (`"com.privy.unity-sdk": "https://github.com/<org>/unity-sdk.git?path=SDK"`)
* Install via OpenUPM (`openupm add com.privy.unity-sdk`)

The rest of this document focuses on producing the `.unitypackage`, which remains the
easiest way to deliver the full set of assets including WebGL templates.

### What the package includes

- All SDK scripts
- External dependencies (WebView, Newtonsoft Json)

### Why a .unitypackage

- **External dependencies** — The SDK bundles third‑party libraries (WebView,
  Newtonsoft.Json, jsoncanonicalizer) inside the `.unitypackage`. This ensures
  that users who import the package receive everything they need without
  additional package resolution.
- **WebGL custom templates** — WebGL builds require a custom template that must
  live in the user's `Assets` folder. The Unity package manager cannot place
  files there, so the `.unitypackage` handles it.

### Export script

The [VersionedExport](../SampleApp/Assets/Editor/VersionedExport.cs) script
automates the packaging process. It:

2. Updates the `version` field in `SDK/package.json` to match `SdkVersion.cs`.
3. Exports the package directory and any `Assets/WebGLTemplates` folders into a
   versioned `.unitypackage` file.

Because `SDK/` is now a local UPM package, the script no longer relies on
symlinks; it exports from `Packages/com.privy.unity-sdk` directly.

The script also bumps the version number based on the value in [SdkVersion](../SDK/Runtime/Utils/SdkVersion.cs).

### How to export

1. Make your changes to the SDK and update `SDK/Runtime/Utils/SdkVersion.cs`.
2. Optionally run `tools/bump-version.*` to sync `package.json`.
3. Open the `SampleApp` in the Unity Editor (it references the local SDK package).
4. Go to `Tools > Export Privy SDK`.
5. Save the resulting `.unitypackage` file. Attach it to a GitHub release or
   distribute via Asset Store.

For UPM releases, tag the repository (e.g. `v0.7.2`) and push; the GitHub action
will validate versions and optionally build the package.
