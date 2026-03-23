using System;
using Privy;
using System.Threading.Tasks;
using Privy.Auth;
using Privy.Auth.Models;
using Privy.Config;
using Privy.Core;
using Privy.Utils;
using UnityEngine;
using UnityEngine.UI;

public class InitialScreenController : MonoBehaviour
{
    public GameObject initialUI;
    public Button loginWithEmailButton;
    public Button loginWithSmsButton;
    public Button loginWithOAuthGoogleButton;
    public Button loginWithOAuthDiscordButton;
    public Button loginWithOAuthTwitterButton;
    public Button loginWithOAuthAppleButton;
    public EnvConfig envConfig;

    private readonly string _redirectUri = Application.platform == RuntimePlatform.WebGLPlayer ?
        (new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority) + "/unity_callback.html") :
        Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ?
        "https://auth.staging.privy.io/api/v1/oauth/callback" : // Use Privy generic callback for Windows builds
        "unitydl://";   // Use local callback for non-web builds ios/andriod

    private void Awake()
    {
        EnvFileReader.Config = envConfig;

        var appId = EnvFileReader.Get("PRIVY_APP_ID");
        var webClientId = EnvFileReader.Get("PRIVY_WEB_CLIENT_ID");
        var mobileClientId = EnvFileReader.Get("PRIVY_MOBILE_CLIENT_ID");

        PrivyManager.Initialize(new PrivyConfig
        {
            AppId = appId,
            ClientId = Application.platform == RuntimePlatform.WebGLPlayer
                ? webClientId
                : mobileClientId,
            LogLevel = PrivyLogLevel.Debug
        });

        loginWithEmailButton.onClick.AddListener(OnLoginWithEmailButtonClick);
        loginWithSmsButton.onClick.AddListener(OnLoginWithSmsButtonClick);
        loginWithOAuthGoogleButton.onClick.AddListener(OnLoginWithOAuthGoogleButtonClick);
        loginWithOAuthDiscordButton.onClick.AddListener(OnLoginWithOAuthDiscordButtonClick);
        loginWithOAuthTwitterButton.onClick.AddListener(OnLoginWithOAuthTwitterButtonClick);
        loginWithOAuthAppleButton.onClick.AddListener(OnLoginWithOAuthAppleButtonClick);
    }

    private async void Start()
    {
        var state = await PrivyManager.Instance.GetAuthState();
        Debug.Log("PrivyManager is ready.");
        if (state == AuthState.Authenticated)
            UIManager.Instance.ShowAuthorizedScreen();
    }

    private void OnLoginWithEmailButtonClick()
    {
        UIManager.Instance.ShowSendCodeScreen(AuthScreenController.LoginMethod.Email);
    }

    private void OnLoginWithSmsButtonClick()
    {
        UIManager.Instance.ShowSendCodeScreen(AuthScreenController.LoginMethod.SMS);
    }

    private async void OnLoginWithOAuthGoogleButtonClick()
    {
        await LoginWithOAuthProvider(OAuthProvider.Google);
    }

    private async void OnLoginWithOAuthDiscordButtonClick()
    {
        await LoginWithOAuthProvider(OAuthProvider.Discord);
    }

    private async void OnLoginWithOAuthTwitterButtonClick()
    {
        await LoginWithOAuthProvider(OAuthProvider.Twitter);
    }

    private async void OnLoginWithOAuthAppleButtonClick()
    {
        await LoginWithOAuthProvider(OAuthProvider.Apple);
    }

    private async Task LoginWithOAuthProvider(OAuthProvider provider)
    {
        try
        {
            var state = await PrivyManager.Instance.OAuth.LoginWithProvider(provider, _redirectUri);
            if (state == AuthState.Authenticated)
            {
                UIManager.Instance.ShowAuthorizedScreen();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"OAuth login failed for {provider}: {ex.Message}");
        }
    }
}
