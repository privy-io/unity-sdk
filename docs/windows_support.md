# Windows Support Guide (Embedded Wallet)

> This document describes the Windows implementation details for Privy SDK Embedded Wallet in Unity.
> It is intended as a maintenance and future development reference.

## 1. Scope

- Windows desktop support under `UNITY_STANDALONE_WIN` and `UNITY_EDITOR_WIN`.
- Embedded wallet transport pipeline across `WebViewManager` and `WindowsWebViewHandler`.
- OAuth flow handling with native WebView2 plugin.

## 2. Key files

- `SDK/Runtime/EmbeddedWallet/WebViewManager.cs`
  - central request/response manager
  - correlation by ID map
  - ready state + timeout behavior
  - wallet operation wrappers (CreateEthereumWallet / CreateSolanaWallet / ConnectWallet / RecoverWallet / Request / SignWithUserSigner)

- `SDK/Runtime/EmbeddedWallet/IWebviewHandler.cs`
  - interface for platform-specific handlers

- `SDK/Runtime/EmbeddedWallet/WindowsWebViewHandler.cs`
  - Windows-specific implementation
  - native plugin calls via `DllImport("PrivyWebView")`
  - OAuth relay and redirect parsing
  - content injection and message callback plumbing

- `SDK/Runtime/EmbeddedWallet/WebViewHandler.cs`
  - non-Windows handler (mobile/mac) for reference and cross-platform consistency

## 3. Architecture

### WebViewManager

- Performs JSON message transport to/from embedded wallet JS.
- Tracks outstanding requests in `_requestResponseMap` keyed by request ID.
- Uses `TaskCompletionSource` for asynchronous response completion.
- Handles ping-ready handshake via `PingReadyUntilSuccessful`.
- Supports cancellation via `_disposeCts`.

### WindowsWebViewHandler

- Native plugin lifecycle:
  - `PrivyWebView_Initialize(onMessage, onLoaded, onError)`
  - `PrivyWebView_LoadUrl(url)`
  - `PrivyWebView_EvaluateJS(js)`
  - `PrivyWebView_SetRedirectUrl(redirectUri)`
  - `PrivyWebView_ShowWindow()` / `PrivyWebView_CloseWindow()`

- On page load, injects a JS proxy:
  - `window.PRIVY_UNITY = true`
  - `window.UnityProxy.postMessage(...)` forwards to `window.chrome.webview.postMessage(...)`

- Tracks OAuth via static `_oauthTcs`, `_oauthLock`, `_oauthRedirectUri`.
- Handles redirect URL interception and token extraction.

## 4. Build requirements (Windows)

- Unity player settings for Standalone Windows + Editor Windows.
  - Standalone Windows uses `Product Name` from Unity Project Settings (does not use `Company Name`) for generated output folder and overlay.
  - iOS/Android typically use `Company Name` as publisher bundle id prefix, but this is not used for Windows build targets in this SDK path.
- Include native plugin library `PrivyWebView.dll` in `Plugins` with x64/x86 configurations.
- Ensure `WebView2` runtime is installed on host machine.
- Fallback path: `WebViewHandlerForUnsupportedPlatform` for missing plugin or unsupported platform.

### 4.1 Native Plugin Build (Windows)

The native plugin is built from `SDK\Plugins` and provides the `PrivyWebView` API used by `WindowsWebViewHandler`.

- Build script: `build-plugins.ps1` (root of repository).
- Requirements:
  - `cmake` (>= 3.16)
  - Visual Studio (for Windows target)
  - WebView2 SDK (headers + libs, e.g. `include/WebView2.h`)

Run the script from the repo root:

```powershell
# Open PowerShell in the repo root
.\build-plugins.ps1 -Platform windows -Configuration Release
```

- Terms:
  - uses `SDK\Plugins` as CMake source directory
  - output goes to `SDK\Plugins\build` + `SDK\Plugins\x86_64`
  - chooses WebView2 location from `WEBVIEW2_ROOT` env var or default install paths

- If building manually with CMake:

```powershell
# from repo root
cd SDK\Plugins
mkdir -Force build; cd build
cmake -G "Visual Studio 17 2022" -D BUILD_WINDOWS_PLUGIN=ON -D BUILD_LINUX_PLUGIN=OFF -D WEBVIEW2_ROOT="C:\Program Files (x86)\Microsoft WebView2 SDK" ..
cmake --build . --config Release
```

## 5. Runtime behavior

1. `WebViewManager` constructed with `PrivyConfig` and calls `_webViewHandler.LoadUrl(...)`.
2. `WindowsWebViewHandler` initializes native plugin and loads URL.
3. On embedded wallet page load:
   - JS proxy injection
   - `WebViewManager.PingReadyUntilSuccessful()` starts ping loop.
4. Responses come through callback `OnMessageReceived`.
5. `WebViewManager` resolves requests by ID from `_requestResponseMap`.

- Timeouts:
  - Default wallet call: 30 seconds
  - Create wallet flows: 60 seconds
  - PingReady has 150ms timeout per attempt.

- Disposal:
  - Cancel all pending tasks and `_disposeCts`, clear map, cancel `_readyTcs`.

## 6. Troubleshooting

- Log `No matching task found for ID`: response arrived after timeout/dispose or malformed ID.
- `OAuth flow is already in progress`: previous `RunOAuthFlow` not finished.
- `WEBVIEW_WINDOW_CLOSED`: user cancelled OAuth; handler cancels `_oauthTcs`.
- `JSON/URI decode errors`: `WebViewManager` decodes URL-encoded payloads from native layer when needed.

## 7. Suggested future improvements

- Add explicit WebView2 version detection and user-facing error handling.
- Document writer-level configuration for ping interval and maximum retry.
- Add end-to-end tests for `WebViewManager` RPC timeout and cancellation behavior.
- Consider refactoring code path common between Windows and other handlers into shared helper.

## 8. Metadata

- Tags: `windows`, `embedded-wallet`, `webview2`, `oauth`, `unity`
- Status: draft
