# Releasing the SDK

## Packaging

The SDK is distributed primarily as a UPM package (`com.privy.unity-sdk`).
Consumers can obtain it via several mechanisms:

* Clone/download `SDK/` into their project's `Assets` folder
* Add as a git URL package in manifest.json (`"com.privy.unity-sdk": "https://github.com/<org>/unity-sdk.git?path=SDK"`)
* Install via OpenUPM (`openupm add com.privy.unity-sdk`)


### What the package includes

- All SDK scripts
- External dependencies (WebView, Newtonsoft Json, jsoncanonicalizer) located in `SDK/ExternalDependencies`

Note: the package is structured as a standard UPM package. WebGL custom
templates are copied via the installer script or manually placed in the
consumer project; the package itself does not automatically install them.

### Versioning and export script

The [VersionedExport](../SampleApp/Assets/Editor/VersionedExport.cs) script
can be used to update the `version` field in `SDK/package.json` so it matches
`SdkVersion.cs`. It is primarily used during development and is **not required
for UPM distribution**.

Follow the normal tagging procedure (see below) to release a new package
version.

### How to release

1. Make your changes to the SDK and update `SDK/Runtime/Utils/SdkVersion.cs`.
2. Optionally run `tools/bump-version.*` to sync `package.json`.
3. Commit your changes, tag the commit (e.g. `git tag v0.7.2`), and push to
   `main`.
4. The GitHub workflow will validate the version and may perform additional
   publishing steps.

For UPM releases, simply pushing the tag is sufficient; OpenUPM and Package
Manager clients will pick up the new version automatically.
