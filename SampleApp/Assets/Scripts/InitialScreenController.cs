using System;
using Privy;
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
        "unitydl://";   // Must set each platforms deeplink scheme to this
    
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
            LogLevel = PrivyLogLevel.DEBUG
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
        await PrivyManager.Instance.GetAuthState();
        Debug.Log("PrivyManager is ready.");
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
        await PrivyManager.Instance.OAuth.LoginWithProvider(OAuthProvider.Google, _redirectUri);
    }
    
    private async void OnLoginWithOAuthDiscordButtonClick()
    {
        await PrivyManager.Instance.OAuth.LoginWithProvider(OAuthProvider.Discord, _redirectUri);
    }

    private async void OnLoginWithOAuthTwitterButtonClick()
    {
        await PrivyManager.Instance.OAuth.LoginWithProvider(OAuthProvider.Twitter, _redirectUri);
    }

    private async void OnLoginWithOAuthAppleButtonClick()
    {
        await PrivyManager.Instance.OAuth.LoginWithProvider(OAuthProvider.Apple, _redirectUri);
    }
}
