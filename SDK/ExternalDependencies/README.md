This folder contains vendored third-party libraries required by the Privy SDK.
Each subdirectory should contain the appropriate binaries/source for the
respective dependency.

- `UnityWebView/` – the unity-webview plugin (including `Plugins/` and related files).
- `NewtonsoftJson/` – the Newtonsoft.Json DLL(s) used by the SDK.
- `jsoncanonicalizer/` – local copy of jsoncanonicalizer DLLs.

When building a release, ensure that these directories exactly mirror the
contents that were previously shipped. Do **not** redistribute the entire
repository with modifications to these binaries without respecting their
licenses (see `SDK/THIRD_PARTY_NOTICES`).
