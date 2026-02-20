# Releasing the SDK

## Packaging

The SDK ships as a `.unitypackage` file containing all SDK scripts and external
dependencies.

### What the package includes

- All SDK scripts
- External dependencies (WebView, Newtonsoft Json)

### Why a .unitypackage

- **External dependencies** — Some dependencies, like WebView, do not exist in the
  Unity package registry. Bundling them in the `.unitypackage` ensures they
  install alongside the SDK.
- **WebGL custom templates** — WebGL builds require a custom template that must
  live in the user's `Assets` folder. The Unity package manager cannot place
  files there, so the `.unitypackage` handles it.

### Export script

The [VersionedExport](./SampleApp/Assets/Editor/VersionedExport.cs) script
automates the packaging process. It:

1. Collects SDK assets, WebGL templates, and external dependencies.
2. Moves them into the project's `Assets` folder so Unity can export them.
3. Exports the `.unitypackage` file.
4. Moves the files back to their original locations.

The script also bumps the version number based on the value in [SdkVersion](./SDK/Runtime/Utils/SdkVersion.cs).

### How to export

1. Make your changes to the SDK.
2. Open the `SampleApp` in the Unity Editor.
3. Go to `Tools` in the top menu.
4. Select `Export Privy SDK`.
