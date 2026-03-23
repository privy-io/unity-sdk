# Windows & Linux Embedded-WebView Support

## Goal
Add headless embedded wallet WebView support for **Windows (WebView2)** and **Linux (WebKitGTK)** so that the Privy Unity SDK can run embedded wallet operations (wallet creation, signing, RPC) on desktop platforms without relying on `unity-webview`, which currently only supports iOS/Android/macOS.

This document outlines the approach, design, and implementation steps taken in the repository.

---

## Current State

- **Supported platforms:** iOS, Android, macOS, WebGL.
- **Unsupported platforms:** Windows and Linux currently fall back to a stub handler where embedded wallet features are not functional.
- **OAuth:** Works via external browser on desktop (Windows, Linux, macOS) and remains unchanged.
- **Architecture:** The embedded wallet uses a single `IWebViewHandler` interface to abstract platform differences. Existing implementations:
  - `WebViewHandler` (iOS, Android, macOS)
  - `BrowserDomIframeHandler` (WebGL)
  - `WebViewHandlerForUnsupportedPlatform` (Windows/Linux)

---

## Implementation Overview

### Approach
- Add **platform-specific native plugins** for Windows and Linux.
- Implement **C# handler wrappers** that call into those native plugins via P/Invoke.
- Keep the existing `IWebViewHandler` abstraction so the rest of the SDK is unchanged.
- Keep the embedded wallet **headless**, matching existing behavior.

### Windows (Primary)
- Native plugin: **WebView2** (Microsoft Edge WebView2 Runtime) via C++.
- Exposes a small C API to Unity.
- Uses WebView2's `postMessage` bridge to marshal messages.

### Linux (Stretch)
- Native plugin: **WebKitGTK** (GTK WebKit) via C.
- Exposes the same small C API.

---

## Code Added / Modified

### New Files
- `SDK/Runtime/EmbeddedWallet/WindowsWebViewHandler.cs` (Windows handler)
- `SDK/Runtime/EmbeddedWallet/LinuxWebViewHandler.cs` (Linux handler)
- `SDK/Plugins/Windows/PrivyWebView.cpp` (native WebView2 plugin source)
- `SDK/Plugins/Linux/PrivyWebView.c` (native WebKitGTK plugin source)

### Updated Files
- `SDK/Runtime/EmbeddedWallet/IWebviewHandler.cs` (platform factory updated)
- `SDK/Runtime/EmbeddedWallet/WebViewHandlerForUnsupportedPlatform.cs` (updated logging text)

---

## Usage

The embedded wallet works exactly like before; it is still invoked via `WebViewManager` and uses the same message protocol (JSON request/response). The only change is that on Windows/Linux, the platform-specific implementation is now functional.

---

## Build Notes (Windows)

**Requirements:**
- WebView2 Runtime installed (most Windows 10/11 systems already have it).
- Visual Studio (for native plugin build) or `clang-cl`/`msvc` toolchain.

**Build steps:**
1. Run the build script from the repo root:
   - Windows only: `powershell -ExecutionPolicy Bypass -File build-plugins.ps1 -Platform windows`
   - Linux only: `powershell -ExecutionPolicy Bypass -File build-plugins.ps1 -Platform linux`
   - Both: `powershell -ExecutionPolicy Bypass -File build-plugins.ps1 -Platform all`
2. The script produces plugin output in:
   - Windows: `SDK/Plugins/Windows/x86_64/PrivyWebView.dll`
   - Linux: `SDK/Plugins/Linux/x86_64/libPrivyWebView.so`
3. Ensure Unity plugin importer sees the binaries (they should already be in the correct folder structure).

### Windows runtime behavior & developer flow

- WebView2 window is now shown when OAuth flow starts (`PrivyWebView_LoadUrl`).
- If the user closes the window manually (X), the plugin hides the window instead of destroying it, and the SDK cancels the in-progress OAuth task.
- OAuth flow should be retried safely after close; no `InvalidOperationException` should ever remain from the previous canceled flow.
- The SDK uses `WEBVIEW_WINDOW_CLOSED` internal signal in native callback for this behavior.
- **Redirect URI must be secure (HTTPS) on Windows** for WebView2 OAuth redirect interception to work correctly, e.g. `https://auth.staging.privy.io/api/v1/oauth/callback`.

**Local test/run steps:**
1. Build plugin as above.
2. Start Unity editor and run `SampleApp`, choose `Windows` platform.
3. Launch OAuth login (e.g., Google login button), verify webview appears.
4. Close window manually while login is in progress, verify the task is canceled and no stale flow is left.
5. Tap login again; verify webview reopens and flow proceeds.

---

## Build Notes (Linux)

**Requirements:**
- `libwebkit2gtk-4.0` installed.
- GCC/Clang toolchain.

**Build steps:**
1. Build `PrivyWebView.c` into `libPrivyWebView.so`.
2. Place `libPrivyWebView.so` under `SDK/Plugins/Linux/x86_64/`.

---

## Future / Stretch

- Add an automated build pipeline to produce plugin binaries and store them in the repo.
- Optionally implement an *in-app* OAuth flow on Windows/Linux using the same WebView as the embedded wallet.
- Add unit tests that mock the native plugin boundary and validate message wiring.

---

## Notes

- The native plugin code is intentionally minimal and designed to mirror the existing message protocol used by the iOS/Android WebView handler.
- If WebView2 or WebKitGTK is missing, the SDK logs a clear error and falls back to the unsupported handler.
