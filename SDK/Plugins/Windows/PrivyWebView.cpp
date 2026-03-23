// Native plugin for Windows that provides a minimal WebView2-based webview for the embedded wallet.
//
// This file is intended to be built into a DLL (PrivyWebView.dll) and placed under:
//   SDK/Plugins/Windows/x86_64/PrivyWebView.dll
//
// Build requirements:
// - WebView2 SDK (Microsoft Edge WebView2)
// - Visual Studio (MSVC) toolchain
//

#include <windows.h>
#include <string>
#include <vector>
#include <functional>

// WebView2 headers (part of Microsoft Edge WebView2 SDK)
#include "WebView2.h"

// WRL smart pointers for COM lifetime management
#include <wrl.h>

// Forward-declare the callback function types used by the C# wrapper.
using MessageCallback = void(__cdecl*)(const char*);
using StatusCallback = void(__cdecl*)(const char*);

static MessageCallback g_messageCallback = nullptr;
static StatusCallback g_loadedCallback = nullptr;
static StatusCallback g_errorCallback = nullptr;

// WebView2 Globals
static Microsoft::WRL::ComPtr<ICoreWebView2Environment> g_webViewEnvironment;
static Microsoft::WRL::ComPtr<ICoreWebView2Controller> g_webViewController;
static Microsoft::WRL::ComPtr<ICoreWebView2> g_webView;

// Window for WebView2
static HWND g_hWnd = nullptr;

static const wchar_t kWindowClassName[] = L"PrivyWebViewWindowClass";

static LRESULT CALLBACK WebViewWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_CLOSE) {
        // Notify the managed side that the user closed the webview during auth.
        if (g_errorCallback) {
            g_errorCallback("WEBVIEW_WINDOW_CLOSED");
        }

        // Keep the control alive to allow re-open later; hide instead of destroying.
        ShowWindow(hWnd, SW_HIDE);
        return 0;
    }

    return DefWindowProcW(hWnd, message, wParam, lParam);
}

static HWND EnsureWebViewWindow()
{
    if (g_hWnd && !IsWindow(g_hWnd)) {
        g_hWnd = nullptr;
    }

    if (g_hWnd)
        return g_hWnd;

    WNDCLASSEXW wcex = {};
    wcex.cbSize = sizeof(wcex);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = WebViewWndProc;
    wcex.cbClsExtra = 0;
    wcex.cbWndExtra = 0;
    wcex.hInstance = GetModuleHandleW(nullptr);
    wcex.hCursor = LoadCursorW(nullptr, (LPCWSTR)IDC_ARROW);
    wcex.hbrBackground = (HBRUSH)(COLOR_WINDOW + 1);
    wcex.lpszMenuName = nullptr;
    wcex.lpszClassName = kWindowClassName;

    if (!RegisterClassExW(&wcex) && GetLastError() != ERROR_CLASS_ALREADY_EXISTS) {
        return nullptr;
    }

    g_hWnd = CreateWindowExW(
        0,
        kWindowClassName,
        L"Privy Login",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        1024,
        768,
        nullptr,
        nullptr,
        wcex.hInstance,
        nullptr);

    if (g_hWnd)
    {
        // Create hidden by default; only show for explicit navigation flow.
        ShowWindow(g_hWnd, SW_HIDE);
        UpdateWindow(g_hWnd);
    }

    return g_hWnd;
}

static void ShowWebViewWindow()
{
    EnsureWebViewWindow();
    if (g_hWnd && IsWindow(g_hWnd)) {
        ShowWindow(g_hWnd, SW_SHOW);
        SetForegroundWindow(g_hWnd);
        SetWindowPos(g_hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }
}

static void HideWebViewWindow()
{
    if (g_hWnd && IsWindow(g_hWnd)) {
        ShowWindow(g_hWnd, SW_HIDE);
    }
}

static std::wstring Utf8ToUtf16(const std::string& utf8)
{
    if (utf8.empty())
        return {};

    int size = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, nullptr, 0);
    if (size <= 0)
        return {};

    std::vector<wchar_t> buffer(size);
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, buffer.data(), size);

    // The returned size includes the terminating null; remove it.
    if (!buffer.empty() && buffer.back() == L'\0')
        buffer.pop_back();

    return std::wstring(buffer.begin(), buffer.end());
}

static std::wstring g_pendingUrl;
static std::wstring g_pendingJs;
static std::wstring g_oauthRedirectUri;

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_SetRedirectUrl(const char* url)
{
    if (url == nullptr || url[0] == '\0') {
        g_oauthRedirectUri.clear();
        if (g_loadedCallback) g_loadedCallback("PrivyWebView_SetRedirectUrl: empty redirect uri");
        return;
    }

    g_oauthRedirectUri = Utf8ToUtf16(url);

    char buf[1024];
    sprintf_s(buf, "PrivyWebView_SetRedirectUrl: redirectUri='%s'", url);
    if (g_loadedCallback) g_loadedCallback(buf);
}

static std::wstring UrlDecode(const std::wstring& input)
{
    std::wstring output;
    output.reserve(input.size());
    for (size_t i = 0; i < input.size(); ++i) {
        wchar_t c = input[i];
        if (c == L'%' && i + 2 < input.size()) {
            auto hex = input.substr(i + 1, 2);
            wchar_t decoded = static_cast<wchar_t>(std::wcstol(hex.c_str(), nullptr, 16));
            output.push_back(decoded);
            i += 2;
        } else if (c == L'+') {
            output.push_back(L' ');
        } else {
            output.push_back(c);
        }
    }
    return output;
}

static std::string Utf16ToUtf8(const std::wstring& utf16)
{
    if (utf16.empty())
        return {};

    int size = WideCharToMultiByte(CP_UTF8, 0, utf16.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (size <= 0)
        return {};

    std::vector<char> buffer(size);
    WideCharToMultiByte(CP_UTF8, 0, utf16.c_str(), -1, buffer.data(), size, nullptr, nullptr);

    // Remove terminating null
    if (!buffer.empty() && buffer.back() == '\0')
        buffer.pop_back();

    return std::string(buffer.begin(), buffer.end());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Initialize(MessageCallback onMessage, StatusCallback onLoaded, StatusCallback onError)
{
    g_messageCallback = onMessage;
    g_loadedCallback = onLoaded;
    g_errorCallback = onError;

    // Note: Initialization is best done on the main thread with a message pump.
    // For simplicity, we use a minimal hidden window.

    // Ensure we have a window to host the WebView2 control
    EnsureWebViewWindow();

    HRESULT hr = CreateCoreWebView2EnvironmentWithOptions(
        nullptr, nullptr, nullptr,
        Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
                if (FAILED(result)) {
                    if (g_errorCallback) {
                        char buf[128];
                        sprintf_s(buf, "Failed to create WebView2 environment (HRESULT=0x%08X)", result);
                        g_errorCallback(buf);
                    }
                    return result;
                }

                g_webViewEnvironment = env;
                env->CreateCoreWebView2Controller(
                    g_hWnd,
                    Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
                            if (FAILED(result)) {
                                if (g_errorCallback) {
                                    char buf[128];
                                    sprintf_s(buf, "Failed to create WebView2 controller (HRESULT=0x%08X)", result);
                                    g_errorCallback(buf);
                                }
                                return result;
                            }

                            g_webViewController = controller;
                            controller->get_CoreWebView2(&g_webView);

                            // Make sure the WebView is visible and sized.
                            controller->put_IsVisible(TRUE);
                            RECT bounds = {0, 0, 1024, 768};
                            controller->put_Bounds(bounds);

                            // Install a message handler to forward messages to Unity
                            g_webView->add_WebMessageReceived(
                                Microsoft::WRL::Callback<ICoreWebView2WebMessageReceivedEventHandler>(
                                    [](ICoreWebView2* sender, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
                                        PWSTR msg = nullptr;
                                        if (SUCCEEDED(args->TryGetWebMessageAsString(&msg)) && msg != nullptr) {
                                            std::wstring messageW(msg);
                                            std::string utf8 = Utf16ToUtf8(messageW);
                                            if (g_messageCallback) {
                                                g_messageCallback(utf8.c_str());
                                            }
                                            CoTaskMemFree(msg);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Intercept custom navigation scheme used to message Unity
                            auto handleUnityScheme = [&](const std::wstring& u) {
                                auto isUnityScheme = [&](const std::wstring& prefix) {
                                    return u.rfind(prefix, 0) == 0;
                                };

                                // Only log intercept checks when OAuth redirect flow is configured.
                                if (!g_oauthRedirectUri.empty())
                                {
                                    std::string currentUrl = Utf16ToUtf8(u);
                                    std::string redirectUrl = Utf16ToUtf8(g_oauthRedirectUri);
                                    char buf[2048];
                                    sprintf_s(buf, "PrivyWebView intercept check: currentUrl='%s', expectedRedirect='%s'", currentUrl.c_str(), redirectUrl.c_str());
                                    if (g_loadedCallback) g_loadedCallback(buf);
                                }

                                // If we see an OAuth code, always intercept and complete the flow.
                                if (u.find(L"privy_oauth_code=") != std::wstring::npos) {
                                    HideWebViewWindow();

                                    std::string utf8 = Utf16ToUtf8(u);
                                    if (g_messageCallback) {
                                        g_messageCallback(utf8.c_str());
                                    }
                                    return true;
                                }

                                // If a redirect URI was configured, keep allowing normal navigation,
                                // but do not send it as a message unless it contains OAuth code.
                                if (!g_oauthRedirectUri.empty() && isUnityScheme(g_oauthRedirectUri)) {
                                    return false;
                                }

                                // If redirect URI is not configured yet, allow auth domain navigation and do not message.
                                if (isUnityScheme(L"https://auth.staging.privy.io/") ||
                                    isUnityScheme(L"https://auth.privy.io/")) {
                                    return false;
                                }

                                return false;
                            };

                            g_webView->add_NavigationStarting(
                                Microsoft::WRL::Callback<ICoreWebView2NavigationStartingEventHandler>(
                                    [handleUnityScheme](ICoreWebView2* sender, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            if (handleUnityScheme(u)) {
                                                args->put_Cancel(TRUE);
                                            }
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Intercept iframe navigations as well.
                            // When the user is already logged in, the OAuth provider may skip
                            // the consent screen and Privy may use an iframe-based silent auth
                            // flow. The redirect with privy_oauth_code happens inside the
                            // iframe, which NavigationStarting does NOT capture.
                            g_webView->add_FrameNavigationStarting(
                                Microsoft::WRL::Callback<ICoreWebView2NavigationStartingEventHandler>(
                                    [handleUnityScheme](ICoreWebView2* sender, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            if (handleUnityScheme(u)) {
                                                args->put_Cancel(TRUE);
                                            }
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Some flows open a new window; intercept those as well.
                            // By default WebView2 may open external browser for new windows, which breaks embedded flows.
                            g_webView->add_NewWindowRequested(
                                Microsoft::WRL::Callback<ICoreWebView2NewWindowRequestedEventHandler>(
                                    [handleUnityScheme](ICoreWebView2* sender, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            // If it's a unity scheme, forward it as a message.
                                            if (handleUnityScheme(u)) {
                                                args->put_Handled(TRUE);
                                            } else {
                                                // Otherwise, keep navigation inside the existing WebView.
                                                // This prevents OAuth flows from popping out to an external browser.
                                                if (sender) {
                                                    sender->Navigate(u.c_str());
                                                }
                                                args->put_Handled(TRUE);
                                            }
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            if (g_loadedCallback) g_loadedCallback("");

                            // If a URL was requested before initialization completed, navigate now.
                            if (!g_pendingUrl.empty()) {
                                g_webView->Navigate(g_pendingUrl.c_str());
                                g_pendingUrl.clear();
                            }

                            // If JS was queued before initialization, execute now.
                            if (!g_pendingJs.empty()) {
                                g_webView->ExecuteScript(g_pendingJs.c_str(), nullptr);
                                g_pendingJs.clear();
                            }

                            // Keep the window hidden until explicitly requested via LoadUrl.
                            // Window will be shown by PrivyWebView_LoadUrl() / PrivyWebView_ShowWindow().

                            return S_OK;
                        }).Get());

                return S_OK;
            }).Get());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_LoadUrl(const char* url)
{
    std::wstring wurl = Utf8ToUtf16(url);
    if (wurl.empty())
        return;

    EnsureWebViewWindow();

    if (!g_webView) {
        // Queue until initialization completes
        g_pendingUrl = std::move(wurl);
        return;
    }

    g_webView->Navigate(wurl.c_str());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_ShowWindow()
{
    ShowWebViewWindow();
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_HideWindow()
{
    HideWebViewWindow();
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_EvaluateJS(const char* js)
{
    std::wstring jsW = Utf8ToUtf16(js);
    if (jsW.empty())
        return;

    if (!g_webView) {
        // Queue until initialization completes
        g_pendingJs = std::move(jsW);
        return;
    }

    g_webView->ExecuteScript(jsW.c_str(), nullptr);
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Destroy()
{
    g_webViewController = nullptr;
    g_webView = nullptr;
    g_webViewEnvironment = nullptr;
}
