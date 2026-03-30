# Windows Support Guide (Embedded Wallet)

> This document describes the Windows implementation details for Privy SDK Embedded Wallet in Unity.
> It is intended as a maintenance and future development reference.

## 1. Scope

- Windows desktop support under `UNITY_STANDALONE_WIN` and `UNITY_EDITOR_WIN`.
- Embedded wallet transport pipeline across `WebViewManager` and `WindowsWebViewHandler`.
- OAuth flow handling with native WebView2 plugin.
- Dual isolated WebView2 instances for security separation between OAuth and wallet contexts.

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
  - manages both the persistent Wallet WebView and the transient OAuth WebView
  - native plugin calls via `DllImport("PrivyWebView")` with explicit `EntryPoint` bindings
  - OAuth relay, redirect interception, and `TaskCompletionSource` completion
  - content injection via `PrivyWebView_Wallet_EvaluateJS` and message callback plumbing

- `SDK/Plugins/Windows/PrivyWebView.cpp`
  - native C++ plugin built into `PrivyWebView.dll`
  - implements two isolated WebView2 instances with separate user-data directories
  - exposes `PrivyWebView_Wallet_*` and `PrivyWebView_OAuth_*` API families
  - retains legacy `PrivyWebView_*` wrappers for backward compatibility (Linux handler, etc.)

- `SDK/Runtime/EmbeddedWallet/WebViewHandler.cs`
  - non-Windows handler (mobile/mac) for reference and cross-platform consistency

## 3. Architecture

### 3.1 Dual-WebView security model

The Windows implementation runs **two completely isolated WebView2 instances** to prevent OAuth provider cookies, localStorage, and session state from leaking into the embedded wallet context (and vice versa).

| | Wallet WebView | OAuth WebView |
|---|---|---|
| **Purpose** | Hosts the Privy embedded-wallet iframe; handles JSON postMessage communication | Displays provider consent screens (Google, Discord, etc.) during login |
| **Visibility** | Always hidden — never shown to the user | Shown as a foreground window during auth; hidden on completion |
| **Lifetime** | Persistent — created once for the SDK lifetime | Transient — lazy-initialized on first OAuth call, reused across flows |
| **User-data directory** | `%LocalAppData%\PrivyWebView\Wallet` | `%LocalAppData%\PrivyWebView\OAuth` |
| **Browser isolation** | Own cookies, localStorage, session cache | Own cookies, localStorage, session cache |
| **Navigation hooks** | None (no URL interception) | `NavigationStarting` + `FrameNavigationStarting` + `NewWindowRequested` — all check for `privy_oauth_code=` |
| **Message mechanism** | `add_WebMessageReceived` → JSON forwarded to Unity | URL redirect interception only — no `postMessage` |

Separate user-data directories are the critical isolation boundary. The Edge/Chromium process hosting each WebView2 instance stores cookies and Web Storage independently, so a compromised or malicious OAuth provider page cannot read wallet key material from localStorage, and wallet operations cannot observe OAuth session cookies.

### 3.2 Native plugin API (`PrivyWebView.dll`)

Two API families, one per webview:

**Wallet WebView** (persistent, hidden):
```
PrivyWebView_Wallet_Initialize(onMessage, onLoaded, onError)
PrivyWebView_Wallet_LoadUrl(url)
PrivyWebView_Wallet_EvaluateJS(js)
PrivyWebView_Wallet_Destroy()
```

**OAuth WebView** (transient, visible during auth):
```
PrivyWebView_OAuth_Initialize(onMessage, onLoaded, onError)
PrivyWebView_OAuth_SetRedirectUrl(redirectUri)
PrivyWebView_OAuth_LoadUrl(url)
PrivyWebView_OAuth_ShowWindow()
PrivyWebView_OAuth_HideWindow()
PrivyWebView_OAuth_Destroy()
```

### 3.3 WebViewManager

- Performs JSON message transport to/from embedded wallet JS.
- Tracks outstanding requests in `_requestResponseMap` keyed by request ID.
- Uses `TaskCompletionSource` for asynchronous response completion.
- Handles ping-ready handshake via `PingReadyUntilSuccessful`.
- Supports cancellation via `_disposeCts`.

### 3.4 WindowsWebViewHandler

- Construction initializes the **Wallet WebView** immediately via `PrivyWebView_Wallet_Initialize`.
- `RunOAuthFlow()` (static) lazy-initializes the **OAuth WebView** on first call, then reuses it across subsequent auth flows.
- Wallet callbacks (`OnWalletMessageReceived`, `OnWalletPageLoaded`, `OnWalletError`):
  - `OnWalletPageLoaded` fires via `NavigationCompleted` when a page finishes loading. It guards on the URL containing `/embedded-wallets` before injecting the JS proxy and starting the ping loop. An empty URL (init signal from the controller) is ignored.
  - `OnWalletMessageReceived` forwards all messages directly to `WebViewManager` — no OAuth URL sniffing needed.
- OAuth callbacks (`OnOAuthMessageReceived`, `OnOAuthLoaded`, `OnOAuthError`) are static and handle only redirect interception and `TaskCompletionSource` completion.
- On page load, injects a JS proxy into the wallet webview:
  - `window.PRIVY_UNITY = true`
  - `window.UnityProxy.postMessage(...)` forwards to `window.chrome.webview.postMessage(...)`
- Tracks OAuth via static `_oauthTcs`, `_oauthLock`, `_oauthRedirectUri`.

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

### Startup

1. `WebViewManager` constructed with `PrivyConfig`; calls `_webViewHandler.LoadUrl(embedded-wallets URL)`.
2. `WindowsWebViewHandler` calls `PrivyWebView_Wallet_Initialize`, creating the persistent Wallet WebView with its own isolated user-data directory.
3. `PrivyWebView_Wallet_LoadUrl` navigates to the embedded-wallet host page.
4. When the page finishes loading, `NavigationCompleted` fires → `OnWalletPageLoaded(url)` → JS proxy injected → `PingReadyUntilSuccessful()` starts.
5. Once the iframe responds, `_readyTcs` is resolved and the wallet is ready for operations.

### OAuth login flow

1. `OAuthWindowsWebViewFlow.PerformOAuthFlow()` calls `WindowsWebViewHandler.RunOAuthFlow(url, redirectUri, timeout)`.
2. On first call, the OAuth WebView is lazy-initialized via `PrivyWebView_OAuth_Initialize` with its own isolated user-data directory.
3. `PrivyWebView_OAuth_SetRedirectUrl` configures the expected callback prefix.
4. `PrivyWebView_OAuth_ShowWindow` makes the login window visible and foreground.
5. `PrivyWebView_OAuth_LoadUrl` navigates to the provider authorization URL.
6. `NavigationStarting`, `FrameNavigationStarting`, and `NewWindowRequested` handlers all check every URL for `privy_oauth_code=`. When found:
   - The OAuth window is hidden.
   - The full redirect URL is forwarded to `OnOAuthMessageReceived`.
   - `OAuthResultData` (`privy_oauth_code`, `privy_oauth_state`) is parsed and the `TaskCompletionSource` is resolved.
7. `RunOAuthFlow` returns the result to the caller. The Wallet WebView is unaffected throughout.

### Timeouts

- Default wallet call: 30 seconds
- Create/recover wallet flows: 60 seconds
- `PingReady` per-attempt timeout: 150 ms
- OAuth flow: 5 minutes

### Disposal

- Cancel all pending tasks and `_disposeCts`, clear request map, cancel `_readyTcs`.

## 6. Troubleshooting

- Log `No matching task found for ID`: response arrived after timeout/dispose or malformed ID.
- `OAuth flow is already in progress`: previous `RunOAuthFlow` not finished.
- `WEBVIEW_WINDOW_CLOSED`: user cancelled OAuth; handler cancels `_oauthTcs`.
- `JSON/URI decode errors`: `WebViewManager` decodes URL-encoded payloads from native layer when needed.
- If the wallet ping loop runs every frame: verify `NavigationCompleted` is firing. The `onLoaded` callback must deliver a non-empty URL for `OnWalletPageLoaded` to proceed. An empty URL (controller init signal) is silently ignored.
- OAuth window not appearing: confirm `PrivyWebView_OAuth_Initialize` succeeded — check for `OAuth WebView2 env failed` or `OAuth controller failed` error logs.

## 7. Suggested future improvements

- Add explicit WebView2 version detection and user-facing error handling.
- Document writer-level configuration for ping interval and maximum retry.
- Add end-to-end tests for `WebViewManager` RPC timeout and cancellation behavior.
- Consider destroying and recreating the OAuth WebView after each flow to free memory and clear any residual provider session state.
- Add a `PrivyWebView_OAuth_ClearData()` export to selectively clear OAuth cookies between flows without full destruction.

## 8. Metadata

- Tags: `windows`, `embedded-wallet`, `webview2`, `oauth`, `unity`, `security`, `isolation`
- Status: current
