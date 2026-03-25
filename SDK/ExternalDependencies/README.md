This folder contains vendored third-party libraries required by the Privy SDK.
Each subdirectory should contain the appropriate binaries/source for the
respective dependency.

- `UnityWebView/` – the unity-webview plugin (including `Plugins/` and related files).
- `jsoncanonicalizer/` – the local embedded JSON canonicalizer package source and assembly definition.

> `NewtonsoftJson` is now managed through `SDK/package.json` as a package dependency and removed from this folder. `jsoncanonicalizer-upm` is embedded in this folder and referenced using assembly reference.

When building a release, ensure that these directories exactly mirror the
contents that were previously shipped. Do **not** redistribute the entire
repository with modifications to these binaries without respecting their
licenses (see `SDK/THIRD_PARTY_NOTICES`).
