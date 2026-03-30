// Native plugin for Windows providing two isolated WebView2 instances:
//
//   1. **Wallet WebView** – Hidden, persistent. Hosts the Privy embedded-wallet
//      iframe and handles JSON postMessage communication with Unity.
//
//   2. **OAuth WebView** – Visible, transient. Shown only during OAuth login
//      flows to display provider consent screens. Intercepts the redirect
//      containing `privy_oauth_code` and forwards it to Unity.
//
// Each webview gets its own ICoreWebView2Environment with a **separate user-data
// directory**, ensuring full browser-level isolation of cookies, localStorage,
// and session state between OAuth and wallet contexts.
//
// Build requirements:
// - WebView2 SDK (Microsoft Edge WebView2)
// - Visual Studio (MSVC) toolchain
//

#include <windows.h>
#include <shlobj.h>   // SHGetFolderPathW
#include <string>
#include <vector>
#include <functional>

// WebView2 headers (part of Microsoft Edge WebView2 SDK)
#include "WebView2.h"

// WRL smart pointers for COM lifetime management
#include <wrl.h>

// ---------------------------------------------------------------------------
// Callback types shared with the C# managed layer.
// ---------------------------------------------------------------------------
using MessageCallback = void(__cdecl*)(const char*);
using StatusCallback  = void(__cdecl*)(const char*);

// ---------------------------------------------------------------------------
// Encoding helpers
// ---------------------------------------------------------------------------
static std::wstring Utf8ToUtf16(const std::string& utf8)
{
    if (utf8.empty())
        return {};

    int size = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, nullptr, 0);
    if (size <= 0)
        return {};

    std::vector<wchar_t> buffer(size);
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(), -1, buffer.data(), size);

    if (!buffer.empty() && buffer.back() == L'\0')
        buffer.pop_back();

    return std::wstring(buffer.begin(), buffer.end());
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

    if (!buffer.empty() && buffer.back() == '\0')
        buffer.pop_back();

    return std::string(buffer.begin(), buffer.end());
}

// Return <LocalAppData>/PrivyWebView/<subfolder>  as a wide string.
// Each webview instance uses a different subfolder to achieve browser-level isolation.
static std::wstring GetUserDataFolder(const wchar_t* subfolder)
{
    wchar_t appDataPath[MAX_PATH] = {};
    if (SUCCEEDED(SHGetFolderPathW(nullptr, CSIDL_LOCAL_APPDATA, nullptr, 0, appDataPath)))
    {
        std::wstring path(appDataPath);
        path += L"\\PrivyWebView\\";
        path += subfolder;
        return path;
    }
    // Fallback: relative to current directory
    std::wstring fallback = L".\\PrivyWebView\\";
    fallback += subfolder;
    return fallback;
}

// ===================================================================
//  WALLET WEBVIEW  – hidden, persistent, iframe communication
// ===================================================================

static MessageCallback g_walletMessageCb  = nullptr;
static StatusCallback  g_walletLoadedCb   = nullptr;
static StatusCallback  g_walletErrorCb    = nullptr;

static Microsoft::WRL::ComPtr<ICoreWebView2Environment> g_walletEnv;
static Microsoft::WRL::ComPtr<ICoreWebView2Controller>  g_walletController;
static Microsoft::WRL::ComPtr<ICoreWebView2>            g_walletWebView;

static HWND g_walletHWnd = nullptr;
static const wchar_t kWalletWindowClass[] = L"PrivyWalletWebViewClass";

static std::wstring g_walletPendingUrl;
static std::wstring g_walletPendingJs;

static LRESULT CALLBACK WalletWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    // The wallet window should never be visible to the user – ignore close.
    if (message == WM_CLOSE) {
        ShowWindow(hWnd, SW_HIDE);
        return 0;
    }
    return DefWindowProcW(hWnd, message, wParam, lParam);
}

static HWND EnsureWalletWindow()
{
    if (g_walletHWnd && !IsWindow(g_walletHWnd))
        g_walletHWnd = nullptr;

    if (g_walletHWnd)
        return g_walletHWnd;

    WNDCLASSEXW wcex = {};
    wcex.cbSize        = sizeof(wcex);
    wcex.style         = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc   = WalletWndProc;
    wcex.hInstance      = GetModuleHandleW(nullptr);
    wcex.hCursor        = LoadCursorW(nullptr, (LPCWSTR)IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW + 1);
    wcex.lpszClassName  = kWalletWindowClass;

    if (!RegisterClassExW(&wcex) && GetLastError() != ERROR_CLASS_ALREADY_EXISTS)
        return nullptr;

    g_walletHWnd = CreateWindowExW(
        0, kWalletWindowClass, L"PrivyWallet",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 1024, 768,
        nullptr, nullptr, wcex.hInstance, nullptr);

    if (g_walletHWnd) {
        ShowWindow(g_walletHWnd, SW_HIDE);
        UpdateWindow(g_walletHWnd);
    }
    return g_walletHWnd;
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Wallet_Initialize(
    MessageCallback onMessage, StatusCallback onLoaded, StatusCallback onError)
{
    g_walletMessageCb = onMessage;
    g_walletLoadedCb  = onLoaded;
    g_walletErrorCb   = onError;

    EnsureWalletWindow();

    std::wstring userDataDir = GetUserDataFolder(L"Wallet");

    HRESULT hr = CreateCoreWebView2EnvironmentWithOptions(
        nullptr, userDataDir.c_str(), nullptr,
        Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
                if (FAILED(result)) {
                    if (g_walletErrorCb) {
                        char buf[128];
                        sprintf_s(buf, "Wallet WebView2 env failed (0x%08X)", result);
                        g_walletErrorCb(buf);
                    }
                    return result;
                }

                g_walletEnv = env;
                env->CreateCoreWebView2Controller(
                    g_walletHWnd,
                    Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
                            if (FAILED(result)) {
                                if (g_walletErrorCb) {
                                    char buf[128];
                                    sprintf_s(buf, "Wallet controller failed (0x%08X)", result);
                                    g_walletErrorCb(buf);
                                }
                                return result;
                            }

                            g_walletController = controller;
                            controller->get_CoreWebView2(&g_walletWebView);
                            controller->put_IsVisible(TRUE);
                            RECT bounds = {0, 0, 1024, 768};
                            controller->put_Bounds(bounds);

                            // Forward postMessage from the embedded wallet iframe to Unity.
                            g_walletWebView->add_WebMessageReceived(
                                Microsoft::WRL::Callback<ICoreWebView2WebMessageReceivedEventHandler>(
                                    [](ICoreWebView2*, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
                                        PWSTR msg = nullptr;
                                        if (SUCCEEDED(args->TryGetWebMessageAsString(&msg)) && msg) {
                                            std::string utf8 = Utf16ToUtf8(std::wstring(msg));
                                            if (g_walletMessageCb)
                                                g_walletMessageCb(utf8.c_str());
                                            CoTaskMemFree(msg);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Prevent new-window popups from the wallet iframe.
                            g_walletWebView->add_NewWindowRequested(
                                Microsoft::WRL::Callback<ICoreWebView2NewWindowRequestedEventHandler>(
                                    [](ICoreWebView2* sender, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            if (sender)
                                                sender->Navigate(uri);
                                            args->put_Handled(TRUE);
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Notify the managed side when a page finishes loading so it can
                            // inject the UnityProxy and start the ready-ping at the right time.
                            g_walletWebView->add_NavigationCompleted(
                                Microsoft::WRL::Callback<ICoreWebView2NavigationCompletedEventHandler>(
                                    [](ICoreWebView2* sender, ICoreWebView2NavigationCompletedEventArgs*) -> HRESULT {
                                        PWSTR source = nullptr;
                                        if (SUCCEEDED(sender->get_Source(&source)) && source) {
                                            std::string utf8 = Utf16ToUtf8(std::wstring(source));
                                            if (g_walletLoadedCb)
                                                g_walletLoadedCb(utf8.c_str());
                                            CoTaskMemFree(source);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            if (!g_walletPendingUrl.empty()) {
                                g_walletWebView->Navigate(g_walletPendingUrl.c_str());
                                g_walletPendingUrl.clear();
                            }
                            if (!g_walletPendingJs.empty()) {
                                g_walletWebView->ExecuteScript(g_walletPendingJs.c_str(), nullptr);
                                g_walletPendingJs.clear();
                            }

                            return S_OK;
                        }).Get());
                return S_OK;
            }).Get());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Wallet_LoadUrl(const char* url)
{
    std::wstring wurl = Utf8ToUtf16(url);
    if (wurl.empty()) return;

    EnsureWalletWindow();

    if (!g_walletWebView) {
        g_walletPendingUrl = std::move(wurl);
        return;
    }
    g_walletWebView->Navigate(wurl.c_str());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Wallet_EvaluateJS(const char* js)
{
    std::wstring jsW = Utf8ToUtf16(js);
    if (jsW.empty()) return;

    if (!g_walletWebView) {
        g_walletPendingJs = std::move(jsW);
        return;
    }
    g_walletWebView->ExecuteScript(jsW.c_str(), nullptr);
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_Wallet_Destroy()
{
    g_walletController = nullptr;
    g_walletWebView    = nullptr;
    g_walletEnv        = nullptr;
}

// ===================================================================
//  OAUTH WEBVIEW  – visible, transient, redirect interception
// ===================================================================

static MessageCallback g_oauthMessageCb = nullptr;
static StatusCallback  g_oauthLoadedCb  = nullptr;
static StatusCallback  g_oauthErrorCb   = nullptr;

static Microsoft::WRL::ComPtr<ICoreWebView2Environment> g_oauthEnv;
static Microsoft::WRL::ComPtr<ICoreWebView2Controller>  g_oauthController;
static Microsoft::WRL::ComPtr<ICoreWebView2>            g_oauthWebView;

static HWND g_oauthHWnd = nullptr;
static const wchar_t kOAuthWindowClass[] = L"PrivyOAuthWebViewClass";

static std::wstring g_oauthPendingUrl;
static std::wstring g_oauthRedirectUri;

static LRESULT CALLBACK OAuthWndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
{
    if (message == WM_CLOSE) {
        if (g_oauthErrorCb)
            g_oauthErrorCb("WEBVIEW_WINDOW_CLOSED");
        ShowWindow(hWnd, SW_HIDE);
        return 0;
    }
    if (message == WM_SIZE && g_oauthController) {
        RECT bounds;
        GetClientRect(hWnd, &bounds);
        g_oauthController->put_Bounds(bounds);
    }
    return DefWindowProcW(hWnd, message, wParam, lParam);
}

static HWND EnsureOAuthWindow()
{
    if (g_oauthHWnd && !IsWindow(g_oauthHWnd))
        g_oauthHWnd = nullptr;

    if (g_oauthHWnd)
        return g_oauthHWnd;

    WNDCLASSEXW wcex = {};
    wcex.cbSize        = sizeof(wcex);
    wcex.style         = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc   = OAuthWndProc;
    wcex.hInstance      = GetModuleHandleW(nullptr);
    wcex.hCursor        = LoadCursorW(nullptr, (LPCWSTR)IDC_ARROW);
    wcex.hbrBackground  = (HBRUSH)(COLOR_WINDOW + 1);
    wcex.lpszClassName  = kOAuthWindowClass;

    if (!RegisterClassExW(&wcex) && GetLastError() != ERROR_CLASS_ALREADY_EXISTS)
        return nullptr;

    g_oauthHWnd = CreateWindowExW(
        0, kOAuthWindowClass, L"Privy Login",
        WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 1024, 768,
        nullptr, nullptr, wcex.hInstance, nullptr);

    if (g_oauthHWnd) {
        ShowWindow(g_oauthHWnd, SW_HIDE);
        UpdateWindow(g_oauthHWnd);
    }
    return g_oauthHWnd;
}

static void ShowOAuthWindow()
{
    EnsureOAuthWindow();
    if (g_oauthHWnd && IsWindow(g_oauthHWnd)) {
        ShowWindow(g_oauthHWnd, SW_SHOW);
        SetForegroundWindow(g_oauthHWnd);
        SetWindowPos(g_oauthHWnd, HWND_TOPMOST, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
    }
}

static void HideOAuthWindow()
{
    if (g_oauthHWnd && IsWindow(g_oauthHWnd))
        ShowWindow(g_oauthHWnd, SW_HIDE);
}

// Checks whether a URL contains the OAuth completion marker.
// If found: hides the OAuth window and sends the URL to the managed callback.
static bool InterceptOAuthRedirect(const std::wstring& url)
{
    if (url.find(L"privy_oauth_code=") != std::wstring::npos) {
        HideOAuthWindow();
        std::string utf8 = Utf16ToUtf8(url);
        if (g_oauthMessageCb)
            g_oauthMessageCb(utf8.c_str());
        return true;
    }
    return false;
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_SetRedirectUrl(const char* url)
{
    if (url == nullptr || url[0] == '\0') {
        g_oauthRedirectUri.clear();
        return;
    }
    g_oauthRedirectUri = Utf8ToUtf16(url);
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_Initialize(
    MessageCallback onMessage, StatusCallback onLoaded, StatusCallback onError)
{
    g_oauthMessageCb = onMessage;
    g_oauthLoadedCb  = onLoaded;
    g_oauthErrorCb   = onError;

    EnsureOAuthWindow();

    std::wstring userDataDir = GetUserDataFolder(L"OAuth");

    HRESULT hr = CreateCoreWebView2EnvironmentWithOptions(
        nullptr, userDataDir.c_str(), nullptr,
        Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>(
            [](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
                if (FAILED(result)) {
                    if (g_oauthErrorCb) {
                        char buf[128];
                        sprintf_s(buf, "OAuth WebView2 env failed (0x%08X)", result);
                        g_oauthErrorCb(buf);
                    }
                    return result;
                }

                g_oauthEnv = env;
                env->CreateCoreWebView2Controller(
                    g_oauthHWnd,
                    Microsoft::WRL::Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>(
                        [](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
                            if (FAILED(result)) {
                                if (g_oauthErrorCb) {
                                    char buf[128];
                                    sprintf_s(buf, "OAuth controller failed (0x%08X)", result);
                                    g_oauthErrorCb(buf);
                                }
                                return result;
                            }

                            g_oauthController = controller;
                            controller->get_CoreWebView2(&g_oauthWebView);
                            controller->put_IsVisible(TRUE);

                            RECT bounds;
                            GetClientRect(g_oauthHWnd, &bounds);
                            controller->put_Bounds(bounds);

                            // Top-level navigation: check for OAuth redirect.
                            g_oauthWebView->add_NavigationStarting(
                                Microsoft::WRL::Callback<ICoreWebView2NavigationStartingEventHandler>(
                                    [](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            if (InterceptOAuthRedirect(u))
                                                args->put_Cancel(TRUE);
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // Iframe navigation: silent-auth flows redirect inside an iframe.
                            g_oauthWebView->add_FrameNavigationStarting(
                                Microsoft::WRL::Callback<ICoreWebView2NavigationStartingEventHandler>(
                                    [](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            if (InterceptOAuthRedirect(u))
                                                args->put_Cancel(TRUE);
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            // New-window requests: keep inside the OAuth webview.
                            g_oauthWebView->add_NewWindowRequested(
                                Microsoft::WRL::Callback<ICoreWebView2NewWindowRequestedEventHandler>(
                                    [](ICoreWebView2* sender, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
                                        LPWSTR uri = nullptr;
                                        if (SUCCEEDED(args->get_Uri(&uri)) && uri) {
                                            std::wstring u(uri);
                                            if (InterceptOAuthRedirect(u)) {
                                                args->put_Handled(TRUE);
                                            } else if (sender) {
                                                sender->Navigate(u.c_str());
                                                args->put_Handled(TRUE);
                                            }
                                            CoTaskMemFree(uri);
                                        }
                                        return S_OK;
                                    }).Get(),
                                nullptr);

                            if (g_oauthLoadedCb) g_oauthLoadedCb("");

                            if (!g_oauthPendingUrl.empty()) {
                                g_oauthWebView->Navigate(g_oauthPendingUrl.c_str());
                                g_oauthPendingUrl.clear();
                            }

                            return S_OK;
                        }).Get());
                return S_OK;
            }).Get());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_LoadUrl(const char* url)
{
    std::wstring wurl = Utf8ToUtf16(url);
    if (wurl.empty()) return;

    EnsureOAuthWindow();

    if (!g_oauthWebView) {
        g_oauthPendingUrl = std::move(wurl);
        return;
    }
    g_oauthWebView->Navigate(wurl.c_str());
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_ShowWindow()
{
    ShowOAuthWindow();
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_HideWindow()
{
    HideOAuthWindow();
}

extern "C" __declspec(dllexport) void __cdecl PrivyWebView_OAuth_Destroy()
{
    g_oauthController = nullptr;
    g_oauthWebView    = nullptr;
    g_oauthEnv        = nullptr;
}
